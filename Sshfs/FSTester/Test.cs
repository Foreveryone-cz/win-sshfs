using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSTester
{
    public class Test
    {
        protected string workingPath;
        protected string lastError;

        public Test(string workPath)
        {
            this.workingPath = workPath;
        }

        virtual public bool Go()
        {
            if (!Directory.Exists(this.workingPath))
            {
                this.lastError = "Working dir not found('" + this.workingPath + "')";
                return false;
            }
            return true;
        }

        public string getLastError()
        {
            return this.lastError;
        }

        protected void Log(string s)
        {
            Console.Write(s);
        }
        protected void LogLine(string line)
        {
            Console.WriteLine( line );
        }

        protected string getUniqueTempBasename( string suffix = "tmp" )
        {
            return Guid.NewGuid().ToString() + "." + suffix;
        }
    }
}
