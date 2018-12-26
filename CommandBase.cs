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
    internal abstract class CommandBase
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public int CommandId = 0;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("f4378b59-f16c-404a-9357-d23b4faba9f1");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        protected readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBase"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        protected CommandBase(AsyncPackage package, OleMenuCommandService commandService, int commandId)
        {
            this.CommandId = commandId;
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, commandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            //menuItem.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CommandBase Instance
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        protected Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        protected virtual void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            int a = 0;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        protected abstract void Execute(object sender, EventArgs e);
    }
}
