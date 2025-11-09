using Launcher.Properties;
using System.Windows.Forms;

namespace KartRider
{
    partial class Launcher
    {
        private void InitializeComponent()
        {
            Start_Button = new Button();
            GetKart_Button = new Button();
            Setting_Button = new Button();
            button_ToggleTerminal = new Button();
            ConsoleLogger = new Button();
            label_Client = new Label();
            ClientVersion = new Label();
            VersionLabel = new Label();
            GitHub = new Label();
            KartInfo = new Label();
            Launcher_label = new Label();
            SuspendLayout();
            // 
            // Start_Button
            // 
            Start_Button.Location = new System.Drawing.Point(19, 20);
            Start_Button.Name = "Start_Button";
            Start_Button.Size = new System.Drawing.Size(114, 23);
            Start_Button.TabIndex = 0;
            Start_Button.Text = "启动游戏";
            Start_Button.UseVisualStyleBackColor = true;
            Start_Button.Click += Start_Button_Click;
            // 
            // GetKart_Button
            // 
            GetKart_Button.Location = new System.Drawing.Point(19, 49);
            GetKart_Button.Name = "GetKart_Button";
            GetKart_Button.Size = new System.Drawing.Size(114, 23);
            GetKart_Button.TabIndex = 1;
            GetKart_Button.Text = "添加道具";
            GetKart_Button.UseVisualStyleBackColor = true;
            GetKart_Button.Click += GetKart_Button_Click;
            // 
            // Setting_Button
            // 
            Setting_Button.Location = new System.Drawing.Point(19, 78);
            Setting_Button.Name = "Setting_Button";
            Setting_Button.Size = new System.Drawing.Size(114, 23);
            Setting_Button.TabIndex = 2;
            Setting_Button.Text = "设置";
            Setting_Button.UseVisualStyleBackColor = true;
            Setting_Button.Click += Setting_Button_Click;
            // 
            // button_ToggleTerminal
            // 
            button_ToggleTerminal.Location = new System.Drawing.Point(19, 107);
            button_ToggleTerminal.Name = "button_ToggleTerminal";
            button_ToggleTerminal.Size = new System.Drawing.Size(57, 23);
            button_ToggleTerminal.TabIndex = 3;
            button_ToggleTerminal.Text = "控制台";
            button_ToggleTerminal.UseVisualStyleBackColor = true;
            button_ToggleTerminal.Click += button_ToggleTerminal_Click;
            // 
            // ConsoleLogger
            // 
            ConsoleLogger.Location = new System.Drawing.Point(76, 107);
            ConsoleLogger.Name = "ConsoleLogger";
            ConsoleLogger.Size = new System.Drawing.Size(57, 23);
            ConsoleLogger.TabIndex = 4;
            ConsoleLogger.Text = "输出";
            ConsoleLogger.UseVisualStyleBackColor = true;
            ConsoleLogger.Click += ConsoleLogger_Click;
            // 
            // label_Client
            // 
            label_Client.AutoSize = true;
            label_Client.BackColor = System.Drawing.SystemColors.Control;
            label_Client.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label_Client.ForeColor = System.Drawing.Color.Blue;
            label_Client.Location = new System.Drawing.Point(2, 144);
            label_Client.Name = "label_Client";
            label_Client.Size = new System.Drawing.Size(42, 12);
            label_Client.TabIndex = 5;
            label_Client.Text = "Client:";
            label_Client.Click += label_Client_Click;
            // 
            // ClientVersion
            // 
            ClientVersion.AutoSize = true;
            ClientVersion.BackColor = System.Drawing.SystemColors.Control;
            ClientVersion.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            ClientVersion.ForeColor = System.Drawing.Color.Red;
            ClientVersion.Location = new System.Drawing.Point(45, 144);
            ClientVersion.Name = "ClientVersion";
            ClientVersion.Size = new System.Drawing.Size(24, 12);
            ClientVersion.TabIndex = 6;
            ClientVersion.Click += label_Client_Click;
            // 
            // Launcher_label
            // 
            Launcher_label.AutoSize = true;
            Launcher_label.ForeColor = System.Drawing.Color.Blue;
            Launcher_label.Location = new System.Drawing.Point(2, 160);
            Launcher_label.Name = "Launcher_label";
            Launcher_label.Size = new System.Drawing.Size(54, 12);
            Launcher_label.TabIndex = 7;
            Launcher_label.Text = "Launcher:";
            Launcher_label.Click += GitHub_Click;
            // 
            // VersionLabel
            // 
            VersionLabel.AutoSize = true;
            VersionLabel.BackColor = System.Drawing.SystemColors.Control;
            VersionLabel.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            VersionLabel.ForeColor = System.Drawing.Color.Red;
            VersionLabel.Location = new System.Drawing.Point(57, 160);
            VersionLabel.Name = "VersionLabel";
            VersionLabel.Size = new System.Drawing.Size(36, 12);
            VersionLabel.TabIndex = 8;
            VersionLabel.Click += GitHub_Click;
            // 
            // GitHub
            // 
            GitHub.AutoSize = true;
            GitHub.ForeColor = System.Drawing.Color.Blue;
            GitHub.Location = new System.Drawing.Point(213, 144);
            GitHub.Name = "GitHub";
            GitHub.Size = new System.Drawing.Size(41, 12);
            GitHub.TabIndex = 9;
            GitHub.Text = "GitHub";
            GitHub.Click += GitHub_Click;
            // 
            // KartInfo
            // 
            KartInfo.AutoSize = true;
            KartInfo.ForeColor = System.Drawing.Color.Blue;
            KartInfo.Location = new System.Drawing.Point(201, 160);
            KartInfo.Name = "KartInfo";
            KartInfo.Size = new System.Drawing.Size(53, 12);
            KartInfo.TabIndex = 10;
            KartInfo.Text = "KartInfo";
            KartInfo.Click += KartInfo_Click;
            // 
            // Launcher
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Control;
            ClientSize = new System.Drawing.Size(257, 180);
            Controls.Add(Start_Button);
            Controls.Add(GetKart_Button);
            Controls.Add(Setting_Button);
            Controls.Add(button_ToggleTerminal);
            Controls.Add(ConsoleLogger);
            Controls.Add(ClientVersion);
            Controls.Add(label_Client);
            Controls.Add(VersionLabel);
            Controls.Add(Launcher_label);
            Controls.Add(GitHub);
            Controls.Add(KartInfo);
            Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Launcher";
            Icon = Resources.icon;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Launcher";
            FormClosing += OnFormClosing;
            Load += OnLoad;
            ResumeLayout(false);
            PerformLayout();
        }

        private Button Start_Button;
        private Button GetKart_Button;
        private Button Setting_Button;
        private Button button_ToggleTerminal;
        private Button ConsoleLogger;
        private Label label_Client;
        private Label ClientVersion;
        private Label Launcher_label;
        private Label VersionLabel;
        private Label GitHub;
        private Label KartInfo;
    }
}
