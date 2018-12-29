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
    internal sealed class BuildAdamsScons : CommandBase
    {
        private BuildAdamsScons(BuildCommandPackage package, OleMenuCommandService commandService) : base(package, commandService, 0x0103)
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
            Instance = new BuildAdamsScons(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        override protected void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string batFile = "sand.bat";
            string args = "adamsscons";
            CmdHelper cmdHelper = new CmdHelper(this.package);
            cmdHelper.setOutputPane("build"); 
            cmdHelper.ExecBat(batFile, args);
        }
    }
}
