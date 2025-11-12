namespace Launcher.App.Forms
{
    partial class Updater
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            ProgressBar = new ProgressBar();
            PromptMsg = new Label();
            SuspendLayout();
            // 
            // ProgressBar
            // 
            ProgressBar.Location = new Point(12, 47);
            ProgressBar.Name = "ProgressBar";
            ProgressBar.Size = new Size(410, 32);
            ProgressBar.Step = 1;
            ProgressBar.Style = ProgressBarStyle.Continuous;
            ProgressBar.TabIndex = 0;
            ProgressBar.Visible = false;
            // 
            // PromptMsg
            // 
            PromptMsg.AutoSize = true;
            PromptMsg.Location = new Point(12, 18);
            PromptMsg.Name = "PromptMsg";
            PromptMsg.Size = new Size(77, 17);
            PromptMsg.TabIndex = 1;
            PromptMsg.Text = "检查更新中...";
            // 
            // Updater
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(434, 91);
            ControlBox = false;
            Controls.Add(PromptMsg);
            Controls.Add(ProgressBar);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Updater";
            Text = "更新";
            Load += OnLoad;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ProgressBar ProgressBar;
        private Label PromptMsg;
    }
}
