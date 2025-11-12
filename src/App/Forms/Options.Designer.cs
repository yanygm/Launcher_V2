namespace KartRider
{
    partial class Options
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
            tab_Options = new TabControl();
            tabPage_LauncherOptions = new TabPage();
            checkBox_AutoUpdate = new CheckBox();
            button_CheckUpdate = new Button();
            button_ToggleShowPacketDetail = new Button();
            button_ToggleConsole = new Button();
            tabPage_InGameOptions = new TabPage();
            button_ForceLeaveRoom = new Button();
            button_KillGameProcesses = new Button();
            tab_Options.SuspendLayout();
            tabPage_LauncherOptions.SuspendLayout();
            tabPage_InGameOptions.SuspendLayout();
            SuspendLayout();
            // 
            // tab_Options
            // 
            tab_Options.Controls.Add(tabPage_LauncherOptions);
            tab_Options.Controls.Add(tabPage_InGameOptions);
            tab_Options.Location = new Point(12, 12);
            tab_Options.Name = "tab_Options";
            tab_Options.SelectedIndex = 0;
            tab_Options.Size = new Size(220, 237);
            tab_Options.TabIndex = 1;
            // 
            // tabPage_LauncherOptions
            // 
            tabPage_LauncherOptions.Controls.Add(checkBox_AutoUpdate);
            tabPage_LauncherOptions.Controls.Add(button_CheckUpdate);
            tabPage_LauncherOptions.Controls.Add(button_ToggleShowPacketDetail);
            tabPage_LauncherOptions.Controls.Add(button_ToggleConsole);
            tabPage_LauncherOptions.Location = new Point(4, 26);
            tabPage_LauncherOptions.Name = "tabPage_LauncherOptions";
            tabPage_LauncherOptions.Padding = new Padding(3);
            tabPage_LauncherOptions.Size = new Size(212, 207);
            tabPage_LauncherOptions.TabIndex = 1;
            tabPage_LauncherOptions.Text = "启动器选项";
            tabPage_LauncherOptions.UseVisualStyleBackColor = true;
            // 
            // checkBox_AutoUpdate
            // 
            checkBox_AutoUpdate.AutoSize = true;
            checkBox_AutoUpdate.Location = new Point(107, 72);
            checkBox_AutoUpdate.Name = "checkBox_AutoUpdate";
            checkBox_AutoUpdate.Size = new Size(99, 21);
            checkBox_AutoUpdate.TabIndex = 381;
            checkBox_AutoUpdate.Text = "自动检查更新";
            checkBox_AutoUpdate.UseVisualStyleBackColor = true;
            checkBox_AutoUpdate.CheckedChanged += checkBox_AutoUpdate_CheckedChanged;
            checkBox_AutoUpdate.MouseEnter += checkBox_AutoUpdate_MouseEnter;
            // 
            // button_CheckUpdate
            // 
            button_CheckUpdate.Location = new Point(6, 68);
            button_CheckUpdate.Name = "button_CheckUpdate";
            button_CheckUpdate.Size = new Size(95, 25);
            button_CheckUpdate.TabIndex = 380;
            button_CheckUpdate.Text = "检查更新";
            button_CheckUpdate.UseVisualStyleBackColor = true;
            button_CheckUpdate.Click += button_CheckUpdate_Click;
            // 
            // button_ToggleShowPacketDetail
            // 
            button_ToggleShowPacketDetail.Location = new Point(6, 37);
            button_ToggleShowPacketDetail.Name = "button_ToggleShowPacketDetail";
            button_ToggleShowPacketDetail.Size = new Size(200, 25);
            button_ToggleShowPacketDetail.TabIndex = 379;
            button_ToggleShowPacketDetail.Text = "切换输出数据包内容";
            button_ToggleShowPacketDetail.UseVisualStyleBackColor = true;
            button_ToggleShowPacketDetail.Click += button_ToggleShowPacketDetail_Click;
            button_ToggleShowPacketDetail.MouseEnter += button_ToggleShowPacketDetail_MouseEnter;
            // 
            // button_ToggleConsole
            // 
            button_ToggleConsole.Location = new Point(6, 6);
            button_ToggleConsole.Name = "button_ToggleConsole";
            button_ToggleConsole.Size = new Size(200, 25);
            button_ToggleConsole.TabIndex = 378;
            button_ToggleConsole.Text = "切换终端";
            button_ToggleConsole.UseVisualStyleBackColor = true;
            button_ToggleConsole.Click += button_ToggleConsole_Click;
            button_ToggleConsole.MouseEnter += button_ToggleConsole_MouseEnter;
            // 
            // tabPage_InGameOptions
            // 
            tabPage_InGameOptions.Controls.Add(button_ForceLeaveRoom);
            tabPage_InGameOptions.Controls.Add(button_KillGameProcesses);
            tabPage_InGameOptions.Location = new Point(4, 26);
            tabPage_InGameOptions.Name = "tabPage_InGameOptions";
            tabPage_InGameOptions.Padding = new Padding(3);
            tabPage_InGameOptions.Size = new Size(212, 207);
            tabPage_InGameOptions.TabIndex = 0;
            tabPage_InGameOptions.Text = "游戏选项";
            tabPage_InGameOptions.UseVisualStyleBackColor = true;
            // 
            // button_ForceLeaveRoom
            // 
            button_ForceLeaveRoom.Location = new Point(6, 37);
            button_ForceLeaveRoom.Name = "button_ForceLeaveRoom";
            button_ForceLeaveRoom.Size = new Size(200, 25);
            button_ForceLeaveRoom.TabIndex = 378;
            button_ForceLeaveRoom.Text = "强制离开多人对战房间";
            button_ForceLeaveRoom.UseVisualStyleBackColor = true;
            button_ForceLeaveRoom.Click += button_ForceLeaveRoom_Click;
            // 
            // button_KillGameProcesses
            // 
            button_KillGameProcesses.Location = new Point(6, 6);
            button_KillGameProcesses.Name = "button_KillGameProcesses";
            button_KillGameProcesses.Size = new Size(200, 25);
            button_KillGameProcesses.TabIndex = 377;
            button_KillGameProcesses.Text = "强制关闭游戏";
            button_KillGameProcesses.UseVisualStyleBackColor = true;
            button_KillGameProcesses.Click += button_KillGameProcesses_Click;
            // 
            // Options
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(244, 261);
            Controls.Add(tab_Options);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "Options";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Options";
            TopMost = true;
            tab_Options.ResumeLayout(false);
            tabPage_LauncherOptions.ResumeLayout(false);
            tabPage_LauncherOptions.PerformLayout();
            tabPage_InGameOptions.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TabControl tab_Options;
        private TabPage tabPage_InGameOptions;
        private TabPage tabPage_LauncherOptions;
        private Button button_ToggleConsole;
        private Button button_KillGameProcesses;
        private Button button_ToggleShowPacketDetail;
        private Button button_ForceLeaveRoom;
        private Button button_CheckUpdate;
        private CheckBox checkBox_AutoUpdate;
    }
}
