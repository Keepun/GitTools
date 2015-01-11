using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace GitLineTrim
{
    public class Program
    {
        private static void Main(string[] args)
        {
            List<string> files = new List<string>();
            bool backup = true;
            for (int x = 0; x < args.Length; x++) {
                int pos = args[x].ToLower().IndexOf("nobackup");
                if (pos > 0 && args[x].IndexOfAny(new char[] {'-', '/'}, 0, pos) > -1) {
                    backup = false;
                } else {
                    files.Add(args[x]);
                }
            }
            try {
                using (MemoryStream finput = new MemoryStream()) {
                    using (Stream bcon = Console.OpenStandardInput()) {
                        byte[] buffer = new byte[100000];
                        int count;
                        while ((count = bcon.Read(buffer, 0, buffer.Length)) > 0) {
                            finput.Write(buffer, 0, count);
                        }
                    }
                    try {
                        if (finput.Length > 0) {
                            long filesize;
                            using (MemoryStream fmem = LineTrim(finput, out filesize)) {
                                if (fmem == null) {
                                    using (Stream bcon = Console.OpenStandardOutput()) {
                                        finput.WriteTo(bcon);
                                    }
                                    return;
                                }
                                using (Stream bcon = Console.OpenStandardOutput()) {
                                    fmem.WriteTo(bcon);
                                }
                                return;
                            }
                        }
                    }
                    catch (Exception ex) {
                        ToLog(args, ex);
                        return;
                    }
                }
            }
            catch {
            }
            if (files.Count < 1) {
                Console.WriteLine("{0} [-nobackup] [files]",
                                  Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location));
                return;
            }
            foreach (string file in files) {
                Console.Write(file);
                try {
                    long filesize;
                    using (MemoryStream fmem = LineTrim(File.OpenRead(file), out filesize)) {
                        if (fmem == null) {
                            Console.WriteLine(" - Binary");
                            continue;
                        }
                        if (fmem.Length == filesize) {
                            continue;
                        }
                        if (backup) {
                            File.Copy(file, file + ".bak", true);
                        }
                        using (Stream fto = File.Open(file, FileMode.Truncate, FileAccess.Write, FileShare.Read)) {
                            fmem.WriteTo(fto);
                        }
                    }
                    Console.WriteLine(" - OK");
                }
                catch (Exception ex) {
                    Console.WriteLine();
                    Console.WriteLine(ex);
                    ToLog(args, ex);
                }
            }
        }

        private static MemoryStream LineTrim(Stream stream, out long fileSize)
        {
            fileSize = stream.Length;
            byte[] bom;
            int seekbom;
            Encoding codepage = GetEncodingStream(stream, out bom, out seekbom);
            if (codepage == null) {
                return null;
            }
            MemoryStream fmem = new MemoryStream();
            using (StreamReader ffrom = new StreamReader(stream, codepage)) {
                fmem.Write(bom, 0, seekbom);
                ffrom.DiscardBufferedData();
                ffrom.BaseStream.Seek(seekbom, SeekOrigin.Begin);
                string newLine = "\r\n";
                string line;
                if ((line = ffrom.ReadLine()) != null) {
                    ffrom.DiscardBufferedData();
                    ffrom.BaseStream.Seek(seekbom + codepage.GetByteCount(line), SeekOrigin.Begin);
                    int ch = ffrom.Peek();
                    if (ch != -1) {
                        if (ch == '\n') {
                            newLine = "\n";
                            ffrom.Read();
                        } else if (ch == '\r') {
                            ffrom.Read();
                            if ((ch = ffrom.Peek()) != -1 && ch == '\n') {
                                newLine = "\r\n";
                                ffrom.Read();
                            } else {
                                newLine = "\r";
                            }
                        }
                    }
                    do {
                        line = line.TrimEnd();
                        if (ffrom.EndOfStream) {
                            int cnl = codepage.GetByteCount(newLine);
                            if (ffrom.BaseStream.Length >= cnl) {
                                ffrom.DiscardBufferedData();
                                ffrom.BaseStream.Seek(-1*cnl, SeekOrigin.End);
                                bool bnl = true;
                                foreach (char chr in newLine) {
                                    if (chr != ffrom.Read()) {
                                        bnl = false;
                                    }
                                }
                                if (bnl) {
                                    line += newLine;
                                }
                            }
                        } else {
                            line += newLine;
                        }
                        byte[] bline = codepage.GetBytes(line);
                        fmem.Write(bline, 0, bline.Length);
                    } while ((line = ffrom.ReadLine()) != null);
                }
            }
            fmem.Seek(0, SeekOrigin.Begin);
            return fmem;
        }

        public static Encoding GetEncodingStream(Stream stream, out byte[] bom, out int sizebom)
        {
            BinaryReader bin = new BinaryReader(stream);
            bom = new byte[4];
            bin.BaseStream.Seek(0, SeekOrigin.Begin);
            bin.BaseStream.Read(bom, 0, bom.Length);
            bin.BaseStream.Seek(0, SeekOrigin.Begin);
            if (bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF) {
                sizebom = 4;
                return Encoding.GetEncoding("UTF-32BE"); // UTF-32, big-endian
            } else if (bom[0] == 0xFE && bom[1] == 0xFF) {
                sizebom = 2;
                return Encoding.BigEndianUnicode; // UTF-16, big-endian
            } else if (bom[0] == 0xFF && bom[1] == 0xFE) {
                if (bom[2] == 0x00 && bom[2] == 0x00) {
                    sizebom = 4;
                    return Encoding.UTF32; // UTF-32, little-endian
                } else {
                    sizebom = 2;
                    return Encoding.Unicode; // UTF-16, little-endian
                }
            } else if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) {
                sizebom = 3;
                return Encoding.UTF8;
            } else {
                sizebom = 0;

                bool binary = false;
                long fsize = bin.BaseStream.Length;
                if (fsize > 100000) {
                    fsize = 100000;
                }
                byte[] bts = new byte[fsize];
                bin.BaseStream.Seek(sizebom, SeekOrigin.Begin);
                bin.BaseStream.Read(bts, 0, (int)fsize);
                bin.BaseStream.Seek(0, SeekOrigin.Begin);
                for (int x = 0; x < fsize; x++) {
                    if (bts[x] == 0) {
                        binary = true;
                        break;
                    }
                }
                if (binary) {
                    return null;
                }

                return Encoding.Default;
            }
        }

        private static void ToLog(string[] args, Exception ex)
        {
            using (StreamWriter flog = new StreamWriter(
                "!" + Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".log", true)
                ) {
                if (flog.BaseStream.Position > 0) {
                    flog.WriteLine();
                }
                flog.WriteLine(DateTime.Now);
                foreach (string arg in args) {
                    flog.WriteLine(arg);
                }
                flog.WriteLine(ex);
            }
        }
    }
}
