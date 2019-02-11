﻿using System;
using System.IO;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace MSCDevHelper
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    //[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(BuildCommandPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class BuildCommandPackage : AsyncPackage
    {
        /// <summary>
        /// BuildCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "a466c0c2-6778-46cd-8eb4-2e8fc1399c6f";

        private EnvDTE80.DTE2 _dte;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildCommandPackage"/> class.
        /// </summary>
        public BuildCommandPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        public EnvDTE80.DTE2 GetDTE()
        {
            if (_dte == null)
            {
                ThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    _dte = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
                });
            }

            return _dte;
        }

        public bool IsOpenSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE80.DTE2 dte = GetDTE();
            if (dte != null)
            {
                if (!String.IsNullOrEmpty(dte.Solution.FullName))
                {
                    //  DO NOT check solution name any longer
                    //string solutionName = System.IO.Path.GetFileName(dte.Solution.FullName);
                    //if (!String.IsNullOrEmpty(solutionName))
                    //{
                    //    return solutionName.StartsWith("Adams");
                    //}
                    return true;
                }
            }

            return false;
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

        public string getSolutionRootDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE80.DTE2 dte = GetDTE();
            if (dte != null)
            {
                if (dte.Solution.FullName.Length > 0)
                {
                    string solutionPath = Path.GetDirectoryName(dte.Solution.FullName);
                    string pluginPath = FindDirectoryInUpstream(solutionPath, "Plugins");
                    string sandDir = Directory.GetParent(pluginPath).FullName;
                    if (Directory.Exists(sandDir))
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

        public string ExpandRelativePath(string relativePath)
        {
            string path = "";

            string solutionRoot = getSolutionRootDirectory();
            if (!String.IsNullOrEmpty(solutionRoot))
            {
                path = relativePath.Replace("$(SolutionRoot)", solutionRoot);
                path = path.Replace("\\\\", "\\");
                return path;
            }

            return "";
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await BuildAdamsPlugin.InitializeAsync(this);
            await BuildAdamsPluginUI.InitializeAsync(this);
            await BuildAdamsScripting.InitializeAsync(this);
            await BuildAdamsScons.InitializeAsync(this);
            await LaunchAdams.InitializeAsync(this);
            await SubCommandGenerator.InitializeAsync(this);
        }

        #endregion
    }
}
