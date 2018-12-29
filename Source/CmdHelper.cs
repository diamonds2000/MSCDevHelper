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
        private BuildCommandPackage _package;
        private string _outputPane;

        public CmdHelper(BuildCommandPackage package)
        {
            _package = package;
        }

        private string FindDirectoryInUpstream(string path, string dirName)
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

        public void setOutputPane(string outputPane)
        {
            _outputPane = outputPane;
        }

        public void getSandEnv()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
        }

        public string getSandDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE80.DTE2 dte = _package.GetDTE();
            if (dte != null)
            {
                if (dte.Solution.FullName.Length > 0)
                {
                    string solutionPath = Path.GetDirectoryName(dte.Solution.FullName);
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

        EnvDTE.OutputWindow getOutputWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE80.DTE2 dte = _package.GetDTE();
            if (dte != null)
            {
                EnvDTE.OutputWindow ouputWin = dte.ToolWindows.OutputWindow;
                return ouputWin;
            }

            return null;
        }

        EnvDTE.OutputWindowPane getOutputPane(string outputPane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (String.IsNullOrEmpty(outputPane))
            {
                return null;
            }

            EnvDTE80.DTE2 dte = _package.GetDTE();
            EnvDTE.OutputWindowPane pane = null;
            if (dte != null)
            {
                EnvDTE.OutputWindowPanes panes = dte.ToolWindows.OutputWindow.OutputWindowPanes;

                try
                {
                    pane = panes.Item(outputPane);
                }
                catch (ArgumentException ex)
                {
                    Trace.Fail(ex.Message);
                }
            }
            return pane;
        }

        public void ExecBat(string batfile, string args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Execute(batfile, args, getSandDirectory());
        }

        public void ExecuteConsoleCmd(string command, string workDir)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Execute("cmd.exe", "/c " + command, workDir);
        }

        public void ExecuteCmd(string exe, string args, string workDir)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Execute(exe, args, workDir);
        }

        public void Execute(string exe, string args, string workDir)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Execute(exe, args, workDir, false);
        }

        public void Execute(string exe, string args, string workDir, bool showWindow)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            bool buildPaneIsActive = false;
            EnvDTE.OutputWindow outputWin = getOutputWindow();
            EnvDTE.OutputWindowPane pane = getOutputPane(_outputPane);
            if (outputWin != null && pane != null)
            {
                if (!buildPaneIsActive)
                {
                    outputWin.Parent.Activate();
                    pane.Activate();
                    buildPaneIsActive = true;
                }
                pane.Clear();
            }

            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = workDir + exe;
            si.Arguments = args;
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            si.CreateNoWindow = !showWindow;
            si.WorkingDirectory = workDir;

            Process proc = new Process();
            proc.StartInfo = si;
            proc.OutputDataReceived += new DataReceivedEventHandler(OnOutputDataReceived);
            proc.Start();
            proc.BeginOutputReadLine();
        }

        private async void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

            EnvDTE.OutputWindowPane pane = getOutputPane(_outputPane);
            if (pane != null && !String.IsNullOrEmpty(e.Data))
            {
                pane.OutputString(e.Data + "\r");
            }
        }
    }
}
