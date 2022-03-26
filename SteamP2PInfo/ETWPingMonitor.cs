using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Net;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace SteamP2PInfo
{
    public static class ETWPingMonitor
    {
        public const int N_SAMPLES = 10;

        private class PingInfo
        {
            public double tFirstSend = -1; // Timestamp of first sent STUN packet
            public double tStunSent = -1; // Timestamp of last sent STUN packet
            public double tLastStunRecv = -1; // Timestamp of last recv STUN packet
            public double ping = -1; // Current ping

            public double avgPing = 0; // average ping over last N_SAMPLES
            public double jitter = 0; // jitter (stdev of ping) over last N_SAMPLES
            public double[] pingSamples;

            public int cnt = 0; // Number of ping packets which came back
            public int stunSentCnt = 0; // Number of STUN packets sent
            public int stunLateCnt = 0; // Number sent after the previous one had not been recieved

            public PingInfo()
            {
                pingSamples = new double[N_SAMPLES];
            }
        }

        private static TraceEventSession kernelSession;
        private static Thread eventThread;
        public static bool Running { get; private set; }
        private static Dictionary<ulong, PingInfo> pings;
        private static readonly object lockObj;

        static ETWPingMonitor()
        {
            Running = false;
            lockObj = new object();
            pings = new Dictionary<ulong, PingInfo>();
        }

        /// <summary>
        /// Begin monitoring STUN pings.
        /// </summary>
        public static void Start()
        {
            if (Running) return;

            if (!(TraceEventSession.IsElevated() ?? false))
            {
                throw new Exception("Program must be run in administrator mode");
            }

            kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName);
            kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
            kernelSession.Source.Kernel.UdpIpSend += Kernel_UdpIpSend;
            kernelSession.Source.Kernel.UdpIpRecv += Kernel_UdpIpRecv;

            Running = true;
            eventThread = new Thread(() => kernelSession.Source.Process());
            eventThread.Start();
        }

        /// <summary>
        /// Stop monitoring STUN pings.
        /// </summary>
        public static void Stop()
        {
            if (!Running) return;

            Running = false;
            kernelSession.Stop();
        }

        /// <summary>
        /// Begin tracking the ping to a remote endpoint. Net ID given by (port << 32) | ipv4.
        /// If netId already is being monitored, will reset average ping.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public static void Register(ulong netId)
        {
            lock (lockObj)
            {
                pings[netId] = new PingInfo();
            }
        }

        /// <summary>
        /// Stop tracking the ping to a remote endpoint. Net ID given by (port << 32) | ipv4.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public static void Unregister(ulong netId)
        {
            lock (lockObj)
            {
                pings.Remove(netId);
            }
        }

        /// <summary>
        /// Get the average ping to the provided endpoint, or -1 if none exists.
        /// Net ID given by (port << 32) | ipv4.
        /// </summary>
        /// <param name="netId"></param>
        /// <returns></returns>
        public static double GetPing(ulong netId)
        {
            lock (lockObj)
            {
                return pings.ContainsKey(netId) ? pings[netId].ping : -1;
            }
        }

        /// <summary>
        /// Get the average ping over N_SAMPLES to the provided endpoint, or -1 if none exists.
        /// Net ID given by (port << 32) | ipv4.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static double GetAveragePing(ulong netId)
        {
            lock (lockObj)
            {
                return pings.ContainsKey(netId) ? pings[netId].avgPing : -1;
            }
        }

        /// <summary>
        /// Get the jitter to the provided endpoint, or -1 if none exists.
        /// Net ID given by (port << 32) | ipv4.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static double GetJitter(ulong netId)
        {
            lock (lockObj)
            {
                return pings.ContainsKey(netId) ? pings[netId].jitter : -1;
            }
        }

        /// <summary>
        /// Get [stun packet sent before reply]/[stun packets sent], as a percentage.
        /// </summary>
        /// <param name="netId"></param>
        /// <returns></returns>
        public static double GetLatePacketRatio(ulong netId)
        {
            lock (lockObj)
            {
                return (pings.ContainsKey(netId) && pings[netId].stunSentCnt > 0) ? pings[netId].stunLateCnt * 100d / pings[netId].stunSentCnt : -1;
            }
        }

        private static void Kernel_UdpIpSend(UdpIpTraceData packet)
        {
            if (packet.size == 56)
            {
                uint ipv4 = BitConverter.ToUInt32(packet.daddr.MapToIPv4().GetAddressBytes(), 0);
                ulong netId = (ulong)packet.dport << 32 | ipv4;

                lock (lockObj)
                {
                    if (pings.ContainsKey(netId))
                    {
                        if (pings[netId].tFirstSend == -1)
                            pings[netId].tFirstSend = packet.TimeStampRelativeMSec;

                        pings[netId].stunSentCnt++;
                        // For the first 10 seconds of connection, some STUN packets may be dropped entirely, which messes with the ping
                        // filter. Assuming late packets were dropped at the beginning helps.
                        if (pings[netId].tStunSent == -1 || packet.TimeStampRelativeMSec - pings[netId].tFirstSend < 10000)
                            pings[netId].tStunSent = packet.TimeStampRelativeMSec;
                        else
                            pings[netId].stunLateCnt++;
                    }
                }
            }
        }

        private static void Kernel_UdpIpRecv(UdpIpTraceData packet)
        {
            if (packet.size == 68)
            {
                uint ipv4 = BitConverter.ToUInt32(packet.saddr.MapToIPv4().GetAddressBytes(), 0);
                ulong netId = (ulong)packet.sport << 32 | ipv4;

                lock (lockObj)
                {
                    if (pings.ContainsKey(netId))
                    {
                        if (pings[netId].tStunSent != -1)
                        {
                            PingInfo pi = pings[netId];

                            pi.tLastStunRecv = packet.TimeStampRelativeMSec;
                            pi.ping = packet.TimeStampRelativeMSec - pi.tStunSent;

                            pi.pingSamples[pi.cnt++ % N_SAMPLES] = pi.ping;
                            if (pi.cnt >= N_SAMPLES)
                            {   // After N_SAMPLES measurements, compute average ping and jitter
                                pi.avgPing = 0;
                                for (int i = 0; i < N_SAMPLES; i++)
                                    pi.avgPing += pi.pingSamples[i];
                                pi.avgPing /= N_SAMPLES;

                                pi.jitter = 0;
                                for (int i = 0; i < N_SAMPLES; i++)
                                    pi.jitter += Math.Pow(pi.pingSamples[i] - pi.avgPing, 2);
                                pi.jitter = Math.Sqrt(pi.jitter / N_SAMPLES);
                            }

                            pings[netId].tStunSent = -1;
                        }
                    }
                }
            }
        }
    }
}
