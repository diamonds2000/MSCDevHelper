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
    internal sealed class BuildAdamsPluginUI : CommandBase
    {
        private BuildAdamsPluginUI(BuildCommandPackage package, OleMenuCommandService commandService) : base(package, commandService, 0x0102)
        {
        }
 
        public static async Task InitializeAsync(BuildCommandPackage package)
        {
            // Switch to the main thread - the call to AddCommand in BuildCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new BuildAdamsPluginUI(package, commandService);
        }

        override protected void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string batFile = this.package.ExpandRelativePath("$(SolutionRoot)\\sand.bat");
            string args = "scons -t uiadams_plugin";
            CmdHelper cmdHelper = new CmdHelper(this.package);
            cmdHelper.setOutputPane("build");
            cmdHelper.ExecBat(batFile, args);
        }
    }
}
