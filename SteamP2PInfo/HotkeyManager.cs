using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SteamP2PInfo.WinAPI;
using MahApps.Metro.Controls;

namespace SteamP2PInfo
{
    // Static class handling hotkeys which activates once on a key combination
    // whose actual key may change at any time.
    public static class HotkeyManager
    {
        /// <summary>
        /// Dynamic hotkey: a getter which gets the key to detect, and a handler.
        /// </summary>
        private class DynamicHotkey
        {
            public Func<int> getter;
            public Action handler;
            public IntPtr hWindow;
        }

        private static int hkCnt;
        private static IntPtr hHook;
        private static User32.HookProc hookProc;
        private static Dictionary<int, DynamicHotkey> hotkeys;

        public static bool Enabled => hHook != IntPtr.Zero;

        static HotkeyManager()
        {
            hkCnt = 0;
            hHook = IntPtr.Zero;
            hotkeys = new Dictionary<int, DynamicHotkey>();
            hookProc = new User32.HookProc(EvtDispatcher);
        }

        public static bool Enable()
        {
            if (hHook != IntPtr.Zero) return true;
            hHook = User32.SetWindowsHookEx(13, hookProc, IntPtr.Zero, 0);
            return Enabled;
        }

        public static void Disable()
        {
            if (Enabled)
            {
                User32.UnhookWindowsHookEx(hHook);
                hHook = IntPtr.Zero;
            }
        }

        public static int AddHotkey(IntPtr hWindow, Func<HotKey> getter, Action handler)
        {
            int iget()
            {
                HotKey hk = getter();
                return (hk == null) ? 0 : (int)hk.ModifierKeys << 8 | (int)hk.Key;
            };
            return AddHotkey(new DynamicHotkey { hWindow = hWindow, getter = iget, handler = handler });
        }

        public static int AddHotkey(IntPtr hWindow, Func<int> getter, Action handler)
        {
            return AddHotkey(new DynamicHotkey { hWindow = hWindow, getter = getter, handler = handler });
        }

        private static int AddHotkey(DynamicHotkey hk)
        {
            hotkeys[++hkCnt] = hk;
            return hkCnt;
        }

        public static bool RemoveHotkey(int id)
        {
            return hotkeys.Remove(id);
        }

        private static IntPtr EvtDispatcher(int nCode, IntPtr wParam, IntPtr lParam)
        {
            int msg = wParam.ToInt32();
            if (nCode >= 0 && msg == 0x100 || msg == 0x104) // Keydown message
            {
                KBDLLHOOKSTRUCT kbInfo = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                IntPtr foreWindow = User32.GetForegroundWindow();

                int kState = kbInfo.vkCode; // Build current key state (with modifiers)
                kState |= (User32.GetAsyncKeyState(0x5B) & 0x8000) >> 4; // LWIN
                kState |= (User32.GetAsyncKeyState(0x5c) & 0x8000) >> 4; // RWIN
                kState |= (User32.GetAsyncKeyState(0x10) & 0x8000) >> 5; // SHIFT
                kState |= (User32.GetAsyncKeyState(0x11) & 0x8000) >> 6; // CTRL
                kState |= (User32.GetAsyncKeyState(0x12) & 0x8000) >> 7; // ALT

                foreach (DynamicHotkey hk in hotkeys.Values)
                {
                    if (hk.hWindow == foreWindow && hk.getter() == kState)
                        hk.handler();
                }
            }
            return User32.CallNextHookEx(hHook, nCode, wParam, lParam);
        }
    }
}
