
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
            this.RunButton = new System.Windows.Forms.Button();
            this.RunAll = new System.Windows.Forms.Button();
            this.loadButton = new System.Windows.Forms.Button();
            this.inputFile = new System.Windows.Forms.TextBox();
            this.Execute = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // CommandsInputBox
            // 
            this.CommandsInputBox.Location = new System.Drawing.Point(10, 38);
            this.CommandsInputBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.CommandsInputBox.Name = "CommandsInputBox";
            this.CommandsInputBox.Size = new System.Drawing.Size(536, 23);
            this.CommandsInputBox.TabIndex = 0;
            // 
            // OutputBox
            // 
            this.OutputBox.Location = new System.Drawing.Point(10, 100);
            this.OutputBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.OutputBox.Multiline = true;
            this.OutputBox.Name = "OutputBox";
            this.OutputBox.Size = new System.Drawing.Size(536, 220);
            this.OutputBox.TabIndex = 1;
            // 
            // CommandsLabel
            // 
            this.CommandsLabel.AutoSize = true;
            this.CommandsLabel.Location = new System.Drawing.Point(11, 10);
            this.CommandsLabel.Name = "CommandsLabel";
            this.CommandsLabel.Size = new System.Drawing.Size(69, 15);
            this.CommandsLabel.TabIndex = 2;
            this.CommandsLabel.Text = "Commands";
            // 
            // OutputLabel
            // 
            this.OutputLabel.AutoSize = true;
            this.OutputLabel.Location = new System.Drawing.Point(11, 76);
            this.OutputLabel.Name = "OutputLabel";
            this.OutputLabel.Size = new System.Drawing.Size(45, 15);
            this.OutputLabel.TabIndex = 3;
            this.OutputLabel.Text = "Output";
            // 
            // RunButton
            // 
            this.RunButton.Location = new System.Drawing.Point(552, 38);
            this.RunButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(82, 22);
            this.RunButton.TabIndex = 4;
            this.RunButton.Text = "Run";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // RunAll
            // 
            this.RunAll.Location = new System.Drawing.Point(968, 25);
            this.RunAll.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.RunAll.Name = "RunAll";
            this.RunAll.Size = new System.Drawing.Size(82, 22);
            this.RunAll.TabIndex = 5;
            this.RunAll.Text = "RunAll";
            this.RunAll.UseVisualStyleBackColor = true;
            this.RunAll.Click += new System.EventHandler(this.RunAll_Click);
            // 
            // loadButton
            // 
            this.loadButton.Location = new System.Drawing.Point(749, 24);
            this.loadButton.Name = "loadButton";
            this.loadButton.Size = new System.Drawing.Size(75, 23);
            this.loadButton.TabIndex = 6;
            this.loadButton.Text = "load";
            this.loadButton.UseVisualStyleBackColor = true;
            this.loadButton.Click += new System.EventHandler(this.Load_Click);
            // 
            // inputFile
            // 
            this.inputFile.Location = new System.Drawing.Point(696, 68);
            this.inputFile.Multiline = true;
            this.inputFile.Name = "inputFile";
            this.inputFile.Size = new System.Drawing.Size(354, 252);
            this.inputFile.TabIndex = 7;
            // 
            // Execute
            // 
            this.Execute.Location = new System.Drawing.Point(858, 24);
            this.Execute.Name = "Execute";
            this.Execute.Size = new System.Drawing.Size(75, 23);
            this.Execute.TabIndex = 8;
            this.Execute.Text = "Execute";
            this.Execute.UseVisualStyleBackColor = true;
            this.Execute.Click += new System.EventHandler(this.Execute_Click);
            // 
            // PuppetMasterGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1130, 338);
            this.Controls.Add(this.Execute);
            this.Controls.Add(this.inputFile);
            this.Controls.Add(this.loadButton);
            this.Controls.Add(this.RunAll);
            this.Controls.Add(this.RunButton);
            this.Controls.Add(this.OutputLabel);
            this.Controls.Add(this.CommandsLabel);
            this.Controls.Add(this.OutputBox);
            this.Controls.Add(this.CommandsInputBox);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "PuppetMasterGUI";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox CommandsInputBox;
        private System.Windows.Forms.TextBox OutputBox;
        private System.Windows.Forms.Label CommandsLabel;
        private System.Windows.Forms.Label OutputLabel;
        private System.Windows.Forms.Button RunButton;
        private System.Windows.Forms.Button loadButton;
        private System.Windows.Forms.TextBox inputFile;
        private System.Windows.Forms.Button Execute;
        private System.Windows.Forms.Button RunAll;
    }
}

