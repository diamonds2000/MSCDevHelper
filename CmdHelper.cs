using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;


namespace MSCDevHelper
{
    class CmdHelper
    {
        private EnvDTE80.DTE2 _dte;

        public CmdHelper(AsyncPackage package)
        {
            if (_dte == null)
            {
                package.GetServiceAsync(typeof(EnvDTE.DTE)).Wait();
                _dte = package.GetServiceAsync(typeof(EnvDTE.DTE)).GetAwaiter().GetResult() as EnvDTE80.DTE2;
            }
        }

        public string FindDirectoryInUpstream(string path, string dirName)
        {
            string ret = String.Empty;
            DirectoryInfo di = new DirectoryInfo(path);
            while (di != null)
            {
                if (String.Compare(di.Name, dirName, true) == 0)
                {
                    ret = di.FullName;
                    break;
                }
                di = di.Parent;
            }

            return ret;
        }

        public void getSandEnv()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
        }

        public string getSandDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte != null)
            {
                if (_dte.Solution.FullName.Length > 0)
                {
                    string solutionPath = Path.GetDirectoryName(_dte.Solution.FullName);
                    string pluginPath = FindDirectoryInUpstream(solutionPath, "Plugins");
                    string sandDir = Path.Combine(pluginPath, @"..\");
                    DirectoryInfo di = new DirectoryInfo(sandDir);
                    if (di.Exists)
                    {
                        return sandDir;
                    }
                }
                else
                {
                    //Assert
                }
            }
            return "";
        }

        EnvDTE.OutputWindowPane getBuildOutputPane()
        {
            EnvDTE.OutputWindowPane pane = null;
            if (_dte != null)
            {
                EnvDTE.OutputWindowPanes panes = _dte.ToolWindows.OutputWindow.OutputWindowPanes;
                pane = panes.Item("生成");
                if (pane == null)
                {
                    pane = _dte.ToolWindows.OutputWindow.OutputWindowPanes.Item("build");
                }
            }
            return pane;
        }

        public void ExecBat(string batfile, string args)
        {
            Execute(batfile, args, getSandDirectory());
        }

        public void ExecuteCmd(string command, string workDir)
        {
            Execute("cmd.exe", "/c " + command, workDir);
        }

        public void Execute(string exe, string args, string workDir)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            bool buildPaneIsActive = false;
            EnvDTE.OutputWindowPane pane = getBuildOutputPane();
            if (!buildPaneIsActive)
            {
                pane.Activate();
                buildPaneIsActive = true;
            }

            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = exe;
            si.Arguments = args;
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            if (workDir.Length > 0)
            {
                si.WorkingDirectory = workDir;
            }

            Process proc = new Process();
            proc.StartInfo = si;
            proc.OutputDataReceived += new DataReceivedEventHandler(OnOutputDataReceived);
            proc.Start();
            proc.BeginOutputReadLine();
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            EnvDTE.OutputWindowPane pane = getBuildOutputPane();
            if (pane != null && !String.IsNullOrEmpty(e.Data))
            {
                pane.OutputString(e.Data + "\r");
            }
        }
    }
}
