using Grpc.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    public partial class PuppetMasterGUI : Form
    {
        public const int puppetMasterPort = 10001;

        public PuppetMasterInitializer initializer = new PuppetMasterInitializer();

        public Server server;

        private List<string> commands = new List<string>();
        private int currentCommand = 0;
        public PuppetMasterGUI()
        {
            InitializeComponent();
            server = new Server
            {
                Services = { PuppetMasterService.BindService(new PuppetMasterServer()) },
                Ports = { new ServerPort("localhost", puppetMasterPort, ServerCredentials.Insecure) }
            };
            server.Start();
            initializer.StartPCS();
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            initializer.Execute(CommandsInputBox.Text);
            CommandsInputBox.Clear();
        }

        private void Load_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string strfilename = openFileDialog1.FileName;

                commands = new List<string>();

                foreach (string line in System.IO.File.ReadLines(strfilename))
                {
                    if (line != null) 
                    {
                        commands.Add(line);
                    }
                }

                inputFile.Text = string.Join("\r\n", commands.ToArray());

            }
        }

        private void Execute_Click(object sender, EventArgs e)
        {
            if (currentCommand == commands.Count)
            {
                return;
            }
            initializer.Execute(commands[currentCommand]);
            commands[currentCommand] += " -> done";
            currentCommand += 1;
            inputFile.Text = string.Join("\r\n", commands.ToArray());
        }

        private void RunAll_Click(object sender, EventArgs e)
        {
            initializer.ReadStartupCommands();
        }
    }
}
