using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace GitFilters
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Commandline.Option[] cmdopts =
                new Commandline.Option[]
                {
                    new Commandline.Option('h', "help", false, "Show this help"),
                    new Commandline.Option('l', "list", false, "List support codepages"),
                    new Commandline.Option((char)0, "no-bom", false, "Not to add BOM for Unicode"),
                    new Commandline.Option((char)0, "linetrim", false, "line.TrimEnd()"),
                    new Commandline.Option((char)0, "addline", false, "Add a line to the end of the file"),
                    new Commandline.Option((char)0, "from", true, "Convert From codepages"),
                    new Commandline.Option((char)0, "to", true, "Convert To codepages"),
                    new Commandline.Option((char)0, "no-backup", false, "Do not create backup files")
                };
            string[] files;
            bool parseargs = Commandline.Parse(args, cmdopts, out files);
            Dictionary<string, Commandline.Option> cmdargs = Commandline.MapOptions(cmdopts);

            bool nobom = cmdargs["no-bom"].set;
            bool linetrim = cmdargs["linetrim"].set;
            bool addline = cmdargs["addline"].set;
            bool nobackup = cmdargs["no-backup"].set;

            if (parseargs == false || cmdargs["help"].set) {
                Console.WriteLine("{0} [-h | -l] [options] [--from Codepage] [--to Codepage] [files]",
                    Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location));
                Console.WriteLine(Commandline.Print(cmdopts));
                Console.WriteLine("P.S.");
                Console.WriteLine("When the input stream files are ignored.");
                Console.WriteLine("Auto detection From only for Unicode with BOM.");
                Console.WriteLine("If you do not set the From and not Unicode, then the conversion will not.");
                Console.WriteLine("To=UTF-8 with BOM default.");
                return;
            }

            if (cmdargs["list"].set) {
                foreach (EncodingInfo cpinfo in Encoding.GetEncodings()) {
                    Console.WriteLine("{0}\t{1}\t{2}", cpinfo.CodePage, cpinfo.Name, cpinfo.DisplayName);
                }
                return;
            }

            Encoding cdpgfrom = null;
            if (cmdargs["from"].set) {
                try {
                    int cdpgnum;
                    if (int.TryParse(cmdargs["from"].value, out cdpgnum)) {
                        cdpgfrom = Encoding.GetEncoding(cdpgnum);
                    } else {
                        cdpgfrom = Encoding.GetEncoding(cmdargs["from"].value);
                    }
                }
                catch {
                    Console.Error.WriteLine("Codepage for From is wrong!");
                    return;
                }
            }
            Encoding cdpgto = null;
            if (cmdargs["to"].set) {
                try {
                    int cdpgnum;
                    if (int.TryParse(cmdargs["to"].value, out cdpgnum)) {
                        cdpgto = Encoding.GetEncoding(cdpgnum);
                    } else {
                        cdpgto = Encoding.GetEncoding(cmdargs["to"].value);
                    }
                }
                catch {
                    Console.Error.WriteLine("Codepage for To is wrong!");
                    return;
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
                        finput.Seek(0, SeekOrigin.Begin);
                    }
                    try {
                        if (finput.Length > 0) {
                            long filesize;
                            using (MemoryStream fmem = Filters(finput, out filesize,
                                linetrim, addline, cdpgfrom, cdpgto, nobom)) {
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
            if (files.Length < 1) {
                Console.Error.WriteLine("No files and arguments!");
                return;
            }
            foreach (string file in files) {
                Console.Write(file);
                try {
                    long filesize;
                    using (MemoryStream fmem = Filters(File.OpenRead(file), out filesize,
                        linetrim, addline, cdpgfrom, cdpgto, nobom)) {
                        if (fmem == null) {
                            Console.WriteLine(" - Binary");
                            continue;
                        }
                        if (fmem.Length == filesize) {
                            continue;
                        }
                        if (nobackup == false) {
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

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fileSize"></param>
        /// <param name="linetrim"></param>
        /// <param name="addline"></param>
        /// <param name="codepagefrom"></param>
        /// <param name="codepageto"></param>
        /// <param name="nobom"> </param>
        /// <returns>null - binary</returns>
        public static MemoryStream Filters(Stream stream, out long fileSize, bool linetrim, bool addline,
            Encoding codepagefrom, Encoding codepageto = null, bool nobom = false)
        {
            fileSize = stream.Length;
            Encoding cdpgfrom = codepagefrom ?? GetEncodingStream(stream);
            if (cdpgfrom == null) {
                return null;
            }
            MemoryStream fmem = new MemoryStream();
            StreamWriter fmemtxt = new StreamWriter(fmem, codepageto ?? new UTF8Encoding(true));
            if (nobom) {
                fmemtxt.WriteLine();
                fmemtxt.Flush();
                fmemtxt.BaseStream.Seek(0, SeekOrigin.Begin);
                fmem.SetLength(0);
            }
            using (StreamReader ffrom = new StreamReader(stream, cdpgfrom)) {
                int seekbom = cdpgfrom.GetPreamble().Length;
                if (codepagefrom != null) {
                    Encoding testbom = GetEncodingStream(stream);
                    if (testbom == null || testbom == Encoding.Default) {
                        seekbom = 0;
                    }
                }
                string newLine = "\r\n";
                string line;
                if ((line = ffrom.ReadLine()) != null) {
                    ffrom.DiscardBufferedData();
                    ffrom.BaseStream.Seek(seekbom + cdpgfrom.GetByteCount(line), SeekOrigin.Begin);
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
                        if (linetrim) {
                            line = line.TrimEnd();
                        }
                        if (addline == false && ffrom.EndOfStream) {
                            int cnl = cdpgfrom.GetByteCount(newLine);
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
                        fmemtxt.Write(line);
                    } while ((line = ffrom.ReadLine()) != null);
                }
            }
            fmemtxt.Flush();
            fmem.Seek(0, SeekOrigin.Begin);
            return fmem;
        }

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>null - binary</returns>
        public static Encoding GetEncodingStream(Stream stream)
        {
            BinaryReader bin = new BinaryReader(stream);
            byte[] bom = new byte[4];
            bin.BaseStream.Seek(0, SeekOrigin.Begin);
            bin.BaseStream.Read(bom, 0, bom.Length);
            bin.BaseStream.Seek(0, SeekOrigin.Begin);
            if (bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF) {
                return new UTF32Encoding(true, true); // UTF-32, big-endian
            } else if (bom[0] == 0xFE && bom[1] == 0xFF) {
                return new UnicodeEncoding(true, true); // UTF-16, big-endian
            } else if (bom[0] == 0xFF && bom[1] == 0xFE) {
                if (bom[2] == 0x00 && bom[2] == 0x00) {
                    return new UTF32Encoding(false, true); // UTF-32, little-endian
                } else {
                    return new UnicodeEncoding(false, true); // UTF-16, little-endian
                }
            } else if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) {
                return new UTF8Encoding(true);
            } else {
                bool binary = false;
                long fsize = bin.BaseStream.Length;
                if (fsize > 100000) {
                    fsize = 100000;
                }
                byte[] bts = new byte[fsize];
                bin.BaseStream.Seek(0, SeekOrigin.Begin);
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
