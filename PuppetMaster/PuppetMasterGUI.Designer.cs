
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
            this.RunAllButton = new System.Windows.Forms.Button();
            this.LoadFileButton = new System.Windows.Forms.Button();
            this.inputFile = new System.Windows.Forms.TextBox();
            this.ExecuteNextButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // CommandsInputBox
            // 
            this.CommandsInputBox.Location = new System.Drawing.Point(11, 51);
            this.CommandsInputBox.Name = "CommandsInputBox";
            this.CommandsInputBox.Size = new System.Drawing.Size(612, 27);
            this.CommandsInputBox.TabIndex = 0;
            // 
            // OutputBox
            // 
            this.OutputBox.Location = new System.Drawing.Point(11, 133);
            this.OutputBox.Multiline = true;
            this.OutputBox.Name = "OutputBox";
            this.OutputBox.Size = new System.Drawing.Size(612, 494);
            this.OutputBox.TabIndex = 1;
            // 
            // CommandsLabel
            // 
            this.CommandsLabel.AutoSize = true;
            this.CommandsLabel.Location = new System.Drawing.Point(13, 13);
            this.CommandsLabel.Name = "CommandsLabel";
            this.CommandsLabel.Size = new System.Drawing.Size(112, 20);
            this.CommandsLabel.TabIndex = 2;
            this.CommandsLabel.Text = "New Command";
            // 
            // OutputLabel
            // 
            this.OutputLabel.AutoSize = true;
            this.OutputLabel.Location = new System.Drawing.Point(13, 101);
            this.OutputLabel.Name = "OutputLabel";
            this.OutputLabel.Size = new System.Drawing.Size(104, 20);
            this.OutputLabel.TabIndex = 3;
            this.OutputLabel.Text = "Debug Output";
            // 
            // RunButton
            // 
            this.RunButton.Location = new System.Drawing.Point(629, 51);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(94, 27);
            this.RunButton.TabIndex = 4;
            this.RunButton.Text = "Execute";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.ExecuteButton_Click);
            // 
            // RunAllButton
            // 
            this.RunAllButton.Location = new System.Drawing.Point(1105, 51);
            this.RunAllButton.Name = "RunAllButton";
            this.RunAllButton.Size = new System.Drawing.Size(94, 27);
            this.RunAllButton.TabIndex = 5;
            this.RunAllButton.Text = "Run All";
            this.RunAllButton.UseVisualStyleBackColor = true;
            this.RunAllButton.Click += new System.EventHandler(this.RunAllButton_Click);
            // 
            // LoadFileButton
            // 
            this.LoadFileButton.Location = new System.Drawing.Point(795, 51);
            this.LoadFileButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.LoadFileButton.Name = "LoadFileButton";
            this.LoadFileButton.Size = new System.Drawing.Size(86, 27);
            this.LoadFileButton.TabIndex = 6;
            this.LoadFileButton.Text = "Load File";
            this.LoadFileButton.UseVisualStyleBackColor = true;
            this.LoadFileButton.Click += new System.EventHandler(this.LoadFileButton_Click);
            // 
            // inputFile
            // 
            this.inputFile.Location = new System.Drawing.Point(795, 133);
            this.inputFile.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.inputFile.Multiline = true;
            this.inputFile.Name = "inputFile";
            this.inputFile.Size = new System.Drawing.Size(404, 494);
            this.inputFile.TabIndex = 7;
            // 
            // ExecuteNextButton
            // 
            this.ExecuteNextButton.Location = new System.Drawing.Point(935, 51);
            this.ExecuteNextButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ExecuteNextButton.Name = "ExecuteNextButton";
            this.ExecuteNextButton.Size = new System.Drawing.Size(120, 27);
            this.ExecuteNextButton.TabIndex = 8;
            this.ExecuteNextButton.Text = "Execute Next";
            this.ExecuteNextButton.UseVisualStyleBackColor = true;
            this.ExecuteNextButton.Click += new System.EventHandler(this.ExecuteNextButton_Click);
            // 
            // PuppetMasterGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1291, 639);
            this.Controls.Add(this.ExecuteNextButton);
            this.Controls.Add(this.inputFile);
            this.Controls.Add(this.LoadFileButton);
            this.Controls.Add(this.RunAllButton);
            this.Controls.Add(this.RunButton);
            this.Controls.Add(this.OutputLabel);
            this.Controls.Add(this.CommandsLabel);
            this.Controls.Add(this.OutputBox);
            this.Controls.Add(this.CommandsInputBox);
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
        private System.Windows.Forms.Button LoadFileButton;
        private System.Windows.Forms.TextBox inputFile;
        private System.Windows.Forms.Button ExecuteNextButton;
        private System.Windows.Forms.Button RunAllButton;
    }
}

