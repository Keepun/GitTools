using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GitLauncher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try {
                string windir = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.System) + "/..");
                string progfiles = ProgramFilesx86();
                string launcher = Assembly.GetExecutingAssembly().Location;
                ProcessStartInfo psi = new ProcessStartInfo();

                if (args.Length == 0) {
                    psi.FileName = launcher;
                    psi.Arguments = "install";
                    psi.Verb = "runas";
                    Process.Start(psi);
                    return;
                }

                if (args.Length == 1) {
                    if (args[0] == "install") {
                        RegistryKey clsroot = Registry.ClassesRoot.CreateSubKey(@"Directory\shell");

                        RegistryKey git_gui = clsroot.CreateSubKey("git_gui");
                        git_gui.SetValue("", "Git &GUI Here", RegistryValueKind.String);
                        git_gui.CreateSubKey("command").SetValue("",
                                                                 "\"" + launcher + "\" gui \"%1\"",
                                                                 RegistryValueKind.String);

                        RegistryKey git_shell = clsroot.CreateSubKey("git_shell");
                        git_shell.SetValue("", "Git Ba&sh Here", RegistryValueKind.String);
                        git_shell.CreateSubKey("command").SetValue("",
                                                                   "\"" + launcher + "\" shell \"%1\"",
                                                                   RegistryValueKind.String);

                        string name = Path.GetFileNameWithoutExtension(launcher);
                        MessageBox.Show(name + " installed", name);
                        return;
                    }
                    throw new Exception("Install?");
                }

                string workdir = args[1];
                if (args[0] == "shell") {
                    psi.FileName = windir + "/SysWOW64/wscript";
                    psi.Arguments = "\"" + progfiles + "/Git/Git Bash.vbs\" \"" + workdir + "\"";
                } else if (args[0] == "gui") {
                    psi.FileName = progfiles + "/Git/bin/wish.exe";
                    psi.Arguments = "\"" + progfiles + "/Git/libexec/git-core/git-gui\" --working-dir " +
                                    "\"" + workdir + "\"";
                } else {
                    throw new Exception("Shell or GUI?");
                }
                psi.WorkingDirectory = workdir;
                psi.EnvironmentVariables.Add("GIT_DIR", (workdir + "/.git").Replace('\\', '/'));
                psi.UseShellExecute = false;
                for (int x = 3; x < args.Length; x++) {
                    psi.Arguments += " \"" + args[x] + "\"";
                }
                Process.Start(psi);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "ERROR");
            }
        }

        private static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))) {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }
    }
}
