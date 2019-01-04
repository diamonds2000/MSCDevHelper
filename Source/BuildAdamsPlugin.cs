using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace MSCDevHelper
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class BuildAdamsPlugin : CommandBase
    {
        private BuildAdamsPlugin(BuildCommandPackage package, OleMenuCommandService commandService) : base(package, commandService, 0x0101)
        {
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(BuildCommandPackage package)
        {
            // Switch to the main thread - the call to AddCommand in BuildCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new BuildAdamsPlugin(package, commandService);
        }

        override protected void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string batFile = this.package.ExpandRelativePath("$(SolutionRoot)\\sand.bat");
            string args = "scons -t adams_plugin";
            CmdHelper cmdHelper = new CmdHelper(this.package);
            cmdHelper.setOutputPane("build");
            cmdHelper.ExecBat(batFile, args);
        }
    }
}
