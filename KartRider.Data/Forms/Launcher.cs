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
using KartRider.Common.Data;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Xml.XPath;

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
        private Button button_ToggleTerminal;
        private Label label_Client;
        private ComboBox Speed_comboBox;
        private Label Speed_label;
        private Label GitHub;
        private Label KartInfo;
        private Label Launcher_label;
        private Label ClientVersion;
        private Label VersionLabel;

        public Launcher()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            Start_Button = new Button();
            GetKart_Button = new Button();
            button_ToggleTerminal = new Button();
            label_Client = new Label();
            ClientVersion = new Label();
            VersionLabel = new Label();
            Speed_comboBox = new ComboBox();
            Speed_label = new Label();
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
            Start_Button.TabIndex = 364;
            Start_Button.Text = "启动游戏";
            Start_Button.UseVisualStyleBackColor = true;
            Start_Button.Click += Start_Button_Click;
            // 
            // GetKart_Button
            // 
            GetKart_Button.Location = new System.Drawing.Point(19, 49);
            GetKart_Button.Name = "GetKart_Button";
            GetKart_Button.Size = new System.Drawing.Size(114, 23);
            GetKart_Button.TabIndex = 365;
            GetKart_Button.Text = "添加道具";
            GetKart_Button.UseVisualStyleBackColor = true;
            GetKart_Button.Click += GetKart_Button_Click;
            // 
            // button_ToggleTerminal
            // 
            button_ToggleTerminal.Location = new System.Drawing.Point(19, 78);
            button_ToggleTerminal.Name = "button_ToggleTerminal";
            button_ToggleTerminal.Size = new System.Drawing.Size(114, 23);
            button_ToggleTerminal.TabIndex = 366;
            button_ToggleTerminal.Text = "切换终端";
            button_ToggleTerminal.UseVisualStyleBackColor = true;
            button_ToggleTerminal.Click += button_ToggleTerminal_Click;
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
            Speed_comboBox.Location = new System.Drawing.Point(54, 107);
            Speed_comboBox.Name = "Speed_comboBox";
            Speed_comboBox.Size = new System.Drawing.Size(78, 20);
            Speed_comboBox.TabIndex = 367;
            Speed_comboBox.Text = "标准";
            Speed_comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            Speed_comboBox.SelectedIndexChanged += Speed_comboBox_SelectedIndexChanged;
            // 
            // Speed_label
            // 
            Speed_label.AutoSize = true;
            Speed_label.ForeColor = System.Drawing.Color.Blue;
            Speed_label.Location = new System.Drawing.Point(19, 111);
            Speed_label.Name = "Speed_label";
            Speed_label.Size = new System.Drawing.Size(59, 12);
            Speed_label.TabIndex = 368;
            Speed_label.Text = "速度:";
            // 
            // label_Client
            // 
            label_Client.AutoSize = true;
            label_Client.BackColor = System.Drawing.SystemColors.Control;
            label_Client.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label_Client.ForeColor = System.Drawing.Color.Blue;
            label_Client.Location = new System.Drawing.Point(2, 144);
            label_Client.Name = "label_Client";
            label_Client.Size = new System.Drawing.Size(47, 12);
            label_Client.TabIndex = 369;
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
            ClientVersion.Size = new System.Drawing.Size(0, 12);
            ClientVersion.TabIndex = 370;
            ClientVersion.Click += label_Client_Click;
            //
            // VersionLabel
            //
            VersionLabel.AutoSize = true;
            VersionLabel.BackColor = System.Drawing.SystemColors.Control;
            VersionLabel.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            VersionLabel.ForeColor = System.Drawing.Color.Red;
            VersionLabel.Location = new System.Drawing.Point(57, 160);
            VersionLabel.Name = "VersionLabel";
            VersionLabel.Size = new System.Drawing.Size(0, 12);
            VersionLabel.TabIndex = 371;
            VersionLabel.Click += GitHub_Click;
            // 
            // GitHub
            // 
            GitHub.AutoSize = true;
            GitHub.ForeColor = System.Drawing.Color.Blue;
            GitHub.Location = new System.Drawing.Point(213, 144);
            GitHub.Name = "GitHub";
            GitHub.Size = new System.Drawing.Size(41, 12);
            GitHub.TabIndex = 372;
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
            KartInfo.TabIndex = 373;
            KartInfo.Text = "KartInfo";
            KartInfo.Click += KartInfo_Click;
            //
            // Launcher_label
            //
            Launcher_label.AutoSize = true;
            Launcher_label.ForeColor = System.Drawing.Color.Blue;
            Launcher_label.Location = new System.Drawing.Point(2, 160);
            Launcher_label.Name = "Launcher_label";
            Launcher_label.Size = new System.Drawing.Size(47, 12);
            Launcher_label.TabIndex = 374;
            Launcher_label.Text = "Launcher:";
            Launcher_label.Click += GitHub_Click;
            // 
            // Launcher
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Control;
            ClientSize = new System.Drawing.Size(257, 180);
            Controls.Add(VersionLabel);
            Controls.Add(Launcher_label);
            Controls.Add(KartInfo);
            Controls.Add(GitHub);
            Controls.Add(Speed_comboBox);
            Controls.Add(Speed_label);
            Controls.Add(ClientVersion);
            Controls.Add(label_Client);
            Controls.Add(button_ToggleTerminal);
            Controls.Add(GetKart_Button);
            Controls.Add(Start_Button);
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
            Load_SpeedType();
            string executablePath = Process.GetCurrentProcess().MainModule.FileName;
            Load_KartExcData();
            StartingLoad_ALL.StartingLoad();
            PINFile val = new PINFile(this.kartRiderDirectory + "KartRider.pin");
            SetGameOption.Version = val.Header.MinorVersion;
            SetGameOption.Save_SetGameOption();
            ClientVersion.Text = SetGameOption.Version.ToString();
            DateTime compilationDate = File.GetLastWriteTime(executablePath);
            string formattedDate = compilationDate.ToString("yyMMdd");
            VersionLabel.Text = formattedDate;
            Console.WriteLine("Process: {0}", this.kartRiderDirectory + Launcher.KartRider);
            try
            {
                RouterListener.Start();
            }
            catch (Exception ex)
            {
                if (ex is System.Net.Sockets.SocketException)
                {
                    Console.WriteLine("This port has been used. Probably there is another launcher starts at the same time.");
                    Console.WriteLine("Exit with code 1.");
                    MessageBox.Show("已经有一个启动器在运行了。\n不可以同时运行多个启动器，因为通常每个套接字地址只允许使用一次\n点击确认以退出程序", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }
            }
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
                    foreach (PINFile.AuthMethod authMethod in val.AuthMethods)
                    {
                        Console.WriteLine("Changing IP Addr to local... {0}", authMethod.Name);
                        authMethod.LoginServers.Clear();
                        authMethod.LoginServers.Add(new PINFile.IPEndPoint
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

        public void Load_SpeedType()
        {
            string Load_Speed = AppDomain.CurrentDomain.BaseDirectory + "Profile\\Speed.ini";
            if (File.Exists(Load_Speed))
            {
                string textValue = System.IO.File.ReadAllText(Load_Speed);
                KeyValuePair<string, byte> speed = SpeedType.speedNames.FirstOrDefault(a => a.Value.ToString() == textValue);
                if (!String.IsNullOrEmpty(speed.Key))
                {
                    Speed_comboBox.Text = speed.Key;
                }
            }
            else
            {
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Profile"))
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Profile");
                }
                using (StreamWriter streamWriter = new StreamWriter(Load_Speed, false))
                {
                    streamWriter.Write(config.SpeedType);
                }
            }
        }

        public void Load_KartExcData()
        {
            string ModelMaxPath = AppDomain.CurrentDomain.BaseDirectory + @"Profile\ModelMax.xml";
            string ModelMax = Resources.ModelMax;
            if (!File.Exists(ModelMaxPath))
            {
                using (StreamWriter streamWriter = new StreamWriter(ModelMaxPath, false))
                {
                    streamWriter.Write(ModelMax);
                }
            }
            XmlFileUpdater.XmlUpdater updater = new XmlFileUpdater.XmlUpdater();
            updater.UpdateLocalXmlWithResource(ModelMaxPath, ModelMax);

            EnsureDefaultDataFileExists(AppDomain.CurrentDomain.BaseDirectory + @"Profile\AI.xml", CreateAIDefaultData);

            KartExcData.NewKart = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\NewKart.xml", LoadNewKart);
            KartExcData.TuneList = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\TuneData.xml", LoadTuneData);
            KartExcData.PlantList = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\PlantData.xml", LoadPlantData);
            KartExcData.LevelList = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\LevelData.xml", LoadLevelData);
            KartExcData.PartsList = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\PartsData.xml", LoadPartsData);
            KartExcData.Parts12List = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\Parts12Data.xml", LoadParts12Data);
            KartExcData.Level12List = LoadKartData(AppDomain.CurrentDomain.BaseDirectory + @"Profile\Level12Data.xml", LoadLevel12Data);
        }

        private void EnsureDefaultDataFileExists(string filePath, Action<string> createDefaultData)
        {
            if (!File.Exists(filePath))
            {
                createDefaultData(filePath);
            }
            else
            {
                try
                {
                    XDocument doc = XDocument.Load(filePath); // 解析XML内容

                    // 处理SpeedAI和SpeedSpec
                    XElement speedAI = doc.Root.Element("SpeedAI");
                    // 如果SpeedAI不存在，则创建它
                    if (speedAI == null)
                    {
                        speedAI = new XElement("SpeedAI");
                        doc.Root.Add(speedAI);
                    }

                    // 检查是否存在SpeedSpec，不存在则添加
                    bool hasSpeedSpec = speedAI.Elements("SpeedSpec").Any();
                    if (!hasSpeedSpec)
                    {
                        XElement speedSpecElement = new XElement("SpeedSpec");
                        speedSpecElement.SetAttributeValue("a", "1");
                        speedSpecElement.SetAttributeValue("b", "2300");
                        speedSpecElement.SetAttributeValue("c", "2930");
                        speedSpecElement.SetAttributeValue("d", "1.4");
                        speedSpecElement.SetAttributeValue("e", "1000");
                        speedSpecElement.SetAttributeValue("f", "1500");
                        speedAI.Add(speedSpecElement);
                    }

                    // 处理ItemAI和ItemSpec
                    XElement itemAI = doc.Root.Element("ItemAI");
                    // 如果ItemAI不存在，则创建它
                    if (itemAI == null)
                    {
                        itemAI = new XElement("ItemAI");
                        doc.Root.Add(itemAI);
                    }

                    // 检查是否存在ItemSpec，不存在则添加
                    bool hasItemSpec = itemAI.Elements("ItemSpec").Any();
                    if (!hasItemSpec)
                    {
                        XElement itemSpecElement = new XElement("ItemSpec");
                        itemSpecElement.SetAttributeValue("a", "0.8");
                        itemSpecElement.SetAttributeValue("b", "2300");
                        itemSpecElement.SetAttributeValue("c", "2930");
                        itemSpecElement.SetAttributeValue("d", "1.4");
                        itemSpecElement.SetAttributeValue("e", "1000");
                        itemSpecElement.SetAttributeValue("f", "1500");
                        itemAI.Add(itemSpecElement);
                    }

                    doc.Save(filePath); // 保存修改后的XML内容
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理XML时出错: {ex.Message}");
                }
            }
        }

        private void CreateAIDefaultData(string filePath)
        {
            try
            {
                // 创建XML文档
                XDocument doc = new XDocument(
                    // XML声明
                    new XDeclaration("1.0", "utf-8", null),
                    // 根元素AI
                    new XElement("AI",
                        // SpeedAI元素及其内容
                        new XElement("SpeedAI",
                            new XElement("SpeedSpec",
                                new XAttribute("a", "1"),
                                new XAttribute("b", "2300"),
                                new XAttribute("c", "2930"),
                                new XAttribute("d", "1.4"),
                                new XAttribute("e", "1000"),
                                new XAttribute("f", "1500")
                            )
                        ),
                        // ItemAI元素及其内容
                        new XElement("ItemAI",
                            new XElement("ItemSpec",
                                new XAttribute("a", "0.8"),
                                new XAttribute("b", "2300"),
                                new XAttribute("c", "2930"),
                                new XAttribute("d", "1.4"),
                                new XAttribute("e", "1000"),
                                new XAttribute("f", "1500")
                            )
                        )
                    )
                );
                doc.Save(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成XML文件时出错：{ex.Message}");
            }
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
                    Console.WriteLine(selectedSpeed);

                    string Load_Speed = AppDomain.CurrentDomain.BaseDirectory + "Profile\\Speed.ini";
                    if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Profile"))
                    {
                        Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Profile");
                    }
                    using (StreamWriter streamWriter = new StreamWriter(Load_Speed, false))
                    {
                        streamWriter.Write(config.SpeedType);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid speed type selected.");
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

        private void button_ToggleTerminal_Click(object sender, EventArgs e)
        {
            bool isConsoleVisible = Program.IsWindowVisible(Program.consoleHandle);
            isConsoleVisible = !isConsoleVisible;
            Program.ShowWindow(Program.consoleHandle, isConsoleVisible ? Program.SW_SHOW : Program.SW_HIDE);
            using (StreamWriter streamWriter = new StreamWriter(Program.Load_Console, false))
            {
                streamWriter.Write((isConsoleVisible ? "1" : "0"));
            }
        }
    }
}

