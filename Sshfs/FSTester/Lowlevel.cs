using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSTester
{
    class Lowlevel
    {

        public enum FileAttributesEx
        {
            FILE_FLAG_BACKUP_SEMANTICS = 0x02000000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(
         [MarshalAs(UnmanagedType.LPTStr)] string filename,
         [MarshalAs(UnmanagedType.U4)] FileAccess access,
         [MarshalAs(UnmanagedType.U4)] FileShare share,
         IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
         [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
         [MarshalAs(UnmanagedType.U4)] FileAttributesEx flagsAndAttributes,
         IntPtr templateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr CreateFileA(
             [MarshalAs(UnmanagedType.LPStr)] string filename,
             [MarshalAs(UnmanagedType.U4)] FileAccess access,
             [MarshalAs(UnmanagedType.U4)] FileShare share,
             IntPtr securityAttributes,
             [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
             [MarshalAs(UnmanagedType.U4)] FileAttributesEx flagsAndAttributes,
             IntPtr templateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFileW(
             [MarshalAs(UnmanagedType.LPWStr)] string filename,
             [MarshalAs(UnmanagedType.U4)] FileAccess access,
             [MarshalAs(UnmanagedType.U4)] FileShare share,
             IntPtr securityAttributes,
             [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
             [MarshalAs(UnmanagedType.U4)] FileAttributesEx flagsAndAttributes,
             IntPtr templateFile);


        [DllImport("kernel32.dll")]
        public static extern bool GetFileTime(IntPtr hFile, ref FILETIME lpCreationTime,
             ref FILETIME lpLastAccessTime, ref FILETIME lpLastWriteTime);
    }
}
