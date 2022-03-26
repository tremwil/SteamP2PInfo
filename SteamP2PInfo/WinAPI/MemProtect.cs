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
    public enum MemProtect
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        TargetsInvalid = 0x40000000,

        Guard = 0x100,
        NoCache = 0x200,
        WriteCombine = 0x400
    }
}
