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
    public enum ProcessAccessFlags
    {
        /// <summary>
        /// Required to create a thread.
        /// </summary>
        CreateThread = 0x0002,

        /// <summary>
        ///
        /// </summary>
        SetSessionId = 0x0004,

        /// <summary>
        /// Required to perform an operation on the address space of a process
        /// </summary>
        VmOperation = 0x0008,

        /// <summary>
        /// Required to read memory in a process using ReadProcessMemory.
        /// </summary>
        VmRead = 0x0010,

        /// <summary>
        /// Required to write to memory in a process using WriteProcessMemory.
        /// </summary>
        VmWrite = 0x0020,

        /// <summary>
        /// Required to duplicate a handle using DuplicateHandle.
        /// </summary>
        DupHandle = 0x0040,

        /// <summary>
        /// Required to create a process.
        /// </summary>
        CreateProcess = 0x0080,

        /// <summary>
        /// Required to set memory limits using SetProcessWorkingSetSize.
        /// </summary>
        SetQuota = 0x0100,

        /// <summary>
        /// Required to set certain information about a process, such as its priority class (see SetPriorityClass).
        /// </summary>
        SetInformation = 0x0200,

        /// <summary>
        /// Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken).
        /// </summary>
        QueryInformation = 0x0400,

        /// <summary>
        /// Required to suspend or resume a process.
        /// </summary>
        SuspendResume = 0x0800,

        /// <summary>
        /// Required to retrieve certain information about a process (see GetExitCodeProcess, GetPriorityClass, IsProcessInJob, QueryFullProcessImageName).
        /// A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION.
        /// </summary>
        QueryLimitedInformation = 0x1000,

        /// <summary>
        /// Required to wait for the process to terminate using the wait functions.
        /// </summary>
        Synchronize = 0x100000,

        /// <summary>
        /// Required to delete the object.
        /// </summary>
        Delete = 0x00010000,

        /// <summary>
        /// Required to read information in the security descriptor for the object, not including the information in the SACL.
        /// To read or write the SACL, you must request the ACCESS_SYSTEM_SECURITY access right. For more information, see SACL Access Right.
        /// </summary>
        ReadControl = 0x00020000,

        /// <summary>
        /// Required to modify the DACL in the security descriptor for the object.
        /// </summary>
        WriteDac = 0x00040000,

        /// <summary>
        /// Required to change the owner in the security descriptor for the object.
        /// </summary>
        WriteOwner = 0x00080000,

        /// <summary>
        /// 
        /// </summary>
        StandardRightsRequired = 0x000F0000,

        /// <summary>
        /// All possible access rights for a process object.
        /// </summary>
        AllAccess = StandardRightsRequired | Synchronize | 0xFFFF
    }
}
