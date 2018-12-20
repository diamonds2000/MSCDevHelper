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

        public void ExecBat(string batfile, string args)
        {
            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = batfile;
            si.Arguments = args;
            si.UseShellExecute = true;
            si.WorkingDirectory = getSandDirectory();

            Process.Start(si);
        }
    }
}
