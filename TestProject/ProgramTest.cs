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

using System.Collections.Generic;
using System.IO;
using System.Text;
using GitFilters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TestProject
{
    /// <summary>
    ///This is a test class for ProgramTest and is intended
    ///to contain all ProgramTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ProgramTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        #region Additional test attributes

        //
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //

        #endregion

        private struct MemStream
        {
            public MemoryStream fmem;
            public Encoding codepage;

            public MemStream(MemoryStream fmem, Encoding codepage)
            {
                this.fmem = fmem;
                this.codepage = codepage;
            }
        };

        [DeploymentItem("GitFilters.exe")]
        [TestMethod()]
        public void Filters()
        {
            string[] newLines = {"\r\n", "\n", "\r"};
            Dictionary<string, MemStream> fmem = new Dictionary<string, MemStream>();
            Encoding[] codepages = new Encoding[]
                                   {
                                       new UnicodeEncoding(false, true),
                                       new UnicodeEncoding(false, false),
                                       new UnicodeEncoding(true, true),
                                       new UnicodeEncoding(true, false),
                                       new UTF32Encoding(false, true),
                                       new UTF32Encoding(false, false),
                                       new UTF32Encoding(true, true),
                                       new UTF32Encoding(true, false),
                                       new UTF8Encoding(true),
                                       new UTF8Encoding(false),
                                       Encoding.Default
                                   };

            foreach (Encoding cdpg in codepages) {
                for (int nl = 0; nl < newLines.Length; nl++) {
                    string txtName = "Test_" + cdpg.EncodingName + "_" + nl +
                        (cdpg.GetPreamble().Length == 0 ? "_nobom" : "") + ".txt";
                    using (StreamWriter txt = new StreamWriter(File.Create(txtName), cdpg)) {
                        string[] lines = {"", "Русский текст и ♕ символ.   ", "  Hello  "};
                        fmem.Add(txtName, new MemStream(new MemoryStream(), cdpg));
                        StreamWriter fmemtxt = new StreamWriter(fmem[txtName].fmem, cdpg);
                        fmemtxt.NewLine = txt.NewLine = newLines[nl];
                        foreach (string ln in lines) {
                            txt.WriteLine(ln);
                            fmemtxt.WriteLine(ln.TrimEnd());
                        }
                        fmemtxt.Flush();
                    }
                    using (Stream fresult = File.OpenWrite("NeedResult" + txtName)) {
                        fmem[txtName].fmem.WriteTo(fresult);
                    }
                }
            }

            using (BinaryWriter bin = new BinaryWriter(File.Create("Test_Binary.txt"))) {
                fmem.Add("Test_Binary.txt", new MemStream(new MemoryStream(), Encoding.Unicode /*fake*/));
                byte[] binbytes = new byte[] {0xAA, 0xBB, 0x00, 0xCC};
                for (int x = 0; x < 34764; x++) {
                    bin.Write(binbytes, 0, binbytes.Length);
                    fmem["Test_Binary.txt"].fmem.Write(binbytes, 0, binbytes.Length);
                }
            }

            foreach (KeyValuePair<string, MemStream> file in fmem) {
                List<string> args = new List<string>(new string[]
                                                     {file.Key, "--linetrim", "--no-backup"});
                args.AddRange(new string[] {"--to", file.Value.codepage.WebName});
                if (file.Value.codepage.GetPreamble().Length == 0) {
                    args.AddRange(new string[] {"--from", file.Value.codepage.WebName});
                    args.Add("--no-bom");
                }
                Program_Accessor.Main(args.ToArray());
                using (BinaryReader bin = new BinaryReader(File.OpenRead(file.Key))) {
                    Assert.IsFalse(bin.BaseStream.Length != file.Value.fmem.Length, "{0} is bad!", file.Key);
                    file.Value.fmem.Seek(0, SeekOrigin.Begin);
                    for (long x = 0; x < bin.BaseStream.Length; x++) {
                        Assert.IsTrue(bin.ReadByte() == file.Value.fmem.ReadByte(), "{0} isn't identical in {1} byte",
                            file.Key, x);
                    }
                }
            }
        }
    }
}
