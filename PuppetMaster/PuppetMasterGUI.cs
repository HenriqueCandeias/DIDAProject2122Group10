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
        public PuppetMasterInitializer initializer;

        public Server server;

        private List<string> commands = new List<string>();
        private int currentCommand = 0;

        string fileName = null;

        public PuppetMasterGUI()
        {
            InitializeComponent();

            initializer = new PuppetMasterInitializer(this);

            //This function is only used to run the system in a single machine
            initializer.StartPCS();
        }

        private void ExecuteButton_Click(object sender, EventArgs e)
        {
            inputFile.Text = "";
            initializer.Execute(CommandsInputBox.Text);
            CommandsInputBox.Clear();
        }

        private void ExecuteNextButton_Click(object sender, EventArgs e)
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

        private void LoadFileButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fileName = openFileDialog1.FileName;

                commands = new List<string>();
                currentCommand = 0;

                foreach (string line in System.IO.File.ReadLines(fileName))
                {
                    if (line != null)
                    {
                        commands.Add(line);
                    }
                }

                inputFile.Text = string.Join("\r\n", commands.ToArray());
            }
        }

        private void RunAllButton_Click(object sender, EventArgs e)
        {
            inputFile.Text = "";

            if (fileName == null)
            {
                inputFile.Text = "Error: you have to load a file with commands to execute.";
                return;
            }

            foreach (string line in System.IO.File.ReadLines(fileName))
            {
                initializer.Execute(line);
            }

            inputFile.Text = "All commands applied sucessfully.\r\n";
        }

        public void PrintDebugMessage(string debugMessage)
        {
            OutputBox.Text += "\r\n" + debugMessage + "\r\n";
        }
    }
}
