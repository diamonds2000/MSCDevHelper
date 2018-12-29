using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;


namespace MSCDevHelper
{
    class SubCommandGenerator
    {
        [DataContract]
        internal class CustomMenuItem
        {
            [DataMember(Name = "Name")]
            public string itemName = "";

            [DataMember(Name = "Command")]
            public string itemCommandExe = "";

            [DataMember(Name = "Arguments")]
            public string itemCommandArgs = "";
        }

        [CollectionDataContract]
        internal class CustomMenu : List<CustomMenuItem>
        {
        }

        private AsyncPackage package;

        private int baseCmdID = 0x105;

        public static readonly Guid CommandSet = new Guid("f4378b59-f16c-404a-9357-d23b4faba9f1");

        private List<CustomMenuItem> customMenu = new List<CustomMenuItem>();

        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in BuildCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            var instance = new SubCommandGenerator();
            instance.InitSubCmdMenu(package, commandService);
        }

        private void LoadSubCommand()
        {
            // json format
            //[
	        //    {"Name":"Command_1","Command":"Explorer.exe","Arguments":"c:\\"},
	        //    {"Name":"Command_2","Command":"Explorer.exe","Arguments":"d:\\"},
	        //    {"Name":"Command_3","Command":"Explorer.exe","Arguments":"e:\\"}
            //]
            using (FileStream fs = new FileStream(@"F:\CustomMenu.json", FileMode.Open))
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(CustomMenu));
                customMenu = ser.ReadObject(fs) as CustomMenu;
            }
        }

        private void InitSubCmdMenu(AsyncPackage package, OleMenuCommandService mcs)
        {
            this.package = package;

            LoadSubCommand();

            for (int i = 0; i < customMenu.Count; i ++)
            {
                CustomMenuItem item = customMenu[i];
                var cmdID = new CommandID(CommandSet, this.baseCmdID + i);
                var mc = new OleMenuCommand(new EventHandler(OnCommandExec), cmdID);
                mc.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
                mcs.AddCommand(mc);
            }
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (null != menuCommand)
            {
                int index = menuCommand.CommandID.ID - this.baseCmdID;
                if (index >= 0 && index < customMenu.Count)
                {
                    menuCommand.Text = customMenu[index].itemName;
                }
            }
        }

        private void OnCommandExec(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (null != menuCommand)
            {
                int index = menuCommand.CommandID.ID - this.baseCmdID;
                if (index >= 0 && index < customMenu.Count)
                {
                    CustomMenuItem item = customMenu[index];

                    string exeFile = item.itemCommandExe;
                    string args = item.itemCommandArgs;
                    CmdHelper cmdHelper = new CmdHelper(this.package);
                    cmdHelper.ExecuteCmd(exeFile, args, "");
                }
            }
        }
    }
};