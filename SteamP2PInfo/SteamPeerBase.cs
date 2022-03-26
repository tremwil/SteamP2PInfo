using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

using SteamP2PInfo.Config;

namespace SteamP2PInfo
{
    /// <summary>
    /// Repr
    /// </summary>
    abstract class SteamPeerBase : IDisposable
    {
        /// <summary>
        /// Steam ID of the peer.
        /// </summary>
        public CSteamID SteamID { get; protected set; }

        /// <summary>
        /// Main Steam ID of the peer, if playing on an alternate account. 
        /// </summary>
        public CSteamID MainSteamID { get; protected set; }

        /// <summary>
        /// Steam persona name of the peer.
        /// </summary>
        public virtual string Name { get { return SteamFriends.GetFriendPersonaName(SteamID); } }

        /// <summary>
        /// True if the peer is connected via the deprected api, ISteamNetworking.
        /// </summary>
        public abstract bool IsOldAPI { get; }

        /// <summary>
        /// Ping to peer in milliseconds.
        /// </summary>
        public abstract double Ping { get; }

        /// <summary>
        /// Subjective measure of connection quality to the remote peer where 0 = horrible and 1 = perfect.
        /// May not be very accurate if using the old API. When using the new API, should be directly related to packet loss.
        /// </summary>
        public abstract double ConnectionQuality { get; }

        /// <summary>
        /// ARGB hexadecimal color code used to fill the ping text.
        /// </summary>
        public string PingColor
        {
            get
            {
                OverlayConfig.PingColorRange range = new OverlayConfig.PingColorRange()
                {
                    Threshold = double.NegativeInfinity,
                    Color = GameConfig.Current.OverlayConfig.TextColor
                };

                foreach (OverlayConfig.PingColorRange r in GameConfig.Current.OverlayConfig.PingColors)
                {
                    if (r.Threshold <= Ping && r.Threshold > range.Threshold)
                        range = r;
                }

                return range.Color;
            }
        }

        /// <summary>
        /// Request the main steam ID of the player via the Steam Web API.
        /// </summary>
        protected void RequestMainSteamID()
        {
            SteamWebAPI.GetMainSteamIdAsync(SteamID, id => MainSteamID = id);
        }

        protected SteamPeerBase(CSteamID steamID)
        {
            SteamID = steamID;
            RequestMainSteamID();
        }

        /// <summary>
        /// Update peer info that may not be known at instance creation time.
        /// Should return true if the peer is still connected and false otherwise.
        /// </summary>
        public abstract bool UpdatePeerInfo();

        public virtual void Dispose() { }
    }
}
