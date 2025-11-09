using System;
using System.Linq;
using System.Windows.Forms;
using ExcData;
using Profile;

namespace KartRider
{
    public partial class Setting : Form
    {
        public Setting()
        {
            InitializeComponent();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            ProfileService.SettingConfig.Name = PlayerName.Text;
            ProfileService.SettingConfig.ServerIP = ServerIP.Text;
            ProfileService.SettingConfig.ServerPort = ushort.Parse(ServerPort.Text);
            ProfileService.SettingConfig.NgsOn = NgsOn.Checked;
            ProfileService.SettingConfig.SpeedType = SpeedType.speedNames[Speed_comboBox.Text];
            ProfileService.SaveSettings();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            PlayerName.Text = ProfileService.SettingConfig.Name;
            ServerIP.Text = ProfileService.SettingConfig.ServerIP;
            ServerPort.Text = ProfileService.SettingConfig.ServerPort.ToString();
            NgsOn.Checked = ProfileService.SettingConfig.NgsOn;
            ProfileService.SaveSettings();
            foreach (string key in SpeedType.speedNames.Keys)
            {
                Speed_comboBox.Items.Add(key);
            }
            Speed_comboBox.Text = (SpeedType.speedNames.FirstOrDefault(x => x.Value == ProfileService.SettingConfig.SpeedType).Key);
        }

        private void Speed_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Speed_comboBox.SelectedItem != null)
            {
                string selectedSpeed = Speed_comboBox.SelectedItem.ToString();
                if (SpeedType.speedNames.ContainsKey(selectedSpeed))
                {
                    ProfileService.SettingConfig.SpeedType = SpeedType.speedNames[selectedSpeed];
                    ProfileService.SaveSettings();
                    Console.WriteLine(selectedSpeed);
                }
                else
                {
                    Console.WriteLine("Invalid speed type selected.");
                }
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            ProfileService.SettingConfig.Name = PlayerName.Text;
            ProfileService.SettingConfig.ServerIP = ServerIP.Text;
            ProfileService.SettingConfig.ServerPort = ushort.Parse(ServerPort.Text);
            ProfileService.SettingConfig.NgsOn = NgsOn.Checked;
            ProfileService.SettingConfig.SpeedType = SpeedType.speedNames[Speed_comboBox.Text];
            ProfileService.SaveSettings();
        }
    }
}
