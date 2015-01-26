/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
