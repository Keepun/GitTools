using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace GitEditor
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0) {
                Console.WriteLine("{0} \"path/Editor.exe\" [files]",
                                  Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location));
                return;
            }
            Console.WriteLine("GitEditor running...");
            string path = args[0];
            string pargs = "";
            for (int x = 1; x < args.Length; x++) {
                pargs += "\"" + args[x] + "\" ";
            }
            Console.WriteLine(path + " " + pargs);
            Process.Start(path, pargs);
            Console.WriteLine("continue?");
            Console.ReadLine();
        }
    }
}
