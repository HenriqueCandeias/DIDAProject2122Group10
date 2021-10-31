
namespace PuppetMaster
{
    partial class PuppetMasterGUI
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.CommandsInputBox = new System.Windows.Forms.TextBox();
            this.OutputBox = new System.Windows.Forms.TextBox();
            this.CommandsLabel = new System.Windows.Forms.Label();
            this.OutputLabel = new System.Windows.Forms.Label();
            this.LoadFileButton = new System.Windows.Forms.Button();
            this.StartButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.FileNameInputBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // CommandsInputBox
            // 
            this.CommandsInputBox.Location = new System.Drawing.Point(12, 133);
            this.CommandsInputBox.Name = "CommandsInputBox";
            this.CommandsInputBox.Size = new System.Drawing.Size(614, 27);
            this.CommandsInputBox.TabIndex = 0;
            // 
            // OutputBox
            // 
            this.OutputBox.Location = new System.Drawing.Point(12, 207);
            this.OutputBox.Multiline = true;
            this.OutputBox.Name = "OutputBox";
            this.OutputBox.Size = new System.Drawing.Size(612, 292);
            this.OutputBox.TabIndex = 1;
            // 
            // CommandsLabel
            // 
            this.CommandsLabel.AutoSize = true;
            this.CommandsLabel.Location = new System.Drawing.Point(14, 99);
            this.CommandsLabel.Name = "CommandsLabel";
            this.CommandsLabel.Size = new System.Drawing.Size(84, 20);
            this.CommandsLabel.TabIndex = 2;
            this.CommandsLabel.Text = "Commands";
            // 
            // OutputLabel
            // 
            this.OutputLabel.AutoSize = true;
            this.OutputLabel.Location = new System.Drawing.Point(13, 176);
            this.OutputLabel.Name = "OutputLabel";
            this.OutputLabel.Size = new System.Drawing.Size(55, 20);
            this.OutputLabel.TabIndex = 3;
            this.OutputLabel.Text = "Output";
            // 
            // LoadFileButton
            // 
            this.LoadFileButton.Location = new System.Drawing.Point(631, 54);
            this.LoadFileButton.Name = "LoadFileButton";
            this.LoadFileButton.Size = new System.Drawing.Size(126, 29);
            this.LoadFileButton.TabIndex = 4;
            this.LoadFileButton.Text = "Load File";
            this.LoadFileButton.UseVisualStyleBackColor = true;
            this.LoadFileButton.Click += new System.EventHandler(this.LoadFile_Click);
            // 
            // StartButton
            // 
            this.StartButton.Location = new System.Drawing.Point(631, 133);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(126, 29);
            this.StartButton.TabIndex = 5;
            this.StartButton.Text = "Run Command";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.RunCommand_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(166, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Enter Startup File Name";
            // 
            // FileNameInputBox
            // 
            this.FileNameInputBox.Location = new System.Drawing.Point(12, 55);
            this.FileNameInputBox.Name = "FileNameInputBox";
            this.FileNameInputBox.Size = new System.Drawing.Size(614, 27);
            this.FileNameInputBox.TabIndex = 6;
            // 
            // PuppetMasterGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(924, 587);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.FileNameInputBox);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.LoadFileButton);
            this.Controls.Add(this.OutputLabel);
            this.Controls.Add(this.CommandsLabel);
            this.Controls.Add(this.OutputBox);
            this.Controls.Add(this.CommandsInputBox);
            this.Name = "PuppetMasterGUI";
            this.Text = "Puppet Master";
            this.Load += new System.EventHandler(this.PuppetMasterGUI_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox CommandsInputBox;
        private System.Windows.Forms.TextBox OutputBox;
        private System.Windows.Forms.Label CommandsLabel;
        private System.Windows.Forms.Label OutputLabel;
        private System.Windows.Forms.Button LoadFileButton;
        private System.Windows.Forms.Button StartButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox FileNameInputBox;
    }
}

