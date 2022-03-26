using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamP2PInfo.WinAPI
{
    /// <summary>
    /// Used to request usage of a process handle for given actvities.
    /// </summary>
    [Flags]
    public enum MemOpType
    {
        Commit      = 0x00001000,
        Reserve     = 0x00002000,
        Decommit    = 0x00004000,
        Release     = 0x00008000,
        Reset       = 0x00080000,
        ResetUndo   = 0x01000000,
        LargePages  = 0x20000000,
        Physical    = 0x00400000,
        TopDown     = 0x00100000
    }
}
