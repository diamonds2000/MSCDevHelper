using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace MSCDevHelper
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class BuildAdamsScripting : CommandBase
    {
        private BuildAdamsScripting(BuildCommandPackage package, OleMenuCommandService commandService) : base(package, commandService, 0x0104)
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
            Instance = new BuildAdamsScripting(package, commandService);
        }

        override protected void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string batFile = this.package.ExpandRelativePath("$(SolutionRoot)\\sand.bat");
            string args = "scons -t adams_scripting";
            CmdHelper cmdHelper = new CmdHelper(this.package);
            cmdHelper.setOutputPane("build");
            cmdHelper.ExecBat(batFile, args);
        }
    }
}
