using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FSTester;

namespace FSTester.Tests.Others
{
    public class BackupAndShared : Test
    {

        public BackupAndShared(string workPath) : base(workPath) { }

        override public bool Go()
        {
            if (!base.Go())
            {
                return false;
            }
            try {
                string path = this.workingPath + "\\" + this.getUniqueTempBasename();

                Directory.CreateDirectory(path);

                IntPtr tmp1 = Lowlevel.CreateFile(path, FileAccess.Read, FileShare.None, IntPtr.Zero, FileMode.Open, Lowlevel.FileAttributesEx.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
                if ( (int)tmp1 <= 0)
                {
                    throw new Exception("Cannot get tmp1?");
                }
                Lowlevel.FILETIME t1 = new Lowlevel.FILETIME();
                Lowlevel.FILETIME t2 = new Lowlevel.FILETIME();
                Lowlevel.FILETIME t3 = new Lowlevel.FILETIME();
                bool r1 = Lowlevel.GetFileTime(tmp1, ref t1, ref t2, ref t3);

                IntPtr tmp2 = Lowlevel.CreateFile(path, FileAccess.Read, FileShare.None, IntPtr.Zero, FileMode.Open, Lowlevel.FileAttributesEx.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
                if ( (int)tmp2 == -1 )
                {
                    return true;
                }

                bool r2 = Lowlevel.GetFileTime(tmp1, ref t1, ref t2, ref t3);
                bool r3 = Lowlevel.GetFileTime(tmp2, ref t1, ref t2, ref t3);

                throw new Exception("Failed, I can open second time with shared.none (related to Dokany #98 and Dokan <=1.0.0rc2)");

                /*HANDLE tmp1 = CreateFile(L"M:\\test", GENERIC_READ, 0, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, 0);
                std::cout << GetLastError() << std::endl;
                HANDLE tmp2 = CreateFile(L"M:\\test", GENERIC_READ, 0, 0, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, 0);
                std::cout << GetLastError() << std::endl;
                CloseHandle(tmp1);
                CloseHandle(tmp2);*/
            }
            catch(Exception ex)
            {
                this.lastError = ex.Message;
            }

            return false;
        }
    } 
}
