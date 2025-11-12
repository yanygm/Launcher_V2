namespace KartRider
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            Start_Button = new Button();
            GetKart_Button = new Button();
            ConsoleLogger = new Button();
            label_Client = new Label();
            ClientVersion = new Label();
            VersionLabel = new Label();
            Launcher_label = new Label();
            Speed_comboBox = new ComboBox();
            Speed_label = new Label();
            GitHub = new Label();
            KartInfo = new Label();
            label_Docs = new Label();
            label_TimeAttackLog = new Label();
            button_More_Options = new Button();
            SuspendLayout();
            // 
            // Start_Button
            // 
            Start_Button.Location = new Point(12, 12);
            Start_Button.Name = "Start_Button";
            Start_Button.Size = new Size(200, 25);
            Start_Button.TabIndex = 364;
            Start_Button.Text = "启动游戏";
            Start_Button.UseVisualStyleBackColor = true;
            Start_Button.Click += Start_Button_Click;
            // 
            // GetKart_Button
            // 
            GetKart_Button.Location = new Point(13, 43);
            GetKart_Button.Name = "GetKart_Button";
            GetKart_Button.Size = new Size(200, 25);
            GetKart_Button.TabIndex = 365;
            GetKart_Button.Text = "添加道具";
            GetKart_Button.UseVisualStyleBackColor = true;
            GetKart_Button.Click += GetKart_Button_Click;
            // 
            // ConsoleLogger
            // 
            ConsoleLogger.Location = new Point(12, 74);
            ConsoleLogger.Name = "ConsoleLogger";
            ConsoleLogger.Size = new Size(199, 23);
            ConsoleLogger.TabIndex = 375;
            ConsoleLogger.Text = "打日志";
            ConsoleLogger.UseVisualStyleBackColor = true;
            ConsoleLogger.Click += ConsoleLogger_Click;
            // 
            // label_Client
            // 
            label_Client.AutoSize = true;
            label_Client.BackColor = SystemColors.Control;
            label_Client.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label_Client.ForeColor = Color.Blue;
            label_Client.Location = new Point(2, 180);
            label_Client.Name = "label_Client";
            label_Client.Size = new Size(71, 12);
            label_Client.TabIndex = 367;
            label_Client.Text = "游戏版本  :";
            // 
            // ClientVersion
            // 
            ClientVersion.AutoSize = true;
            ClientVersion.BackColor = SystemColors.Control;
            ClientVersion.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ClientVersion.ForeColor = Color.Red;
            ClientVersion.Location = new Point(0, 0);
            ClientVersion.Name = "ClientVersion";
            ClientVersion.Size = new Size(0, 12);
            ClientVersion.TabIndex = 367;
            ClientVersion.Click += label_Client_Click;
            ClientVersion.MouseEnter += ClientVersion_MouseEnter;
            // 
            // VersionLabel
            // 
            VersionLabel.AutoSize = true;
            VersionLabel.BackColor = SystemColors.Control;
            VersionLabel.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            VersionLabel.ForeColor = Color.Red;
            VersionLabel.Location = new Point(0, 0);
            VersionLabel.Name = "VersionLabel";
            VersionLabel.Size = new Size(0, 12);
            VersionLabel.TabIndex = 373;
            VersionLabel.Click += GitHub_Release_Click;
            VersionLabel.MouseEnter += VersionLabel_MouseEnter;
            // 
            // Launcher_label
            // 
            Launcher_label.AutoSize = true;
            Launcher_label.ForeColor = Color.Blue;
            Launcher_label.Location = new Point(2, 193);
            Launcher_label.Name = "Launcher_label";
            Launcher_label.Size = new Size(71, 12);
            Launcher_label.TabIndex = 373;
            Launcher_label.Text = "启动器版本:";
            // 
            // Speed_comboBox
            // 
            Speed_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            Speed_comboBox.ForeColor = Color.Red;
            Speed_comboBox.FormattingEnabled = true;
            Speed_comboBox.Location = new Point(76, 103);
            Speed_comboBox.Name = "Speed_comboBox";
            Speed_comboBox.Size = new Size(135, 20);
            Speed_comboBox.TabIndex = 368;
            Speed_comboBox.SelectedIndexChanged += Speed_comboBox_SelectedIndexChanged;
            // 
            // Speed_label
            // 
            Speed_label.AutoSize = true;
            Speed_label.Font = new Font("宋体", 9F);
            Speed_label.ForeColor = Color.Blue;
            Speed_label.Location = new Point(13, 109);
            Speed_label.Name = "Speed_label";
            Speed_label.Size = new Size(65, 12);
            Speed_label.TabIndex = 369;
            Speed_label.Text = "速度选择: ";
            // 
            // GitHub
            // 
            GitHub.AutoSize = true;
            GitHub.ForeColor = Color.Blue;
            GitHub.Location = new Point(124, 181);
            GitHub.Name = "GitHub";
            GitHub.Size = new Size(89, 12);
            GitHub.TabIndex = 371;
            GitHub.Text = "GitHub源码仓库";
            GitHub.Click += GitHub_Click;
            GitHub.MouseEnter += GitHub_MouseEnter;
            // 
            // KartInfo
            // 
            KartInfo.AutoSize = true;
            KartInfo.ForeColor = Color.Blue;
            KartInfo.Location = new Point(159, 193);
            KartInfo.Name = "KartInfo";
            KartInfo.Size = new Size(53, 12);
            KartInfo.TabIndex = 372;
            KartInfo.Text = "KartInfo";
            KartInfo.Click += KartInfo_Click;
            KartInfo.MouseEnter += KartInfo_MouseEnter;
            // 
            // label_Docs
            // 
            label_Docs.AutoSize = true;
            label_Docs.ForeColor = Color.Blue;
            label_Docs.Location = new Point(135, 169);
            label_Docs.Name = "label_Docs";
            label_Docs.Size = new Size(77, 12);
            label_Docs.TabIndex = 374;
            label_Docs.Text = "线上说明文档";
            label_Docs.Click += label_Docs_Click;
            // 
            // label_TimeAttackLog
            // 
            label_TimeAttackLog.AutoSize = true;
            label_TimeAttackLog.ForeColor = Color.Blue;
            label_TimeAttackLog.Location = new Point(135, 157);
            label_TimeAttackLog.Name = "label_TimeAttackLog";
            label_TimeAttackLog.Size = new Size(77, 12);
            label_TimeAttackLog.TabIndex = 375;
            label_TimeAttackLog.Text = "查看计时记录";
            label_TimeAttackLog.Click += label_TimeAttackLog_Click;
            // 
            // button_More_Options
            // 
            button_More_Options.Location = new Point(13, 129);
            button_More_Options.Name = "button_More_Options";
            button_More_Options.Size = new Size(200, 25);
            button_More_Options.TabIndex = 378;
            button_More_Options.Text = "更多选项";
            button_More_Options.UseVisualStyleBackColor = true;
            button_More_Options.Click += button_More_Options_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(224, 214);
            Controls.Add(button_More_Options);
            Controls.Add(label_TimeAttackLog);
            Controls.Add(label_Docs);
            Controls.Add(VersionLabel);
            Controls.Add(Launcher_label);
            Controls.Add(KartInfo);
            Controls.Add(GitHub);
            Controls.Add(Speed_comboBox);
            Controls.Add(Speed_label);
            Controls.Add(ClientVersion);
            Controls.Add(label_Client);
            Controls.Add(ConsoleLogger);
            Controls.Add(GetKart_Button);
            Controls.Add(Start_Button);
            Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "MainForm";
            Text = "启动器";
            TopMost = true;
            FormClosing += OnFormClosing;
            Load += OnLoad;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label_Client;
        private Label Speed_label;
        private Label GitHub;
        private Label KartInfo;
        private Label Launcher_label;
        private Label ClientVersion;
        private Label label_Docs;
        private Label label_TimeAttackLog;
        private Label VersionLabel;
        private Button Start_Button;
        private Button GetKart_Button;
        private Button ConsoleLogger;
        private Button button_More_Options;
        private ComboBox Speed_comboBox;
    }
}
