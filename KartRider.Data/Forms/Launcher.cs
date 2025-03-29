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
        private Button Button_Launch;
        private Button Button_GetKart;
        private Label label_DeveloperName;
        private ComboBox comboBox_Choose_Speed;
        private Label label_Choose_Speed;
        private Label label_Download_Dotnet80;
        private Label label_GitHub_Repo;
        private Label label_KartInfo;
        private Label label_Docs;
        private Label MinorVersion;

        public Launcher()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Launcher));
            Button_Launch = new Button();
            Button_GetKart = new Button();
            label_DeveloperName = new Label();
            MinorVersion = new Label();
            comboBox_Choose_Speed = new ComboBox();
            label_Choose_Speed = new Label();
            label_Download_Dotnet80 = new Label();
            label_GitHub_Repo = new Label();
            label_KartInfo = new Label();
            label_Docs = new Label();
            SuspendLayout();
            // 
            // Button_Launch
            // 
            Button_Launch.Location = new Point(19, 20);
            Button_Launch.Name = "Button_Launch";
            Button_Launch.Size = new Size(114, 23);
            Button_Launch.TabIndex = 364;
            Button_Launch.Text = "启动游戏";
            Button_Launch.UseVisualStyleBackColor = true;
            Button_Launch.Click += Start_Button_Click;
            // 
            // Button_GetKart
            // 
            Button_GetKart.Location = new Point(19, 49);
            Button_GetKart.Name = "Button_GetKart";
            Button_GetKart.Size = new Size(114, 23);
            Button_GetKart.TabIndex = 365;
            Button_GetKart.Text = "添加道具";
            Button_GetKart.UseVisualStyleBackColor = true;
            Button_GetKart.Click += GetKart_Button_Click;
            // 
            // label_DeveloperName
            // 
            label_DeveloperName.AutoSize = true;
            label_DeveloperName.BackColor = SystemColors.Control;
            label_DeveloperName.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label_DeveloperName.ForeColor = Color.Blue;
            label_DeveloperName.Location = new Point(2, 160);
            label_DeveloperName.Name = "label_DeveloperName";
            label_DeveloperName.Size = new Size(47, 12);
            label_DeveloperName.TabIndex = 367;
            label_DeveloperName.Text = "Client:";
            label_DeveloperName.Click += label_DeveloperName_Click;
            // 
            // MinorVersion
            // 
            MinorVersion.AutoSize = true;
            MinorVersion.BackColor = SystemColors.Control;
            MinorVersion.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinorVersion.ForeColor = Color.Red;
            MinorVersion.Location = new Point(45, 160);
            MinorVersion.Name = "MinorVersion";
            MinorVersion.Size = new Size(0, 12);
            MinorVersion.TabIndex = 367;
            // 
            // comboBox_Choose_Speed
            // 
            comboBox_Choose_Speed.ForeColor = Color.Red;
            comboBox_Choose_Speed.FormattingEnabled = true;
            comboBox_Choose_Speed.Items.AddRange(new object[] { "标准", "慢速", "普通", "快速", "高速" });
            comboBox_Choose_Speed.Location = new Point(84, 78);
            comboBox_Choose_Speed.Name = "comboBox_Choose_Speed";
            comboBox_Choose_Speed.Size = new Size(49, 20);
            comboBox_Choose_Speed.TabIndex = 368;
            comboBox_Choose_Speed.Text = "标准";
            comboBox_Choose_Speed.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // label_Choose_Speed
            // 
            label_Choose_Speed.AutoSize = true;
            label_Choose_Speed.ForeColor = Color.Blue;
            label_Choose_Speed.Location = new Point(19, 82);
            label_Choose_Speed.Name = "label_Choose_Speed";
            label_Choose_Speed.Size = new Size(59, 12);
            label_Choose_Speed.TabIndex = 369;
            label_Choose_Speed.Text = "速度选择:";
            // 
            // label_Download_Dotnet80
            // 
            label_Download_Dotnet80.AutoSize = true;
            label_Download_Dotnet80.ForeColor = Color.Blue;
            label_Download_Dotnet80.Location = new Point(207, 128);
            label_Download_Dotnet80.Name = "label_Download_Dotnet80";
            label_Download_Dotnet80.Size = new Size(47, 12);
            label_Download_Dotnet80.TabIndex = 370;
            label_Download_Dotnet80.Text = ".NET8.0";
            label_Download_Dotnet80.Click += label2_Click;
            // 
            // label_GitHub_Repo
            // 
            label_GitHub_Repo.AutoSize = true;
            label_GitHub_Repo.ForeColor = Color.Blue;
            label_GitHub_Repo.Location = new Point(213, 144);
            label_GitHub_Repo.Name = "label_GitHub_Repo";
            label_GitHub_Repo.Size = new Size(41, 12);
            label_GitHub_Repo.TabIndex = 371;
            label_GitHub_Repo.Text = "GitHub";
            label_GitHub_Repo.Click += label3_Click;
            // 
            // label_KartInfo
            // 
            label_KartInfo.AutoSize = true;
            label_KartInfo.ForeColor = Color.Blue;
            label_KartInfo.Location = new Point(201, 160);
            label_KartInfo.Name = "label_KartInfo";
            label_KartInfo.Size = new Size(53, 12);
            label_KartInfo.TabIndex = 372;
            label_KartInfo.Text = "KartInfo";
            label_KartInfo.Click += label4_Click;
            // 
            // label_Docs
            // 
            label_Docs.AutoSize = true;
            label_Docs.ForeColor = Color.Blue;
            label_Docs.Location = new Point(225, 116);
            label_Docs.Name = "label_Docs";
            label_Docs.Size = new Size(29, 12);
            label_Docs.TabIndex = 373;
            label_Docs.Text = "Docs";
            label_Docs.Click += label_Docs_Click;
            // 
            // Launcher
            // 
            AutoScaleDimensions = new SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(257, 180);
            Controls.Add(label_Docs);
            Controls.Add(label_KartInfo);
            Controls.Add(label_GitHub_Repo);
            Controls.Add(label_Download_Dotnet80);
            Controls.Add(label_Choose_Speed);
            Controls.Add(comboBox_Choose_Speed);
            Controls.Add(MinorVersion);
            Controls.Add(label_DeveloperName);
            Controls.Add(Button_GetKart);
            Controls.Add(Button_Launch);
            Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
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
                    Button_Launch.Enabled = true;
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
                        Button_Launch.Enabled = true;
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
                    short.TryParse(xe.GetAttribute("Engine"), out short Engine) &&
                    short.TryParse(xe.GetAttribute("Engine_id"), out short Engine_id) &&
                    short.TryParse(xe.GetAttribute("Handle"), out short Handle) &&
                    short.TryParse(xe.GetAttribute("Handle_id"), out short Handle_id) &&
                    short.TryParse(xe.GetAttribute("Wheel"), out short Wheel) &&
                    short.TryParse(xe.GetAttribute("Wheel_id"), out short Wheel_id) &&
                    short.TryParse(xe.GetAttribute("Kit"), out short Kit) &&
                    short.TryParse(xe.GetAttribute("Kit_id"), out short Kit_id))
                {
                    result.Add(new List<short> { id, sn, Engine, Engine_id, Handle, Handle_id, Wheel, Wheel_id, Kit, Kit_id });
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
                    short.TryParse(xe.GetAttribute("Engine"), out short Engine) &&
                    short.TryParse(xe.GetAttribute("EngineGrade"), out short EngineGrade) &&
                    short.TryParse(xe.GetAttribute("EngineValue"), out short EngineValue) &&
                    short.TryParse(xe.GetAttribute("Handle"), out short Handle) &&
                    short.TryParse(xe.GetAttribute("HandleGrade"), out short HandleGrade) &&
                    short.TryParse(xe.GetAttribute("HandleValue"), out short HandleValue) &&
                    short.TryParse(xe.GetAttribute("Wheel"), out short Wheel) &&
                    short.TryParse(xe.GetAttribute("WheelGrade"), out short WheelGrade) &&
                    short.TryParse(xe.GetAttribute("WheelValue"), out short WheelValue) &&
                    short.TryParse(xe.GetAttribute("Booster"), out short Booster) &&
                    short.TryParse(xe.GetAttribute("BoosterGrade"), out short BoosterGrade) &&
                    short.TryParse(xe.GetAttribute("BoosterValue"), out short BoosterValue) &&
                    short.TryParse(xe.GetAttribute("Coating"), out short Coating) &&
                    short.TryParse(xe.GetAttribute("TailLamp"), out short TailLamp))
                {
                    result.Add(new List<short> { id, sn, Engine, EngineGrade, EngineValue, Handle, HandleGrade, HandleValue, Wheel, WheelGrade, WheelValue, Booster, BoosterGrade, BoosterValue, Coating, TailLamp });
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
                    short.TryParse(xe.GetAttribute("Engine"), out short Engine) &&
                    short.TryParse(xe.GetAttribute("defaultEngine"), out short defaultEngine) &&
                    short.TryParse(xe.GetAttribute("EngineValue"), out short EngineValue) &&
                    short.TryParse(xe.GetAttribute("Handle"), out short Handle) &&
                    short.TryParse(xe.GetAttribute("defaultHandle"), out short defaultHandle) &&
                    short.TryParse(xe.GetAttribute("HandleValue"), out short HandleValue) &&
                    short.TryParse(xe.GetAttribute("Wheel"), out short Wheel) &&
                    short.TryParse(xe.GetAttribute("defaultWheel"), out short defaultWheel) &&
                    short.TryParse(xe.GetAttribute("WheelValue"), out short WheelValue) &&
                    short.TryParse(xe.GetAttribute("Booster"), out short Booster) &&
                    short.TryParse(xe.GetAttribute("defaultBooster"), out short defaultBooster) &&
                    short.TryParse(xe.GetAttribute("BoosterValue"), out short BoosterValue) &&
                    short.TryParse(xe.GetAttribute("Coating"), out short Coating) &&
                    short.TryParse(xe.GetAttribute("TailLamp"), out short TailLamp) &&
                    short.TryParse(xe.GetAttribute("BoosterEffect"), out short BoosterEffect) &&
                    short.TryParse(xe.GetAttribute("ExceedType"), out short ExceedType))
                {
                    result.Add(new List<short> { id, sn, Engine, defaultEngine, EngineValue, Handle, defaultHandle, HandleValue, Wheel, defaultWheel, WheelValue, Booster, defaultBooster, BoosterValue, Coating, TailLamp, BoosterEffect, ExceedType });
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
            if (comboBox_Choose_Speed.SelectedItem != null)
            {
                Console.WriteLine(comboBox_Choose_Speed.SelectedItem.ToString());
                if (comboBox_Choose_Speed.SelectedItem.ToString() == "标准")
                {
                    config.SpeedType = 7;
                }
                else if (comboBox_Choose_Speed.SelectedItem.ToString() == "慢速")
                {
                    config.SpeedType = 3;
                }
                else if (comboBox_Choose_Speed.SelectedItem.ToString() == "普通")
                {
                    config.SpeedType = 0;
                }
                else if (comboBox_Choose_Speed.SelectedItem.ToString() == "快速")
                {
                    config.SpeedType = 1;
                }
                else if (comboBox_Choose_Speed.SelectedItem.ToString() == "高速")
                {
                    config.SpeedType = 2;
                }
                else
                {
                    Console.WriteLine("Error: Unknown speed type, switched to S1.5");
                    config.SpeedType = 7;
                }
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
            string url = "https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-desktop-8.0.13-windows-x64-installer";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {
            string url = "https://github.com/yanygm/Launcher_V2/releases";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {
            string url = "https://kartinfo.me/thread-9369-1-1.html";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        private void label_DeveloperName_Click(object sender, EventArgs e)
        {
            string url = "https://github.com/brownsugar/popkart-client-archive/releases";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        private void label_Docs_Click(object sender, EventArgs e)
        {
            string url = "https://themagicflute.github.io/Launcher_V2";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }
    }
}
