using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FSTester.Tests.IO
{
    public class TestWriteLines : Test
    { 
        public TestWriteLines(string workPath) : base(workPath) {}

        override public bool Go()
        {
            if (!base.Go())
            {
                return false;
            }

            string tmp = this.getUniqueTempBasename();

            FileStream f = File.Open(this.workingPath + " \\ " + tmp, FileMode.OpenOrCreate);
            string s = "ABCD" + "\n";
            byte[] sb = ASCIIEncoding.UTF8.GetBytes(s);
            f.Write(sb, 0, sb.Length);
            f.Seek(1, SeekOrigin.Begin);
            f.Write(sb, 0, sb.Length);

            f.Flush();
            f.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[4096];
            int received = f.Read(data, 0, data.Length);
            string ret = ASCIIEncoding.UTF8.GetString(data, 0, received);

            if (ret == "A" + s)
            {
                return true;
            }

            this.lastError = "Got "+ret+", waited for "+ "A"+s;
            return false;
        }


    }
}
