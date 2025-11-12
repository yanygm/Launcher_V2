using Launcher.App;
using Launcher.App.Constant;
using Launcher.App.ExcData;
using Launcher.App.Logger;
using Launcher.App.Profile;
using Launcher.App.Server;
using Launcher.App.Forms;
using Launcher.Library.Data;
using Launcher.Properties;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using static Launcher.App.Program;
using static Launcher.App.Utility.Utils;

namespace KartRider
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// KartRider.exe Path
        /// </summary>
        public static string KartRider = Path.GetFullPath(Path.Combine(RootDirectory, FileName.KartRider));

        /// <summary>
        /// PinFile Path
        /// </summary>
        /// </summary>
        public static string PinFile = Path.GetFullPath(Path.Combine(RootDirectory, FileName.PinFile));

        /// <summary>
        /// Backup PinFile Path
        /// </summary>
        public static string PinFileBak = Path.GetFullPath(Path.Combine(RootDirectory, FileName.PinFileBak));

        /// <summary>
        /// The PinFile Object
        /// </summary>
        private static PINFile PinFileData = new(PinFile);

        public MainForm()
        {
            // Initialize Component
            InitializeComponent();

            ClientVersion.Location = new Point(label_Client.Location.X + 70, label_Client.Location.Y);
            VersionLabel.Location = new Point(Launcher_label.Location.X + 70, Launcher_label.Location.Y);

            StartPosition = FormStartPosition.Manual;
            Rectangle screen = Screen.PrimaryScreen != null ? Screen.PrimaryScreen.WorkingArea : new Rectangle(0, 0, Width, Height);
            Location = new Point(screen.Width - Width, screen.Height - Height);
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (Process.GetProcessesByName("KartRider").Length != 0)
            {
                MessageBox.Show("跑跑卡丁车正在运行!\n为保证游戏文件不被损坏, 请结束跑跑卡丁车后再退出该程序!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
                return;
            }
            if (File.Exists(PinFileBak)) // restore PinFile
            {
                File.Delete(PinFile);
                File.Move(PinFileBak, PinFile);
            }
            ProfileService.Save();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            new Loader().ShowDialog();
            Load_KartExcData();

            ProfileService.ProfileConfig.GameOption.Version = PinFileData.Header.MinorVersion;
            ProfileService.Save();

            foreach (string key in SpeedType.speedNames.Keys)
            {
                Speed_comboBox.Items.Add(key);
            }
            KeyValuePair<string, byte> speed = SpeedType.speedNames.FirstOrDefault(a => a.Value == ProfileService.ProfileConfig.GameOption.SpeedType);
            if (!String.IsNullOrEmpty(speed.Key))
            {
                Speed_comboBox.Text = speed.Key;
            }
            ClientVersion.Text = $"P{ProfileService.ProfileConfig.GameOption.Version}";
            VersionLabel.Text = Constants.Version;

            Console.WriteLine($"[INFO] Game Client Version: {PinFileData.Header.MinorVersion}");
            Console.WriteLine($"[INFO] Launcher Version: {Constants.Version}");

            if (Constants.DBG)
                Console.WriteLine($"Config:\n{JsonConvert.SerializeObject(ProfileService.ProfileConfig, Newtonsoft.Json.Formatting.Indented)}");

            Console.WriteLine($"[INFO] Process: {KartRider}");

            try
            {
                RouterListener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
                if (ex is System.Net.Sockets.SocketException)
                {
                    MsgMultiInstance();
                }
            }
        }

        private void Start_Button_Click(object sender, EventArgs e)
        {
            if (Process.GetProcessesByName("KartRider").Length != 0)
            {
                MsgKartIsRunning();
                return;
            }
            if (!CheckGameAvailability(Program.RootDirectory))
            {
                MsgFileNotFound();
                return;
            }
            new Thread(() =>
            {
                Start_Button.Enabled = false;
                GetKart_Button.Enabled = false;

                Console.WriteLine("Backing up old PinFile...");
                if (File.Exists(PinFileBak))
                {
                    File.Delete(PinFile);
                    File.Move(PinFileBak, PinFile);
                }
                File.Copy(PinFile, PinFileBak);
                Console.WriteLine($"Backup PinFile: {PinFileBak}");

                PinFileData = new(PinFile);
                foreach (PINFile.AuthMethod authMethod in PinFileData.AuthMethods)
                {
                    Console.WriteLine($"Changing IP Address to local... {authMethod.Name}");
                    foreach (PINFile.IPEndPoint loginServer in authMethod.LoginServers)
                    {
                        Console.WriteLine($"{loginServer} -> 127.0.0.1:39312");
                    }
                    authMethod.LoginServers.Clear();
                    authMethod.LoginServers.Add(new PINFile.IPEndPoint
                    {
                        IP = "127.0.0.1",
                        Port = 39312
                    });
                    Console.WriteLine($"All Changed to {authMethod.LoginServers[0]} \n");
                }
                Console.WriteLine("All IP Addresses Changed to Local");

                Console.WriteLine("Scanning Bml Objects in PinFile...");
                foreach (BmlObject bml in PinFileData.BmlObjects)
                {
                    for (int i = bml.SubObjects.Count - 1; i >= 0; i--)
                    {
                        Console.WriteLine($"Found {bml.SubObjects[i].Item1} in {bml.Name}");
                        if (bml.SubObjects[i].Item1 != "NgsOn")
                            continue;
                        Console.WriteLine($"Removing {bml.SubObjects[i].Item1}");
                        bml.SubObjects.RemoveAt(i);
                    }
                }
                Console.WriteLine();

                File.WriteAllBytes(PinFile, PinFileData.GetEncryptedData());

                // original passport:aHR0cHM6Ly9naXRodWIuY29tL3lhbnlnbS9MYXVuY2hlcl9WMi9yZWxlYXNlcw==
                ProcessStartInfo startInfo = new ProcessStartInfo(KartRider, "TGC -region:3 -passport:OFFLINE")
                {
                    WorkingDirectory = Path.GetFullPath(Program.RootDirectory),
                    UseShellExecute = true,
                    Verb = "runas"
                };
                try
                {
                    Process.Start(startInfo);
                    Thread.Sleep(1000);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    // 用户取消了UAC提示或没有权限
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("用户积极取消操作, 或者没有Administer权限.");
                }
                finally
                {
                    Start_Button.Enabled = true;
                    GetKart_Button.Enabled = true;
                }
            }).Start();
        }

        private void GetKart_Button_Click(object sender, EventArgs e)
        {
            GetKartDlg = new();
            GetKartDlg.ShowDialog();
        }

        public void Load_KartExcData()
        {
            Console.WriteLine("正在读取配置文件...");
            string ModelMaxPath = AppDomain.CurrentDomain.BaseDirectory + @"Profile\ModelMax.xml";
            string ModelMax = Resources.ModelMax;
            if (!File.Exists(FileName.ModelMax_LoadFile))
            {
                using (StreamWriter streamWriter = new StreamWriter(FileName.ModelMax_LoadFile, false))
                {
                    streamWriter.Write(ModelMax);
                }
            }
            XmlUpdater updater = new XmlUpdater();
            updater.UpdateLocalXmlWithResource(FileName.ModelMax_LoadFile, ModelMax);

            EnsureDefaultDataFileExists(FileName.AI_LoadFile, CreateAIDefaultData);

            KartExcData.NewKart = LoadKartData(FileName.NewKart_LoadFile, LoadNewKart);
            KartExcData.TuneList = LoadKartData(FileName.TuneData_LoadFile, LoadTuneData);
            KartExcData.PlantList = LoadKartData(FileName.PlantData_LoadFile, LoadPlantData);
            KartExcData.LevelList = LoadKartData(FileName.LevelData_LoadFile, LoadLevelData);
            KartExcData.PartsList = LoadKartData(FileName.PartsData_LoadFile, LoadPartsData);
            KartExcData.Parts12List = LoadKartData(FileName.Parts12Data_LoadFile, LoadParts12Data);
            KartExcData.Level12List = LoadKartData(FileName.Level12Data_LoadFile, LoadLevel12Data);
            SpecialKartConfig.SaveConfigToFile(FileName.SpecialKartConfig);
            MultiPlayer.kartConfig = SpecialKartConfig.LoadConfigFromFile(FileName.SpecialKartConfig);
            Console.WriteLine("配置文件读取完成!");
        }

        private void EnsureDefaultDataFileExists(string filePath, Action<string> createDefaultData)
        {
            if (!File.Exists(filePath))
            {
                createDefaultData(filePath);
                return;
            }
            try
            {
                XDocument doc = XDocument.Load(filePath); // 解析XML内容

                // 处理SpeedAI和SpeedSpec
                XElement speedAI = doc.Root.Element("SpeedAI");
                // 如果SpeedAI不存在, 则创建它
                if (speedAI == null)
                {
                    speedAI = new XElement("SpeedAI");
                    doc.Root.Add(speedAI);
                }

                // 检查是否存在SpeedSpec, 不存在则添加
                bool hasSpeedSpec = speedAI.Elements("SpeedSpec").Any();
                if (!hasSpeedSpec)
                {
                    XElement speedSpecElement = new XElement("SpeedSpec");
                    speedSpecElement.SetAttributeValue("a", "1");
                    speedSpecElement.SetAttributeValue("b", "2500");
                    speedSpecElement.SetAttributeValue("c", "2970");
                    speedSpecElement.SetAttributeValue("d", "1.5");
                    speedSpecElement.SetAttributeValue("e", "1000");
                    speedSpecElement.SetAttributeValue("f", "1500");
                    speedAI.Add(speedSpecElement);
                }

                // 处理ItemAI和ItemSpec
                XElement itemAI = doc.Root.Element("ItemAI");
                // 如果ItemAI不存在, 则创建它
                if (itemAI == null)
                {
                    itemAI = new XElement("ItemAI");
                    doc.Root.Add(itemAI);
                }

                // 检查是否存在ItemSpec, 不存在则添加
                bool hasItemSpec = itemAI.Elements("ItemSpec").Any();
                if (!hasItemSpec)
                {
                    XElement itemSpecElement = new XElement("ItemSpec");
                    itemSpecElement.SetAttributeValue("a", "0.8");
                    itemSpecElement.SetAttributeValue("b", "2500");
                    itemSpecElement.SetAttributeValue("c", "2970");
                    itemSpecElement.SetAttributeValue("d", "1.5");
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
                                new XAttribute("b", "2500"),
                                new XAttribute("c", "2970"),
                                new XAttribute("d", "1.5"),
                                new XAttribute("e", "1000"),
                                new XAttribute("f", "1500")
                           )
                       ),
                        // ItemAI元素及其内容
                        new XElement("ItemAI",
                            new XElement("ItemSpec",
                                new XAttribute("a", "0.8"),
                                new XAttribute("b", "2500"),
                                new XAttribute("c", "2970"),
                                new XAttribute("d", "1.5"),
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
                Console.WriteLine($"生成 XML 文件时出错: {ex.Message}");
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
                    Console.WriteLine(@"读取 Profile\NewKart.xml 文件失败, 建议删除文件后重试!");
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
                    Console.WriteLine(@"读取 Profile\TuneData.xml 文件失败, 建议删除文件后重试!");
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
                    Console.WriteLine(@"读取 Profile\PlantData.xml 文件失败, 建议删除文件后重试!");
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
                    Console.WriteLine(@"读取 Profile\LevelData.xml 文件失败, 建议删除文件后重试!");
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
                    Console.WriteLine(@"读取 Profile\PartsData.xml 文件失败, 建议删除文件后重试!");
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
                    Console.WriteLine(@"读取 Profile\Parts12Data.xml 文件失败, 建议删除文件后重试!");
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
                    Console.WriteLine(@"读取 Profile\Level12Data.xml 文件失败, 建议删除文件后重试!");
                }
            }
            return result;
        }

        private void Speed_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Speed_comboBox.SelectedItem != null)
            {
                string selectedSpeed = Speed_comboBox.SelectedItem.ToString() ?? "";
                if (SpeedType.speedNames.TryGetValue(selectedSpeed, out byte value))
                {
                    ProfileService.ProfileConfig.GameOption.SpeedType = value;
                    ProfileService.Save();
                    Console.WriteLine($"速度更改为: {selectedSpeed}.");
                }
                else
                {
                    Console.WriteLine("未知/错误的速度选项");
                }
            }
        }

        private void GitHub_Click(object sender, EventArgs e)
        {
            string url = $"https://github.com/{Constants.Owner}/{Constants.Repo}";
            TryOpenUrl(url);
        }
        private void VersionLabel_MouseEnter(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(VersionLabel, "点击前往GitHub Release");
        }

        private void GitHub_Release_Click(object sender, EventArgs e)
        {
            string url = $"https://github.com/{Constants.Owner}/{Constants.Repo}/releases";
            TryOpenUrl(url);
        }

        private void GitHub_MouseEnter(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(GitHub, "点击前往GitHub仓库");
        }

        private void KartInfo_Click(object sender, EventArgs e)
        {
            string url = "https://kartinfo.me/thread-9369-1-1.html";
            TryOpenUrl(url);
        }

        private void KartInfo_MouseEnter(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(KartInfo, "点击前往KartInfo论坛");
        }

        private void label_Client_Click(object sender, EventArgs e)
        {
            string url = "https://github.com/brownsugar/popkart-client-archive/releases";
            TryOpenUrl(url);
        }

        private void ClientVersion_MouseEnter(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(ClientVersion, "点击前往BrownSugar的跑跑卡丁车存档");
        }

        private void label_Docs_Click(object sender, EventArgs e)
        {
            string url = "https://themagicflute.github.io/Launcher_V2/";
            TryOpenUrl(url);
        }

        private void label_TimeAttackLog_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(FileName.TimeAttackLog) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                if (ex is System.ComponentModel.Win32Exception)
                {
                    Console.WriteLine("计时日志文件未找到, 请进行计时后再查看!");
                }
                else
                {
                    Console.WriteLine($"查找计时日志时发生错误: {ex.Message}");
                }
            }
        }

        private void button_More_Options_Click(object sender, EventArgs e)
        {
            OptionsDlg = new();
            OptionsDlg.ShowDialog();
        }

        private void ConsoleLogger_Click(object sender, EventArgs e)
        {
            string logFileName = CachedConsoleWriter.SaveToFile();
            CachedConsoleWriter.cachedWriter.ClearCache();
            if (logFileName == string.Empty)
                MessageBox.Show("日志写入失败!", "写入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show($"日志已经写入了{logFileName}", "写入完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
