using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace GitLineTrim
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> files = new List<string>();
            bool backup = true;
            for (int x = 0; x < args.Length; x++)
            {
                int pos = args[x].ToLower().IndexOf("nobackup");
                if (pos > 0 && args[x].IndexOfAny(new char[] { '-', '/' }, 0, pos) > -1)
                {
                    backup = false;
                }
                else
                {
                    files.Add(args[x]);
                }
            }
            if (files.Count < 1)
            {
                Console.WriteLine("No files!");
                Console.WriteLine("{0} [-nobackup] files",
                    Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location));
                return;
            }
            foreach (string file in files)
            {
                Console.Write(file);
                if (backup)
                {
                    File.Copy(file, file + ".bak");
                }
                try
                {
                    Encoding codepage;
                    byte[] bom = new byte[4];
                    int seekbom = 0;
                    using (BinaryReader ffrom = new BinaryReader(File.OpenRead(file)))
                    {
                        ffrom.BaseStream.Read(bom, 0, bom.Length);
                        if (bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF)
                        {
                            codepage = Encoding.GetEncoding("UTF-32BE"); // UTF-32, big-endian
                            seekbom = 4;
                        }
                        else if (bom[0] == 0xFE && bom[1] == 0xFF)
                        {
                            codepage = Encoding.BigEndianUnicode; // UTF-16, big-endian
                            seekbom = 2;
                        }
                        else if (bom[0] == 0xFF && bom[1] == 0xFE)
                        {
                            if (bom[2] == 0x00 && bom[2] == 0x00)
                            {
                                codepage = Encoding.UTF32; // UTF-32, little-endian
                                seekbom = 4;
                            }
                            else
                            {
                                codepage = Encoding.Unicode; // UTF-16, little-endian
                                seekbom = 2;
                            }
                        }
                        else if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                        {
                            codepage = Encoding.UTF8;
                            seekbom = 3;
                        }
                        else
                        {
                            codepage = Encoding.Default;
                            seekbom = 0;

                            bool binary = false;
                            long fsize = ffrom.BaseStream.Length;
                            if (fsize > 100000)
                            {
                                fsize = 100000;
                            }
                            byte[] bts = new byte[fsize];
                            ffrom.BaseStream.Seek(seekbom, SeekOrigin.Begin);
                            ffrom.BaseStream.Read(bts, 0, (int)fsize);
                            for (int x = 0; x < fsize; x++)
                            {
                                if (bts[x] == 0)
                                {
                                    binary = true;
                                    break;
                                }
                            }
                            if (binary)
                            {
                                Console.WriteLine(" - Binary");
                                continue;
                            }
                        }
                    }
                    using (MemoryStream fmem = new MemoryStream())
                    {
                        using (StreamReader ffrom = new StreamReader(file, codepage))
                        {
                            fmem.Write(bom, 0, seekbom);
                            ffrom.BaseStream.Seek(seekbom, SeekOrigin.Begin);
                            string line;
                            while ((line = ffrom.ReadLine()) != null)
                            {
                                line = line.TrimEnd() + Environment.NewLine;
                                byte[] bline = codepage.GetBytes(line);
                                fmem.Write(bline, 0, bline.Length);
                            }
                            if (fmem.Length >= ffrom.BaseStream.Length)
                            {
                                continue;
                            }
                        }
                        using (Stream fto = File.Open(file, FileMode.Truncate, FileAccess.Write, FileShare.Read))
                        {
                            fmem.WriteTo(fto);
                        }
                    }
                    Console.WriteLine(" - OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine(ex);
                    using (StreamWriter flog = new StreamWriter(
                        "!" + Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".log", true))
                    {
                        if (flog.BaseStream.Position > 0)
                        {
                            flog.WriteLine();
                        }
                        flog.WriteLine(DateTime.Now);
                        foreach (string arg in args)
                        {
                            flog.WriteLine(arg);
                        }
                        flog.WriteLine(ex);
                    }
                }
            }
        }
    }
}
