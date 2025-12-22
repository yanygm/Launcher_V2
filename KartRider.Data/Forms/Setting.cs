using System;
using System.Linq;
using System.Windows.Forms;
using ExcData;
using Profile;

namespace KartRider
{
    public partial class Setting : Form
    {
        public string[] AiSpeed = new string[] { "简单", "困难", "地狱" };

        public string[] urls = new string[] { "https://gh-proxy.com/", "https://ghproxy.net/", "https://git.myany.uk/", "" };

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
            ProfileService.SettingConfig.PatchUpdate = PatchUpdate.Checked;
            ProfileService.SettingConfig.SpeedType = SpeedType.speedNames[Speed_comboBox.Text];
            ProfileService.SettingConfig.AiSpeedType = AiSpeed_comboBox.Text;
            ProfileService.SettingConfig.Proxy = Proxy_comboBox.Text;
            ProfileService.SaveSettings();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            PlayerName.Text = ProfileService.SettingConfig.Name;
            ServerIP.Text = ProfileService.SettingConfig.ServerIP;
            ServerPort.Text = ProfileService.SettingConfig.ServerPort.ToString();
            NgsOn.Checked = ProfileService.SettingConfig.NgsOn;
            PatchUpdate.Checked = ProfileService.SettingConfig.PatchUpdate;
            ProfileService.SaveSettings();
            foreach (string key in SpeedType.speedNames.Keys)
            {
                Speed_comboBox.Items.Add(key);
            }
            foreach (string key in AiSpeed)
            {
                AiSpeed_comboBox.Items.Add(key);
            }
            foreach (string key in urls)
            {
                Proxy_comboBox.Items.Add(key);
            }
            Speed_comboBox.Text = (SpeedType.speedNames.FirstOrDefault(x => x.Value == ProfileService.SettingConfig.SpeedType).Key);
            AiSpeed_comboBox.Text = ProfileService.SettingConfig.AiSpeedType;
            Proxy_comboBox.Text = ProfileService.SettingConfig.Proxy;
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
                }
                else
                {
                    Console.WriteLine("Invalid speed type selected.");
                }
            }
        }

        private void AiSpeed_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AiSpeed_comboBox.SelectedItem != null)
            {
                string selectedAiSpeed = AiSpeed_comboBox.SelectedItem.ToString();
                if (AiSpeed.Contains(selectedAiSpeed))
                {
                    ProfileService.SettingConfig.AiSpeedType = selectedAiSpeed;
                    ProfileService.SaveSettings();
                }
                else
                {
                    Console.WriteLine("Invalid AiSpeed type selected.");
                }
            }
        }

        private void Proxy_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Proxy_comboBox.SelectedItem != null)
            {
                string selectedProxy = Proxy_comboBox.SelectedItem.ToString();
                if (urls.Contains(selectedProxy))
                {
                    ProfileService.SettingConfig.Proxy = selectedProxy;
                    ProfileService.SaveSettings();
                }
                else
                {
                    Console.WriteLine("Invalid Proxy type selected.");
                }
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            ProfileService.SettingConfig.Name = PlayerName.Text;
            ProfileService.SettingConfig.ServerIP = ServerIP.Text;
            ProfileService.SettingConfig.ServerPort = ushort.Parse(ServerPort.Text);
            ProfileService.SettingConfig.NgsOn = NgsOn.Checked;
            ProfileService.SettingConfig.PatchUpdate = PatchUpdate.Checked;
            ProfileService.SettingConfig.SpeedType = SpeedType.speedNames[Speed_comboBox.Text];
            ProfileService.SettingConfig.AiSpeedType = AiSpeed_comboBox.Text;
            ProfileService.SettingConfig.Proxy = Proxy_comboBox.Text;
            ProfileService.SaveSettings();
        }
    }
}
