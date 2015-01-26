﻿/*
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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitUnicode
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try {
                using (MemoryStream finput = new MemoryStream()) {
                    using (Stream bcon = Console.OpenStandardInput()) {
                        byte[] buffer = new byte[100000];
                        int count;
                        while ((count = bcon.Read(buffer, 0, buffer.Length)) > 0) {
                            finput.Write(buffer, 0, count);
                        }
                    }
                    if (finput.Length > 0) {
                        using (MemoryStream fmem = GetUTF8(finput)) {
                            if (fmem == null) {
                                using (Stream bcon = Console.OpenStandardOutput()) {
                                    finput.WriteTo(bcon);
                                }
                                return;
                            }
                            using (Stream bcon = Console.OpenStandardOutput()) {
                                fmem.WriteTo(bcon);
                            }
                        }
                    } else if (args.Length > 0) {
                        using (MemoryStream fmem = GetUTF8(File.OpenRead(args[0]))) {
                            if (fmem == null) {
                                using (Stream bcon = Console.OpenStandardOutput()) {
                                    using (Stream bfile = File.OpenRead(args[0])) {
                                        byte[] buffer = new byte[100000];
                                        int count;
                                        while ((count = bfile.Read(buffer, 0, buffer.Length)) > 0) {
                                            bcon.Write(buffer, 0, count);
                                        }
                                    }
                                }
                                return;
                            }
                            using (Stream bcon = Console.OpenStandardOutput()) {
                                fmem.WriteTo(bcon);
                            }
                        }
                    }
                }
            }
            catch {
            }
        }

        private static MemoryStream GetUTF8(Stream stream)
        {
            byte[] bom;
            int seekbom;
            Encoding codepage = GitLineTrim.Program.GetEncodingStream(stream, out bom, out seekbom);
            if (codepage == null || codepage == Encoding.Default) {
                return null;
            }
            MemoryStream fmem = new MemoryStream();
            fmem.Write(new byte[] {0xEF, 0xBB, 0xBF}, 0, 3);
            using (StreamReader ffrom = new StreamReader(stream, codepage)) {
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
                        byte[] bline = Encoding.UTF8.GetBytes(line);
                        fmem.Write(bline, 0, bline.Length);
                    } while ((line = ffrom.ReadLine()) != null);
                }
            }
            fmem.Seek(0, SeekOrigin.Begin);
            return fmem;
        }
    }
}
