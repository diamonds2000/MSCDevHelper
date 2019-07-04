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
    class CustomCommands
    {
        [DataContract]
        internal class CustomMenuItem
        {
            [DataMember(Name = "Name")]
            public string name = "";

            [DataMember(Name = "MenuHierarchy")]
            public string hierarchy = "";

            [DataMember(Name = "Command")]
            public string commandExe = "";

            [DataMember(Name = "Arguments")]
            public string commandArgs = "";

            [DataMember(Name = "WorkingDir")]
            public string workingDir = "";

            [DataMember(Name = "OutputToVS")]
            public string outputToVS = "";

            public bool includeRelPath = false;
        }

        [CollectionDataContract]
        internal class CustomMenu : List<CustomMenuItem>
        {
        }

        private BuildCommandPackage package;

        //private int baseCmdID = 0x0101;
        private Dictionary<string, int> _hierarchy2BaseCmdId = new Dictionary<string, int>
        {
            { "", 0x0101 },
            { "\\", 0x0101 },
            { "ExplorerMenu", 0x0150 },
        };

        private Dictionary<int, CustomMenuItem> _cmdId2MenuItem = new Dictionary<int, CustomMenuItem>();

        public static readonly Guid CommandSet = new Guid("f4378b59-f16c-404a-9357-d23b4faba9f1");

        private List<CustomMenuItem> _customMenu = new List<CustomMenuItem>();

        public static async Task InitializeAsync(BuildCommandPackage package)
        {
            // Switch to the main thread - the call to AddCommand in BuildCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            var instance = new CustomCommands();
            instance.InitMenuItems(package, commandService);
        }

        private void LoadCustomCommand()
        {
            // $(SolutionRoot) which presents in the field Command, Arguments and WorkingDir stands for root directory of the solution.

            string configFile = Path.Combine(this.package.UserDataPath + "\\CustomMenu.json");
            if (File.Exists(configFile))
            {
                using (FileStream fs = new FileStream(configFile, FileMode.Open))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(CustomMenu));
                    _customMenu = ser.ReadObject(fs) as CustomMenu;
                }

                for (int i = 0; i < _customMenu.Count; i++)
                {
                    CustomMenuItem item = _customMenu[i];
                    if (item.name.IndexOf("$(SolutionRoot)") != -1 ||
                        item.commandExe.IndexOf("$(SolutionRoot)") != -1 ||
                        item.commandArgs.IndexOf("$(SolutionRoot)") != -1 ||
                        item.workingDir.IndexOf("$(SolutionRoot)") != -1)
                    {
                        item.includeRelPath = true;
                    }
                }
            }
        }

        int getMenuId(CustomMenuItem item)
        {
            if (_hierarchy2BaseCmdId.ContainsKey(item.hierarchy))
            {
                return _hierarchy2BaseCmdId[item.hierarchy];
            }

            return 0;
        }

        private void InitMenuItems(BuildCommandPackage package, OleMenuCommandService mcs)
        {
            this.package = package;

            LoadCustomCommand();

            foreach (var menuHierarchy in _hierarchy2BaseCmdId.Keys)
            {
                int menuIdx = 0;
                for (int j = 0; j < _customMenu.Count; j++)
                {
                    CustomMenuItem item = _customMenu[j];
                    if (item.hierarchy == menuHierarchy)
                    {
                        int baseCmdId = getMenuId(item);
                        if (baseCmdId != 0)
                        {
                            var cmdID = new CommandID(CommandSet, baseCmdId + menuIdx);
                            var mc = new OleMenuCommand(new EventHandler(OnCommandExec), cmdID);
                            mc.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
                            mcs.AddCommand(mc);
                            _cmdId2MenuItem[cmdID.ID] = item;
                            menuIdx++;
                        }
                    }
                }
            }
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (null != menuCommand)
            {
                if (_cmdId2MenuItem.ContainsKey(menuCommand.CommandID.ID))
                {
                    CustomMenuItem item = _cmdId2MenuItem[menuCommand.CommandID.ID];
                    menuCommand.Text = item.name;
                    menuCommand.Enabled = (!item.includeRelPath || package.IsOpenSolution());
                }
            }
        }

        private void OnCommandExec(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (null != menuCommand)
            {
                if (_cmdId2MenuItem.ContainsKey(menuCommand.CommandID.ID))
                {
                    CustomMenuItem item = _cmdId2MenuItem[menuCommand.CommandID.ID];

                    string exeFile = package.ExpandRelativePath(item.commandExe);
                    string args = package.ExpandRelativePath(item.commandArgs);
                    string workingDir = package.ExpandRelativePath(item.workingDir);

                    CmdHelper cmdHelper = new CmdHelper(this.package);
                    if (item.outputToVS != "")
                    {
                        cmdHelper.setOutputPane(item.outputToVS);
                    }
                    cmdHelper.ExecuteCmd(exeFile, args, workingDir);
                }
            }
        }
    }
};