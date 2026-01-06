using System.Windows.Forms;
using Launcher.Properties;

namespace KartRider
{
    partial class Setting
    {
        private void InitializeComponent()
        {
            Proxy_comboBox = new ComboBox();
            Proxy_label = new Label();
            AiSpeed_comboBox = new ComboBox();
            AiSpeed_label = new Label();
            Speed_comboBox = new ComboBox();
            Speed_label = new Label();
            Version_comboBox = new ComboBox();
            Version_label = new Label();
            PlayerName = new TextBox();
            Name_label = new Label();
            ServerIP = new TextBox();
            IP_label = new Label();
            ServerPort = new TextBox();
            Port_label = new Label();
            NgsOn = new CheckBox();
            PatchUpdate = new CheckBox();
            Save = new Button();
            SuspendLayout();
            // 
            // PlayerName
            // 
            PlayerName.Location = new System.Drawing.Point(68, 20);
            PlayerName.Name = "PlayerName";
            PlayerName.Size = new System.Drawing.Size(114, 23);
            PlayerName.TabIndex = 1;
            // 
            // Name_label
            // 
            Name_label.AutoSize = true;
            Name_label.ForeColor = System.Drawing.Color.Blue;
            Name_label.Location = new System.Drawing.Point(19, 24);
            Name_label.Name = "Name_label";
            Name_label.Size = new System.Drawing.Size(30, 12);
            Name_label.Text = "昵称:";
            // 
            // ServerIP
            // 
            ServerIP.Location = new System.Drawing.Point(68, 49);
            ServerIP.Name = "ServerIP";
            ServerIP.Size = new System.Drawing.Size(114, 23);
            ServerIP.TabIndex = 2;
            ServerIP.Text = "127.0.0.1";
            // 
            // IP_label
            // 
            IP_label.AutoSize = true;
            IP_label.ForeColor = System.Drawing.Color.Blue;
            IP_label.Location = new System.Drawing.Point(19, 53);
            IP_label.Name = "IP_label";
            IP_label.Size = new System.Drawing.Size(30, 12);
            IP_label.Text = "IP:";
            // 
            // ServerPort
            // 
            ServerPort.Location = new System.Drawing.Point(68, 78);
            ServerPort.Name = "ServerPort";
            ServerPort.Size = new System.Drawing.Size(114, 23);
            ServerPort.TabIndex = 3;
            ServerPort.Text = "39312";
            // 
            // Port_label
            // 
            Port_label.AutoSize = true;
            Port_label.ForeColor = System.Drawing.Color.Blue;
            Port_label.Location = new System.Drawing.Point(19, 82);
            Port_label.Name = "Port_label";
            Port_label.Size = new System.Drawing.Size(30, 12);
            Port_label.Text = "端口:";
            // 
            // Version_comboBox
            // 
            Version_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            Version_comboBox.ForeColor = System.Drawing.Color.Red;
            Version_comboBox.FormattingEnabled = true;
            Version_comboBox.Location = new System.Drawing.Point(68, 107);
            Version_comboBox.Name = "Version_comboBox";
            Version_comboBox.Size = new System.Drawing.Size(114, 23);
            Version_comboBox.TabIndex = 4;
            Version_comboBox.SelectedIndexChanged += Version_comboBox_SelectedIndexChanged;
            // 
            // Version_label
            // 
            Version_label.AutoSize = true;
            Version_label.ForeColor = System.Drawing.Color.Blue;
            Version_label.Location = new System.Drawing.Point(19, 111);
            Version_label.Name = "Version_label";
            Version_label.Size = new System.Drawing.Size(30, 12);
            Version_label.Text = "版本:";
            // 
            // Speed_comboBox
            // 
            Speed_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            Speed_comboBox.ForeColor = System.Drawing.Color.Red;
            Speed_comboBox.FormattingEnabled = true;
            Speed_comboBox.Location = new System.Drawing.Point(68, 136);
            Speed_comboBox.Name = "Speed_comboBox";
            Speed_comboBox.Size = new System.Drawing.Size(114, 23);
            Speed_comboBox.TabIndex = 5;
            Speed_comboBox.SelectedIndexChanged += Speed_comboBox_SelectedIndexChanged;
            // 
            // Speed_label
            // 
            Speed_label.AutoSize = true;
            Speed_label.ForeColor = System.Drawing.Color.Blue;
            Speed_label.Location = new System.Drawing.Point(19, 140);
            Speed_label.Name = "Speed_label";
            Speed_label.Size = new System.Drawing.Size(30, 12);
            Speed_label.Text = "速度:";
            // 
            // AiSpeed_comboBox
            // 
            AiSpeed_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            AiSpeed_comboBox.ForeColor = System.Drawing.Color.Red;
            AiSpeed_comboBox.FormattingEnabled = true;
            AiSpeed_comboBox.Location = new System.Drawing.Point(68, 165);
            AiSpeed_comboBox.Name = "AiSpeed_comboBox";
            AiSpeed_comboBox.Size = new System.Drawing.Size(114, 23);
            AiSpeed_comboBox.TabIndex = 6;
            AiSpeed_comboBox.SelectedIndexChanged += AiSpeed_comboBox_SelectedIndexChanged;
            // 
            // AiSpeed_label
            // 
            AiSpeed_label.AutoSize = true;
            AiSpeed_label.ForeColor = System.Drawing.Color.Blue;
            AiSpeed_label.Location = new System.Drawing.Point(19, 169);
            AiSpeed_label.Name = "AiSpeed_label";
            AiSpeed_label.Size = new System.Drawing.Size(30, 12);
            AiSpeed_label.Text = "Ai速度:";
            // 
            // Proxy_comboBox
            // 
            Proxy_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            Proxy_comboBox.ForeColor = System.Drawing.Color.Red;
            Proxy_comboBox.FormattingEnabled = true;
            Proxy_comboBox.Location = new System.Drawing.Point(68, 194);
            Proxy_comboBox.Name = "Proxy_comboBox";
            Proxy_comboBox.Size = new System.Drawing.Size(114, 23);
            Proxy_comboBox.TabIndex = 7;
            Proxy_comboBox.SelectedIndexChanged += Proxy_comboBox_SelectedIndexChanged;
            // 
            // Proxy_label
            // 
            Proxy_label.AutoSize = true;
            Proxy_label.ForeColor = System.Drawing.Color.Blue;
            Proxy_label.Location = new System.Drawing.Point(19, 198);
            Proxy_label.Name = "Proxy_label";
            Proxy_label.Size = new System.Drawing.Size(30, 12);
            Proxy_label.Text = "代理:";
            // 
            // NgsOn
            // 
            NgsOn.AutoSize = true;
            NgsOn.ForeColor = System.Drawing.Color.Blue;
            NgsOn.Location = new System.Drawing.Point(190, 136);
            NgsOn.Name = "NgsOn";
            NgsOn.Size = new System.Drawing.Size(52, 16);
            NgsOn.TabIndex = 8;
            NgsOn.Text = "NgsOn";
            NgsOn.UseVisualStyleBackColor = true;
            // 
            // PatchUpdate
            // 
            PatchUpdate.AutoSize = true;
            PatchUpdate.ForeColor = System.Drawing.Color.Blue;
            PatchUpdate.Location = new System.Drawing.Point(190, 165);
            PatchUpdate.Name = "PatchUpdate";
            PatchUpdate.Size = new System.Drawing.Size(52, 16);
            PatchUpdate.TabIndex = 9;
            PatchUpdate.Text = "更新补丁";
            PatchUpdate.UseVisualStyleBackColor = true;
            // 
            // Save
            // 
            Save.Location = new System.Drawing.Point(190, 194);
            Save.Name = "Save";
            Save.Size = new System.Drawing.Size(75, 23);
            Save.TabIndex = 10;
            Save.Text = "保存";
            Save.UseVisualStyleBackColor = true;
            Save.Click += Save_Click;
            // 
            // Setting
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Control;
            ClientSize = new System.Drawing.Size(273, 229);
            Controls.Add(PlayerName);
            Controls.Add(Name_label);
            Controls.Add(ServerIP);
            Controls.Add(IP_label);
            Controls.Add(ServerPort);
            Controls.Add(Port_label);
            Controls.Add(Speed_comboBox);
            Controls.Add(Speed_label);
            Controls.Add(Version_comboBox);
            Controls.Add(Version_label);
            Controls.Add(AiSpeed_comboBox);
            Controls.Add(AiSpeed_label);
            Controls.Add(Proxy_comboBox);
            Controls.Add(Proxy_label);
            Controls.Add(NgsOn);
            Controls.Add(PatchUpdate);
            Controls.Add(Save);
            Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Setting";
            Icon = Resources.icon;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "设置";
            FormClosing += OnFormClosing;
            Load += OnLoad;
            ResumeLayout(false);
            PerformLayout();
        }

        private TextBox PlayerName;
        private TextBox ServerIP;
        private TextBox ServerPort;
        private ComboBox Speed_comboBox;
        private ComboBox Version_comboBox;
        private ComboBox AiSpeed_comboBox;
        private ComboBox Proxy_comboBox;
        private CheckBox NgsOn;
        private CheckBox PatchUpdate;
        private Button Save;
        private Label Name_label;
        private Label IP_label;
        private Label Port_label;
        private Label Speed_label;
        private Label Version_label;
        private Label AiSpeed_label;
        private Label Proxy_label;
    }
}
