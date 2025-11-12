namespace Launcher.App.Forms
{
    partial class Loader
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
            ProgressBar.MarqueeAnimationSpeed = 25;
            ProgressBar.Name = "ProgressBar";
            ProgressBar.Size = new Size(410, 32);
            ProgressBar.Step = 1;
            ProgressBar.Style = ProgressBarStyle.Continuous;
            ProgressBar.TabIndex = 0;
            // 
            // PromptMsg
            // 
            PromptMsg.AutoSize = true;
            PromptMsg.Location = new Point(12, 18);
            PromptMsg.Name = "PromptMsg";
            PromptMsg.Size = new Size(180, 17);
            PromptMsg.TabIndex = 1;
            PromptMsg.Text = "正在读取游戏资源文件, 请稍候...";
            // 
            // Loader
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(434, 91);
            ControlBox = false;
            Controls.Add(PromptMsg);
            Controls.Add(ProgressBar);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Loader";
            Text = "加载中";
            Load += OnLoad;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ProgressBar ProgressBar;
        private Label PromptMsg;
    }
}
