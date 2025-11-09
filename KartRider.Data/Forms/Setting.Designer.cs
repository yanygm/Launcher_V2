using System.Windows.Forms;
using Launcher.Properties;

namespace KartRider
{
    partial class Setting
    {
        private void InitializeComponent()
        {
            Speed_comboBox = new ComboBox();
            Speed_label = new Label();
            PlayerName = new TextBox();
            Name_label = new Label();
            ServerIP = new TextBox();
            IP_label = new Label();
            ServerPort = new TextBox();
            Port_label = new Label();
            NgsOn = new CheckBox();
            Save = new Button();
            SuspendLayout();
            // 
            // PlayerName
            // 
            PlayerName.Location = new System.Drawing.Point(54, 20);
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
            // NgsOn
            // 
            NgsOn.AutoSize = true;
            NgsOn.ForeColor = System.Drawing.Color.Blue;
            NgsOn.Location = new System.Drawing.Point(180, 78);
            NgsOn.Name = "NgsOn";
            NgsOn.Size = new System.Drawing.Size(52, 16);
            NgsOn.TabIndex = 5;
            NgsOn.Text = "NgsOn";
            NgsOn.UseVisualStyleBackColor = true;
            // 
            // ServerIP
            // 
            ServerIP.Location = new System.Drawing.Point(54, 49);
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
            ServerPort.Location = new System.Drawing.Point(54, 78);
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
            // Speed_comboBox
            // 
            Speed_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            Speed_comboBox.ForeColor = System.Drawing.Color.Red;
            Speed_comboBox.FormattingEnabled = true;
            Speed_comboBox.Location = new System.Drawing.Point(54, 107);
            Speed_comboBox.Name = "Speed_comboBox";
            Speed_comboBox.Size = new System.Drawing.Size(114, 23);
            Speed_comboBox.TabIndex = 4;
            Speed_comboBox.SelectedIndexChanged += Speed_comboBox_SelectedIndexChanged;
            // 
            // Speed_label
            // 
            Speed_label.AutoSize = true;
            Speed_label.ForeColor = System.Drawing.Color.Blue;
            Speed_label.Location = new System.Drawing.Point(19, 111);
            Speed_label.Name = "Speed_label";
            Speed_label.Size = new System.Drawing.Size(30, 12);
            Speed_label.Text = "速度:";
            // 
            // Save
            // 
            Save.Location = new System.Drawing.Point(180, 107);
            Save.Name = "Save";
            Save.Size = new System.Drawing.Size(75, 23);
            Save.TabIndex = 6;
            Save.Text = "保存";
            Save.UseVisualStyleBackColor = true;
            Save.Click += Save_Click;
            // 
            // Setting
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Control;
            ClientSize = new System.Drawing.Size(273, 150);
            Controls.Add(PlayerName);
            Controls.Add(Name_label);
            Controls.Add(ServerIP);
            Controls.Add(IP_label);
            Controls.Add(ServerPort);
            Controls.Add(Port_label);
            Controls.Add(Speed_comboBox);
            Controls.Add(Speed_label);
            Controls.Add(NgsOn);
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
        private CheckBox NgsOn;
        private Button Save;
        private Label Name_label;
        private Label IP_label;
        private Label Port_label;
        private Label Speed_label;
    }
}
