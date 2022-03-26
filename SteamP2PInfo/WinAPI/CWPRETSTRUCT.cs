using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SteamP2PInfo.WinAPI
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CWPRETSTRUCT
    {
        public uint lResult;
        public IntPtr lParam;
        public IntPtr wParam;
        public uint message;
        public IntPtr hwnd;
    }
}
