using System.Collections.Generic;
using System.IO;
using System.Text;
using GitLineTrim;
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

        /// <summary>
        ///A test for Main
        ///</summary>
        [TestMethod()]
        [DeploymentItem("GitLineTrim.exe")]
        public void MainTest()
        {
            string[] newLines = {"\r\n", "\n", "\r"};
            Dictionary<string, MemoryStream> fmem = new Dictionary<string, MemoryStream>();
            Dictionary<Encoding, byte[]> codepages = new Dictionary<Encoding, byte[]>();
            codepages.Add(Encoding.Unicode, new byte[] {0xFF, 0xFE});
            codepages.Add(Encoding.BigEndianUnicode, new byte[] {0xFE, 0xFF});
            codepages.Add(Encoding.GetEncoding("UTF-32BE"), new byte[] {0x00, 0x00, 0xFE, 0xFF});
            codepages.Add(Encoding.UTF32, new byte[] {0xFF, 0xFE, 0x00, 0x00});
            codepages.Add(Encoding.UTF8, new byte[] {0xEF, 0xBB, 0xBF});
            codepages.Add(Encoding.Default, new byte[] {});

            foreach (KeyValuePair<Encoding, byte[]> cdpg in codepages) {
                for (int nl = 0; nl < newLines.Length; nl++) {
                    string txtName = "Test_" + cdpg.Key.EncodingName + "_" + nl + ".txt";
                    using (StreamWriter txt = new StreamWriter(File.Create(txtName), cdpg.Key)) {
                        string[] lines = {"", "Русский текст и ♕ символ.   ", "  Hello  "};
                        txt.NewLine = newLines[nl];
                        fmem.Add(txtName, new MemoryStream());
                        fmem[txtName].Write(cdpg.Value, 0, cdpg.Value.Length);
                        foreach (string ln in lines) {
                            txt.WriteLine(ln);
                            byte[] bln = cdpg.Key.GetBytes(ln.TrimEnd() + txt.NewLine);
                            fmem[txtName].Write(bln, 0, bln.Length);
                        }
                    }
                    using (Stream fresult = File.OpenWrite("NeedResult" + txtName)) {
                        fmem[txtName].WriteTo(fresult);
                    }
                }
            }

            using (BinaryWriter bin = new BinaryWriter(File.Create("Test_Binary.txt"))) {
                fmem.Add("Test_Binary.txt", new MemoryStream());
                byte[] binbytes = new byte[] {0xAA, 0xBB, 0x00, 0xCC};
                for (int x = 0; x < 34764; x++) {
                    bin.Write(binbytes, 0, binbytes.Length);
                    fmem["Test_Binary.txt"].Write(binbytes, 0, binbytes.Length);
                }
            }
            foreach (KeyValuePair<string, MemoryStream> file in fmem) {
                Program_Accessor.Main(new string[] {file.Key, "-nobackup"});
                using (BinaryReader bin = new BinaryReader(File.OpenRead(file.Key))) {
                    Assert.IsFalse(bin.BaseStream.Length != file.Value.Length, "{0} is bad!", file.Key);
                    file.Value.Seek(0, SeekOrigin.Begin);
                    for (long x = 0; x < bin.BaseStream.Length; x++) {
                        Assert.IsTrue(bin.ReadByte() == file.Value.ReadByte(), "{0} isn't identical in {1} byte",
                                      file.Key, x);
                    }
                }
            }
        }
    }
}
