using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSTester
{
    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("You need help. right?");
                return;
            }
            else
            {
                string workingPath = args[0];

                Collection<Test> tests = new Collection<Test>();
                tests.Add(new Tests.IO.TestWriteLines(workingPath));
                tests.Add(new Tests.Others.BackupAndShared(workingPath));

                foreach (Test test in tests)
                {
                    Console.Write("Test " + test.ToString() + ": ");
                    if (test.Go())
                    {
                        Console.WriteLine("Success");
                    }
                    else
                    {
                        Console.Write("Fail");
                        Console.WriteLine(" >>> " + test.getLastError());
                    }
                    
                }
            }

            Console.ReadKey();
        }
    }
}
