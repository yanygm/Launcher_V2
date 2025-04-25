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
        public static string KartRider = "KartRider.exe";
        public static string pinFile = "KartRider.pin";
        private Button Start_Button;
        private Button GetKart_Button;
        private Label label_Client;
        private ComboBox Speed_comboBox;
        private Label Speed_label;
        private Label GitHub;
        private Label KartInfo;
        private Label Launcher_label;
        private Label ClientVersion;
        private Label label_Docs;
        private Label label_TimeAttackLog;
        private Label VersionLabel;

        public Launcher()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Launcher));
            Start_Button = new Button();
            GetKart_Button = new Button();
            label_Client = new Label();
            ClientVersion = new Label();
            VersionLabel = new Label();
            Speed_comboBox = new ComboBox();
            Speed_label = new Label();
            GitHub = new Label();
            KartInfo = new Label();
            Launcher_label = new Label();
            label_Docs = new Label();
            label_TimeAttackLog = new Label();
            SuspendLayout();
            // 
            // Start_Button
            // 
            Start_Button.Location = new Point(19, 20);
            Start_Button.Name = "Start_Button";
            Start_Button.Size = new Size(114, 23);
            Start_Button.TabIndex = 364;
            Start_Button.Text = "启动游戏";
            Start_Button.UseVisualStyleBackColor = true;
            Start_Button.Click += Start_Button_Click;
            // 
            // GetKart_Button
            // 
            GetKart_Button.Location = new Point(19, 49);
            GetKart_Button.Name = "GetKart_Button";
            GetKart_Button.Size = new Size(114, 23);
            GetKart_Button.TabIndex = 365;
            GetKart_Button.Text = "添加道具";
            GetKart_Button.UseVisualStyleBackColor = true;
            GetKart_Button.Click += GetKart_Button_Click;
            // 
            // label_Client
            // 
            label_Client.AutoSize = true;
            label_Client.BackColor = SystemColors.Control;
            label_Client.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label_Client.ForeColor = Color.Blue;
            label_Client.Location = new Point(2, 144);
            label_Client.Name = "label_Client";
            label_Client.Size = new Size(53, 12);
            label_Client.TabIndex = 367;
            label_Client.Text = "游戏版本:";
            label_Client.Click += label_Client_Click;
            // 
            // ClientVersion
            // 
            ClientVersion.AutoSize = true;
            ClientVersion.BackColor = SystemColors.Control;
            ClientVersion.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ClientVersion.ForeColor = Color.Red;
            ClientVersion.Location = new Point(70, 144);
            ClientVersion.Name = "ClientVersion";
            ClientVersion.Size = new Size(0, 12);
            ClientVersion.TabIndex = 367;
            ClientVersion.Click += label_Client_Click;
            // 
            // VersionLabel
            // 
            VersionLabel.AutoSize = true;
            VersionLabel.BackColor = SystemColors.Control;
            VersionLabel.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            VersionLabel.ForeColor = Color.Red;
            VersionLabel.Location = new Point(70, 160);
            VersionLabel.Name = "VersionLabel";
            VersionLabel.Size = new Size(0, 12);
            VersionLabel.TabIndex = 373;
            VersionLabel.Click += GitHub_Click;
            // 
            // Speed_comboBox
            // 
            Speed_comboBox.ForeColor = System.Drawing.Color.Red;
            Speed_comboBox.FormattingEnabled = true;
            Speed_comboBox.Sorted = false;
            foreach (string key in SpeedType.speedNames.Keys)
            {
                Speed_comboBox.Items.Add(key);
            }
            Speed_comboBox.Location = new System.Drawing.Point(54, 78);
            Speed_comboBox.Name = "Speed_comboBox";
            Speed_comboBox.Size = new System.Drawing.Size(100, 20);
            Speed_comboBox.TabIndex = 368;
            Speed_comboBox.Text = "统合S1.5";
            Speed_comboBox.SelectedIndexChanged += Speed_comboBox_SelectedIndexChanged;
            // 
            // Speed_label
            // 
            Speed_label.AutoSize = true;
            Speed_label.ForeColor = Color.Blue;
            Speed_label.Location = new Point(19, 82);
            Speed_label.Name = "Speed_label";
            Speed_label.Size = new Size(35, 12);
            Speed_label.TabIndex = 369;
            Speed_label.Text = "速度:";
            // 
            // GitHub
            // 
            GitHub.AutoSize = true;
            GitHub.ForeColor = Color.Blue;
            GitHub.Location = new Point(213, 144);
            GitHub.Name = "GitHub";
            GitHub.Size = new Size(41, 12);
            GitHub.TabIndex = 371;
            GitHub.Text = "GitHub";
            GitHub.Click += GitHub_Click;
            // 
            // KartInfo
            // 
            KartInfo.AutoSize = true;
            KartInfo.ForeColor = Color.Blue;
            KartInfo.Location = new Point(201, 160);
            KartInfo.Name = "KartInfo";
            KartInfo.Size = new Size(53, 12);
            KartInfo.TabIndex = 372;
            KartInfo.Text = "KartInfo";
            KartInfo.Click += KartInfo_Click;
            // 
            // Launcher_label
            // 
            Launcher_label.AutoSize = true;
            Launcher_label.ForeColor = Color.Blue;
            Launcher_label.Location = new Point(2, 160);
            Launcher_label.Name = "Launcher_label";
            Launcher_label.Size = new Size(71, 12);
            Launcher_label.TabIndex = 373;
            Launcher_label.Text = "启动器版本:";
            Launcher_label.Click += GitHub_Click;
            // 
            // label_Docs
            // 
            label_Docs.AutoSize = true;
            label_Docs.ForeColor = Color.Blue;
            label_Docs.Location = new Point(177, 132);
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
            label_TimeAttackLog.Location = new Point(177, 120);
            label_TimeAttackLog.Name = "label_TimeAttackLog";
            label_TimeAttackLog.Size = new Size(77, 12);
            label_TimeAttackLog.TabIndex = 375;
            label_TimeAttackLog.Text = "查看计时记录";
            label_TimeAttackLog.Click += label_TimeAttackLog_Click;
            // 
            // Launcher
            // 
            AutoScaleDimensions = new SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(257, 180);
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
            Controls.Add(GetKart_Button);
            Controls.Add(Start_Button);
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
            Load_KartExcData();
            StartingLoad_ALL.StartingLoad();
            PINFile val = new PINFile(this.kartRiderDirectory + "KartRider.pin");
            SetGameOption.Version = val.Header.MinorVersion;
            SetGameOption.Save_SetGameOption();
            ClientVersion.Text = $"P{SetGameOption.Version.ToString()}";
            DateTime compilationDate = File.GetLastWriteTime(AppDomain.CurrentDomain.BaseDirectory + "Launcher.exe");
            string formattedDate = compilationDate.ToString("yyMMdd");
            VersionLabel.Text = formattedDate;
            Console.WriteLine("Process: {0}", this.kartRiderDirectory + Launcher.KartRider);
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
                    ProcessStartInfo startInfo = new ProcessStartInfo(Launcher.KartRider, "TGC -region:3 -passport:aHR0cHM6Ly9naXRodWIuY29tL3lhbnlnbS9MYXVuY2hlcl9WMi9yZWxlYXNlcw==")
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
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Profile\ModelMax.xml"))
            {
                string ModelMax = Resources.ModelMax;
                using (StreamWriter streamWriter = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"Profile\ModelMax.xml", false))
                {
                    streamWriter.Write(ModelMax);
                }
            }
            EnsureDefaultDataFileExists(AppDomain.CurrentDomain.BaseDirectory + @"Profile\AI.xml", CreateAIDefaultData);

            KartExcData.NewKart = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\NewKart.xml", LoadNewKart);
            KartExcData.TuneList = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\TuneData.xml", LoadTuneData);
            KartExcData.PlantList = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\PlantData.xml", LoadPlantData);
            KartExcData.LevelList = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\LevelData.xml", LoadLevelData);
            KartExcData.PartsList = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\PartsData.xml", LoadPartsData);
            KartExcData.Parts12List = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\Parts12Data.xml", LoadParts12Data);
            KartExcData.Level12List = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\Level12Data.xml", LoadLevel12Data);
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
            using (XmlTextWriter writer = new XmlTextWriter(AppDomain.CurrentDomain.BaseDirectory + @"Profile\AI.xml", System.Text.Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("AI");
                writer.WriteEndElement();
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(AppDomain.CurrentDomain.BaseDirectory + @"Profile\AI.xml");
            XmlNode root = xmlDoc.SelectSingleNode("AI");
            XmlElement xe2 = xmlDoc.CreateElement("AiData");
            xe2.SetAttribute("a", "1");
            xe2.SetAttribute("b", "2300");
            xe2.SetAttribute("c", "2930");
            xe2.SetAttribute("d", "1.4");
            xe2.SetAttribute("e", "1000");
            xe2.SetAttribute("f", "1500");
            root.AppendChild(xe2);
            xmlDoc.Save(AppDomain.CurrentDomain.BaseDirectory + @"Profile\AI.xml");
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

        private void Speed_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Speed_comboBox.SelectedItem != null)
            {
                string selectedSpeed = Speed_comboBox.SelectedItem.ToString();
                if (SpeedType.speedNames.ContainsKey(selectedSpeed))
                {
                    config.SpeedType = SpeedType.speedNames[selectedSpeed];
                    Console.WriteLine($"速度更改为: {selectedSpeed}");
                }
                else
                {
                    Console.WriteLine("未知/错误的速度选项");
                }
            }
        }

        private void GitHub_Click(object sender, EventArgs e)
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

        private void KartInfo_Click(object sender, EventArgs e)
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

        private void label_Client_Click(object sender, EventArgs e)
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
            string url = "https://themagicflute.github.io/Launcher_V2/";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        private void label_TimeAttackLog_Click(object sender, EventArgs e)
        {
            string cmd = AppDomain.CurrentDomain.BaseDirectory + @"Profile\TimeAttack.log";
            try
            {
                Process.Start(new ProcessStartInfo(cmd) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(System.ComponentModel.Win32Exception))
                {
                    Console.WriteLine("计时日志文件未找到, 请进行计时后再查看!");
                }
                else
                {
                    Console.WriteLine($"错误: {ex.Message}");
                }
            }
        }
    }
}
