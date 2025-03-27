using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Set_Data;
using System.Xml;
using ExcData;
using Launcher.Properties;
using KartRider.Common.Data;
using System.Xml.Linq;
using RHOParser;
using KartRider;
using KartRider.IO.Packet;
using KartRider.Common.Utilities;
using KartLibrary.File;
using static KartRider.Common.Data.PINFile;
using System.Collections;

namespace KartRider
{
    public class Launcher : Form
    {
        public static bool GetKart = true;
        public static short KartSN = 0;
        public string kartRiderDirectory = null;
        public string profilePath = null;
        public static string KartRider = "KartRider.exe";
        public static string pinFile = "KartRider.pin";
        private Button Start_Button;
        private Button GetKart_Button;
        private Label label_DeveloperName;
        private ComboBox comboBox1;
        private LinkLabel linkLabel_kartinfo;
        private LinkLabel linkLabel_ghrepo;
        private Label choose_speed;
        private Label MinorVersion;

        public Launcher()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            Start_Button = new Button();
            GetKart_Button = new Button();
            label_DeveloperName = new Label();
            MinorVersion = new Label();
            comboBox1 = new ComboBox();
            linkLabel_kartinfo = new LinkLabel();
            linkLabel_ghrepo = new LinkLabel();
            choose_speed = new Label();
            SuspendLayout();
            // 
            // Start_Button
            // 
            Start_Button.Location = new Point(122, 92);
            Start_Button.Name = "Start_Button";
            Start_Button.Size = new Size(123, 76);
            Start_Button.TabIndex = 364;
            Start_Button.Text = "启动游戏";
            Start_Button.UseVisualStyleBackColor = true;
            Start_Button.Click += Start_Button_Click;
            // 
            // GetKart_Button
            // 
            GetKart_Button.Location = new Point(122, 38);
            GetKart_Button.Name = "GetKart_Button";
            GetKart_Button.Size = new Size(123, 48);
            GetKart_Button.TabIndex = 365;
            GetKart_Button.Text = "添加道具";
            GetKart_Button.UseVisualStyleBackColor = true;
            GetKart_Button.Click += GetKart_Button_Click;
            // 
            // label_DeveloperName
            // 
            label_DeveloperName.AutoSize = true;
            label_DeveloperName.BackColor = SystemColors.Control;
            label_DeveloperName.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label_DeveloperName.ForeColor = Color.Blue;
            label_DeveloperName.Location = new Point(2, 160);
            label_DeveloperName.Name = "label_DeveloperName";
            label_DeveloperName.Size = new Size(53, 12);
            label_DeveloperName.TabIndex = 367;
            label_DeveloperName.Text = "Version:";
            // 
            // MinorVersion
            // 
            MinorVersion.AutoSize = true;
            MinorVersion.BackColor = SystemColors.Control;
            MinorVersion.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinorVersion.ForeColor = Color.Red;
            MinorVersion.Location = new Point(55, 160);
            MinorVersion.Name = "MinorVersion";
            MinorVersion.Size = new Size(0, 12);
            MinorVersion.TabIndex = 367;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "统合 S1.5", "低速 S0", "普通 S1", "快速 S2", "高速 S3" });
            comboBox1.Location = new Point(122, 12);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(123, 20);
            comboBox1.TabIndex = 368;
            comboBox1.Text = "统合 S1.5";
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // linkLabel_kartinfo
            // 
            linkLabel_kartinfo.AutoSize = true;
            linkLabel_kartinfo.LinkColor = Color.Gray;
            linkLabel_kartinfo.Location = new Point(2, 124);
            linkLabel_kartinfo.Name = "linkLabel_kartinfo";
            linkLabel_kartinfo.Size = new Size(65, 12);
            linkLabel_kartinfo.TabIndex = 371;
            linkLabel_kartinfo.TabStop = true;
            linkLabel_kartinfo.Text = "跑跑資訊站";
            linkLabel_kartinfo.LinkClicked += linkLabel_kartinfo_LinkClicked;
            // 
            // linkLabel_ghrepo
            // 
            linkLabel_ghrepo.AutoSize = true;
            linkLabel_ghrepo.LinkColor = Color.Gray;
            linkLabel_ghrepo.Location = new Point(2, 136);
            linkLabel_ghrepo.Name = "linkLabel_ghrepo";
            linkLabel_ghrepo.Size = new Size(65, 12);
            linkLabel_ghrepo.TabIndex = 372;
            linkLabel_ghrepo.TabStop = true;
            linkLabel_ghrepo.Text = "源代码仓库";
            linkLabel_ghrepo.LinkClicked += linkLabel_ghrepo_LinkClicked;
            // 
            // choose_speed
            // 
            choose_speed.AutoSize = true;
            choose_speed.Location = new Point(55, 15);
            choose_speed.Name = "choose_speed";
            choose_speed.Size = new Size(65, 12);
            choose_speed.TabIndex = 373;
            choose_speed.Text = "选择速度：";
            choose_speed.Click += label1_Click;
            // 
            // Launcher
            // 
            AutoScaleDimensions = new SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(257, 180);
            Controls.Add(choose_speed);
            Controls.Add(linkLabel_ghrepo);
            Controls.Add(linkLabel_kartinfo);
            Controls.Add(comboBox1);
            Controls.Add(MinorVersion);
            Controls.Add(label_DeveloperName);
            Controls.Add(GetKart_Button);
            Controls.Add(Start_Button);
            Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
			Icon = Resources.icon;
            Name = "Launcher";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Launcher";
            FormClosing += OnFormClosing;
            Load += OnLoad;
            ResumeLayout(false);
            PerformLayout();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (File.Exists(this.kartRiderDirectory + "KartRider-bak.pin"))
            {
                File.Delete(this.kartRiderDirectory + "KartRider.pin");
                File.Move(this.kartRiderDirectory + "KartRider-bak.pin", this.kartRiderDirectory + "KartRider.pin");
            }
            if (Process.GetProcessesByName("KartRider").Length != 0)
            {
                LauncherSystem.MessageBoxType1();
                e.Cancel = true;
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            string str = Path.Combine(Environment.CurrentDirectory, "Profile", SessionGroup.Service);
            if (!Directory.Exists(str))
            {
                Directory.CreateDirectory(str);
            }
            Load_KartExcData();
            StartingLoad_ALL.StartingLoad();
            PINFile val = new PINFile(this.kartRiderDirectory + "KartRider.pin");
            SetGameOption.Version = val.Header.MinorVersion;
            SetGameOption.Save_SetGameOption();
            MinorVersion.Text = SetGameOption.Version.ToString();
            this.profilePath = Path.Combine(str, "launcher.xml");
            Console.WriteLine("Process: {0}", this.kartRiderDirectory + Launcher.KartRider);
            Console.WriteLine("Profile: {0}", this.profilePath);
            RouterListener.Start();
        }

        private void Start_Button_Click(object sender, EventArgs e)
        {
            if (Process.GetProcessesByName("KartRider").Length != 0)
            {
                LauncherSystem.MessageBoxType2();
            }
            else
            {
                (new Thread(() =>
                {
                    Console.WriteLine("Backing up old PinFile...");
                    Console.WriteLine(this.kartRiderDirectory + "KartRider-bak.pin");
                    if (File.Exists(this.kartRiderDirectory + "KartRider-bak.pin"))
                    {
                        File.Delete(this.kartRiderDirectory + "KartRider.pin");
                        File.Move(this.kartRiderDirectory + "KartRider-bak.pin", this.kartRiderDirectory + "KartRider.pin");
                    }
                    File.Copy(this.kartRiderDirectory + "KartRider.pin", this.kartRiderDirectory + "KartRider-bak.pin");
                    PINFile val = new PINFile(this.kartRiderDirectory + "KartRider.pin");
                    foreach (AuthMethod authMethod in val.AuthMethods)
                    {
                        Console.WriteLine("Changing IP Addr to local... {0}", authMethod.Name);
                        authMethod.LoginServers.Clear();
                        authMethod.LoginServers.Add(new IPEndPoint
                        {
                            IP = "127.0.0.1",
                            Port = 39312
                        });
                    }
                    foreach (BmlObject bml in val.BmlObjects)
                    {
                        if (bml.Name == "extra")
                        {
                            for (int i = bml.SubObjects.Count - 1; i >= 0; i--)
                            {
                                Console.WriteLine("Removing {0}", bml.SubObjects[i].Item1);
                                if (bml.SubObjects[i].Item1 == "NgsOn")
                                {
                                    bml.SubObjects.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                    File.WriteAllBytes(this.kartRiderDirectory + "KartRider.pin", val.GetEncryptedData());
                    Start_Button.Enabled = true;
                    Launcher.GetKart = false;
                    string str = this.profilePath;
                    string[] text = new string[] { "<?xml version='1.0' encoding='UTF-16'?>\r\n<profile>\r\n<username>", SetRider.UserID, "</username>\r\n</profile>" };
                    File.WriteAllText(str, string.Concat(text));
                    ProcessStartInfo startInfo = new ProcessStartInfo(Launcher.KartRider, "TGC -region:3 -passport:556O5Yeg5oqK55yL5ZWl")
                    {
                        WorkingDirectory = this.kartRiderDirectory,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    try
                    {
                        Process.Start(startInfo);
                        Thread.Sleep(1000);
                        Start_Button.Enabled = true;
                        Launcher.GetKart = true;
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        // 用户取消了UAC提示或没有权限
                        Console.WriteLine(ex.Message);
                    }
                })).Start();
            }
        }

        private void GetKart_Button_Click(object sender, EventArgs e)
        {
            if (Launcher.GetKart)
            {
                //GetKart_Button.Enabled = false;
                Program.GetKartDlg = new GetKart();
                Program.GetKartDlg.ShowDialog();
                //GetKart_Button.Enabled = true;
            }
        }

        public void Load_KartExcData()
        {
            EnsureDefaultDataFileExists(@"Profile\AI.xml", CreateAIDefaultData);

            KartExcData.NewKart = LoadKartData(@"Profile\NewKart.xml", LoadNewKart);
            KartExcData.TuneList = LoadKartData(@"Profile\TuneData.xml", LoadTuneData);
            KartExcData.PlantList = LoadKartData(@"Profile\PlantData.xml", LoadPlantData);
            KartExcData.LevelList = LoadKartData(@"Profile\LevelData.xml", LoadLevelData);
            KartExcData.PartsList = LoadKartData(@"Profile\PartsData.xml", LoadPartsData);
            KartExcData.Parts12List = LoadKartData(@"Profile\Parts12Data.xml", LoadParts12Data);
            KartExcData.Level12List = LoadKartData(@"Profile\Level12Data.xml", LoadLevel12Data);
        }

        private void EnsureDefaultDataFileExists(string filePath, Action createDefaultData)
        {
            if (!File.Exists(filePath))
            {
                createDefaultData();
            }
        }

        private void CreateAIDefaultData()
        {
            using (XmlTextWriter writer = new XmlTextWriter(@"Profile\AI.xml", System.Text.Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("AI");
                writer.WriteEndElement();
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(@"Profile\AI.xml");
            XmlNode root = xmlDoc.SelectSingleNode("AI");
            for (var i = 0; i < 7; i++)
            {
                XmlElement xe1 = xmlDoc.CreateElement("AiList");
                xe1.SetAttribute("a", "1");
                xe1.SetAttribute("b", "0");
                xe1.SetAttribute("c", "1508");
                xe1.SetAttribute("d", "0");
                xe1.SetAttribute("e", "0");
                xe1.SetAttribute("f", "0");
                root.AppendChild(xe1);
            }
            XmlElement xe2 = xmlDoc.CreateElement("AiData");
            xe2.SetAttribute("a", "1");
            xe2.SetAttribute("b", "2300");
            xe2.SetAttribute("c", "2930");
            xe2.SetAttribute("d", "1.4");
            xe2.SetAttribute("e", "1000");
            xe2.SetAttribute("f", "1500");
            root.AppendChild(xe2);
            xmlDoc.Save(@"Profile\AI.xml");
        }

        private List<List<short>> LoadKartData(string filePath, Func<XmlNodeList, List<List<short>>> parseDataFunction)
        {
            if (!File.Exists(filePath)) return new List<List<short>>();

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            XmlNodeList lis = doc.GetElementsByTagName("Kart");
            return parseDataFunction(lis);
        }

        private List<List<short>> LoadNewKart(XmlNodeList lis)
        {
            var result = new List<List<short>>();
            foreach (XmlNode xn in lis)
            {
                XmlElement xe = (XmlElement)xn;
                if (short.TryParse(xe.GetAttribute("id"), out short id) &&
                    short.TryParse(xe.GetAttribute("sn"), out short sn))
                {
                    result.Add(new List<short> { id, sn });
                }
                else
                {
                    Console.WriteLine(@"读取“Profile\NewKart.xml”文件失败，建议删除文件后重试！");
                }
            }
            return result;
        }

        private List<List<short>> LoadTuneData(XmlNodeList lis)
        {
            var result = new List<List<short>>();
            foreach (XmlNode xn in lis)
            {
                XmlElement xe = (XmlElement)xn;
                if (short.TryParse(xe.GetAttribute("id"), out short id) &&
                    short.TryParse(xe.GetAttribute("sn"), out short sn) &&
                    short.TryParse(xe.GetAttribute("tune1"), out short tune1) &&
                    short.TryParse(xe.GetAttribute("tune2"), out short tune2) &&
                    short.TryParse(xe.GetAttribute("tune3"), out short tune3) &&
                    short.TryParse(xe.GetAttribute("slot1"), out short slot1) &&
                    short.TryParse(xe.GetAttribute("count1"), out short count1) &&
                    short.TryParse(xe.GetAttribute("slot2"), out short slot2) &&
                    short.TryParse(xe.GetAttribute("count2"), out short count2))
                {
                    result.Add(new List<short> { id, sn, tune1, tune2, tune3, slot1, count1, slot2, count2 });
                }
                else
                {
                    Console.WriteLine(@"读取“Profile\TuneData.xml”文件失败，建议删除文件后重试！");
                }
            }
            return result;
        }

        private List<List<short>> LoadPlantData(XmlNodeList lis)
        {
            var result = new List<List<short>>();
            foreach (XmlNode xn in lis)
            {
                XmlElement xe = (XmlElement)xn;
                if (short.TryParse(xe.GetAttribute("id"), out short id) &&
                    short.TryParse(xe.GetAttribute("sn"), out short sn) &&
                    short.TryParse(xe.GetAttribute("item1"), out short item1) &&
                    short.TryParse(xe.GetAttribute("item_id1"), out short item_id1) &&
                    short.TryParse(xe.GetAttribute("item2"), out short item2) &&
                    short.TryParse(xe.GetAttribute("item_id2"), out short item_id2) &&
                    short.TryParse(xe.GetAttribute("item3"), out short item3) &&
                    short.TryParse(xe.GetAttribute("item_id3"), out short item_id3) &&
                    short.TryParse(xe.GetAttribute("item4"), out short item4) &&
                    short.TryParse(xe.GetAttribute("item_id4"), out short item_id4))
                {
                    result.Add(new List<short> { id, sn, item1, item_id1, item2, item_id2, item3, item_id3, item4, item_id4 });
                }
                else
                {
                    Console.WriteLine(@"读取“Profile\PlantData.xml”文件失败，建议删除文件后重试！");
                }
            }
            return result;
        }

        private List<List<short>> LoadLevelData(XmlNodeList lis)
        {
            var result = new List<List<short>>();
            foreach (XmlNode xn in lis)
            {
                XmlElement xe = (XmlElement)xn;
                if (short.TryParse(xe.GetAttribute("id"), out short id) &&
                    short.TryParse(xe.GetAttribute("sn"), out short sn) &&
                    short.TryParse(xe.GetAttribute("level"), out short level) &&
                    short.TryParse(xe.GetAttribute("point"), out short point) &&
                    short.TryParse(xe.GetAttribute("v1"), out short v1) &&
                    short.TryParse(xe.GetAttribute("v2"), out short v2) &&
                    short.TryParse(xe.GetAttribute("v3"), out short v3) &&
                    short.TryParse(xe.GetAttribute("v4"), out short v4) &&
                    short.TryParse(xe.GetAttribute("Effect"), out short Effect))
                {
                    result.Add(new List<short> { id, sn, level, point, v1, v2, v3, v4, Effect });
                }
                else
                {
                    Console.WriteLine(@"读取“Profile\LevelData.xml”文件失败，建议删除文件后重试！");
                }
            }
            return result;
        }

        private List<List<short>> LoadPartsData(XmlNodeList lis)
        {
            var result = new List<List<short>>();
            foreach (XmlNode xn in lis)
            {
                XmlElement xe = (XmlElement)xn;
                if (short.TryParse(xe.GetAttribute("id"), out short id) &&
                    short.TryParse(xe.GetAttribute("sn"), out short sn) &&
                    short.TryParse(xe.GetAttribute("Item_Id1"), out short Item_Id1) &&
                    short.TryParse(xe.GetAttribute("Grade1"), out short Grade1) &&
                    short.TryParse(xe.GetAttribute("PartsValue1"), out short PartsValue1) &&
                    short.TryParse(xe.GetAttribute("Item_Id2"), out short Item_Id2) &&
                    short.TryParse(xe.GetAttribute("Grade2"), out short Grade2) &&
                    short.TryParse(xe.GetAttribute("PartsValue2"), out short PartsValue2) &&
                    short.TryParse(xe.GetAttribute("Item_Id3"), out short Item_Id3) &&
                    short.TryParse(xe.GetAttribute("Grade3"), out short Grade3) &&
                    short.TryParse(xe.GetAttribute("PartsValue3"), out short PartsValue3) &&
                    short.TryParse(xe.GetAttribute("Item_Id4"), out short Item_Id4) &&
                    short.TryParse(xe.GetAttribute("Grade4"), out short Grade4) &&
                    short.TryParse(xe.GetAttribute("PartsValue4"), out short PartsValue4) &&
                    short.TryParse(xe.GetAttribute("partsCoating"), out short partsCoating) &&
                    short.TryParse(xe.GetAttribute("partsTailLamp"), out short partsTailLamp))
                {
                    result.Add(new List<short> { id, sn, Item_Id1, Grade1, PartsValue1, Item_Id2, Grade2, PartsValue2, Item_Id3, Grade3, PartsValue3, Item_Id4, Grade4, PartsValue4, partsCoating, partsTailLamp });
                }
                else
                {
                    Console.WriteLine(@"读取“Profile\PartsData.xml”文件失败，建议删除文件后重试！");
                }
            }
            return result;
        }

        private List<List<short>> LoadParts12Data(XmlNodeList lis)
        {
            var result = new List<List<short>>();
            foreach (XmlNode xn in lis)
            {
                XmlElement xe = (XmlElement)xn;
                if (short.TryParse(xe.GetAttribute("id"), out short id) &&
                    short.TryParse(xe.GetAttribute("sn"), out short sn) &&
                    short.TryParse(xe.GetAttribute("Item_Id1"), out short Item_Id1) &&
                    short.TryParse(xe.GetAttribute("Grade1"), out short Grade1) &&
                    short.TryParse(xe.GetAttribute("PartsValue1"), out short PartsValue1) &&
                    short.TryParse(xe.GetAttribute("Item_Id2"), out short Item_Id2) &&
                    short.TryParse(xe.GetAttribute("Grade2"), out short Grade2) &&
                    short.TryParse(xe.GetAttribute("PartsValue2"), out short PartsValue2) &&
                    short.TryParse(xe.GetAttribute("Item_Id3"), out short Item_Id3) &&
                    short.TryParse(xe.GetAttribute("Grade3"), out short Grade3) &&
                    short.TryParse(xe.GetAttribute("PartsValue3"), out short PartsValue3) &&
                    short.TryParse(xe.GetAttribute("Item_Id4"), out short Item_Id4) &&
                    short.TryParse(xe.GetAttribute("Grade4"), out short Grade4) &&
                    short.TryParse(xe.GetAttribute("PartsValue4"), out short PartsValue4) &&
                    short.TryParse(xe.GetAttribute("partsCoating"), out short partsCoating) &&
                    short.TryParse(xe.GetAttribute("partsTailLamp"), out short partsTailLamp) &&
                    short.TryParse(xe.GetAttribute("partsBoosterEffect"), out short partsBoosterEffect) &&
                    short.TryParse(xe.GetAttribute("ExceedType"), out short ExceedType))
                {
                    result.Add(new List<short> { id, sn, Item_Id1, Grade1, PartsValue1, Item_Id2, Grade2, PartsValue2, Item_Id3, Grade3, PartsValue3, Item_Id4, Grade4, PartsValue4, partsCoating, partsTailLamp, partsBoosterEffect, ExceedType });
                }
                else
                {
                    Console.WriteLine(@"读取“Profile\Parts12Data.xml”文件失败，建议删除文件后重试！");
                }
            }
            return result;
        }

        private List<List<short>> LoadLevel12Data(XmlNodeList lis)
        {
            var result = new List<List<short>>();
            foreach (XmlNode xn in lis)
            {
                XmlElement xe = (XmlElement)xn;
                if (short.TryParse(xe.GetAttribute("id"), out short id) &&
                    short.TryParse(xe.GetAttribute("sn"), out short sn) &&
                    short.TryParse(xe.GetAttribute("Level"), out short Level) &&
                    short.TryParse(xe.GetAttribute("Skill1"), out short Skill1) &&
                    short.TryParse(xe.GetAttribute("SkillLevel1"), out short SkillLevel1) &&
                    short.TryParse(xe.GetAttribute("Skill2"), out short Skill2) &&
                    short.TryParse(xe.GetAttribute("SkillLevel2"), out short SkillLevel2) &&
                    short.TryParse(xe.GetAttribute("Skill3"), out short Skill3) &&
                    short.TryParse(xe.GetAttribute("SkillLevel3"), out short SkillLevel3) &&
                    short.TryParse(xe.GetAttribute("Point"), out short Point))
                {
                    result.Add(new List<short> { id, sn, Level, Skill1, SkillLevel1, Skill2, SkillLevel2, Skill3, SkillLevel3, Point });
                }
                else
                {
                    Console.WriteLine(@"读取“Profile\Level12Data.xml”文件失败，建议删除文件后重试！");
                }
            }
            return result;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                Console.WriteLine(comboBox1.SelectedItem.ToString());
                if (comboBox1.SelectedItem.ToString() == "统合 S1.5")
                {
                    config.SpeedType = 7;
                }
                else if (comboBox1.SelectedItem.ToString() == "S0")
                {
                    config.SpeedType = 3;
                }
                else if (comboBox1.SelectedItem.ToString() == "S1")
                {
                    config.SpeedType = 0;
                }
                else if (comboBox1.SelectedItem.ToString() == "S2")
                {
                    config.SpeedType = 1;
                }
                else if (comboBox1.SelectedItem.ToString() == "S3")
                {
                    config.SpeedType = 2;
                }
            }
        }

        private void linkLabel_kartinfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabel_kartinfo.LinkVisited = true;
            Process.Start("cmd", "/c start https://kartinfo.me/thread-9369-1-1.html");
        }

        private void linkLabel_ghrepo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabel_ghrepo.LinkVisited = true;
            Process.Start("cmd", "/c start https://github.com/yanygm/Launcher_V2");
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
