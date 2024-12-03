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
			// label_DeveloperName
			// 
			label_DeveloperName.AutoSize = true;
			label_DeveloperName.BackColor = System.Drawing.SystemColors.Control;
			label_DeveloperName.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			label_DeveloperName.ForeColor = System.Drawing.Color.Blue;
			label_DeveloperName.Location = new System.Drawing.Point(2, 160);
			label_DeveloperName.Name = "label_DeveloperName";
			label_DeveloperName.Size = new System.Drawing.Size(53, 12);
			label_DeveloperName.TabIndex = 367;
			label_DeveloperName.Text = "Version:";
			// 
			// MinorVersion
			// 
			MinorVersion.AutoSize = true;
			MinorVersion.BackColor = System.Drawing.SystemColors.Control;
			MinorVersion.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			MinorVersion.ForeColor = System.Drawing.Color.Red;
			MinorVersion.Location = new System.Drawing.Point(55, 160);
			MinorVersion.Name = "MinorVersion";
			MinorVersion.Size = new System.Drawing.Size(0, 12);
			MinorVersion.TabIndex = 367;
			// 
			// comboBox1
			// 
			comboBox1.FormattingEnabled = true;
			comboBox1.Location = new System.Drawing.Point(19, 78);
			comboBox1.Name = "comboBox1";
			comboBox1.Size = new System.Drawing.Size(114, 20);
			comboBox1.TabIndex = 368;
			comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
			comboBox1.Items.Add("Integration");
			comboBox1.Items.Add("S0");
			comboBox1.Items.Add("S1");
			comboBox1.Items.Add("S2");
			comboBox1.Items.Add("S3");
			comboBox1.Text = "Integration";
			// 
			// Launcher
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			AutoScaleMode = AutoScaleMode.Font;
			BackColor = System.Drawing.SystemColors.Control;
			ClientSize = new System.Drawing.Size(257, 180);
			Controls.Add(comboBox1);
			Controls.Add(MinorVersion);
			Controls.Add(label_DeveloperName);
			Controls.Add(GetKart_Button);
			Controls.Add(Start_Button);
			Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
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
			if (!File.Exists(@"Profile\AI.xml"))
			{
				XmlTextWriter writer = new XmlTextWriter(@"Profile\AI.xml", System.Text.Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				writer.WriteStartDocument();
				writer.WriteStartElement("AI");
				writer.WriteEndElement();
				writer.Close();
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
			if (File.Exists(@"Profile\NewKart.xml"))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(@"Profile\NewKart.xml");
				KartExcData.NewKart = new List<List<short>>();
				if (!(doc.GetElementsByTagName("Kart") == null))
				{
					XmlNodeList lis = doc.GetElementsByTagName("Kart");
					foreach (XmlNode xn in lis)
					{
						XmlElement xe = (XmlElement)xn;
						short id = short.Parse(xe.GetAttribute("id"));
						short sn = short.Parse(xe.GetAttribute("sn"));
						List<short> AddList = new List<short>{ id, sn };
						KartExcData.NewKart.Add(AddList);
					}
				}
			}
			if (File.Exists(@"Profile\TuneData.xml"))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(@"Profile\TuneData.xml");
				if (!(doc.GetElementsByTagName("Kart") == null))
				{
					XmlNodeList lis = doc.GetElementsByTagName("Kart");
					KartExcData.TuneList = new List<List<short>>();
					foreach (XmlNode xn in lis)
					{
						XmlElement xe = (XmlElement)xn;
						short i = short.Parse(xe.GetAttribute("id"));
						short sn = short.Parse(xe.GetAttribute("sn"));
						short tune1 = short.Parse(xe.GetAttribute("tune1"));
						short tune2 = short.Parse(xe.GetAttribute("tune2"));
						short tune3 = short.Parse(xe.GetAttribute("tune3"));
						short slot1 = short.Parse(xe.GetAttribute("slot1"));
						short count1 = short.Parse(xe.GetAttribute("count1"));
						short slot2 = short.Parse(xe.GetAttribute("slot2"));
						short count2 = short.Parse(xe.GetAttribute("count2"));
						List<short> AddList = new List<short>{ i, sn, tune1, tune2, tune3, slot1, count1, slot2, count2 };
						KartExcData.TuneList.Add(AddList);
					}
				}
			}
			if (File.Exists(@"Profile\PlantData.xml"))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(@"Profile\PlantData.xml");
				if (!(doc.GetElementsByTagName("Kart") == null))
				{
					XmlNodeList lis = doc.GetElementsByTagName("Kart");
					KartExcData.PlantList = new List<List<short>>();
					foreach (XmlNode xn in lis)
					{
						XmlElement xe = (XmlElement)xn;
						short i = short.Parse(xe.GetAttribute("id"));
						short sn = short.Parse(xe.GetAttribute("sn"));
						short item1 = short.Parse(xe.GetAttribute("item1"));
						short item_id1 = short.Parse(xe.GetAttribute("item_id1"));
						short item2 = short.Parse(xe.GetAttribute("item2"));
						short item_id2 = short.Parse(xe.GetAttribute("item_id2"));
						short item3 = short.Parse(xe.GetAttribute("item3"));
						short item_id3 = short.Parse(xe.GetAttribute("item_id3"));
						short item4 = short.Parse(xe.GetAttribute("item4"));
						short item_id4 = short.Parse(xe.GetAttribute("item_id4"));
						List<short> AddList = new List<short>{ i, sn, item1, item_id1, item2, item_id2, item3, item_id3, item4, item_id4 };
						KartExcData.PlantList.Add(AddList);
					}
				}
			}
			if (File.Exists(@"Profile\LevelData.xml"))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(@"Profile\LevelData.xml");
				if (!(doc.GetElementsByTagName("Kart") == null))
				{
					XmlNodeList lis = doc.GetElementsByTagName("Kart");
					KartExcData.LevelList = new List<List<short>>();
					foreach (XmlNode xn in lis)
					{
						XmlElement xe = (XmlElement)xn;
						short i = short.Parse(xe.GetAttribute("id"));
						short sn = short.Parse(xe.GetAttribute("sn"));
						short level = short.Parse(xe.GetAttribute("level"));
						short point = short.Parse(xe.GetAttribute("point"));
						short v1 = short.Parse(xe.GetAttribute("v1"));
						short v2 = short.Parse(xe.GetAttribute("v2"));
						short v3 = short.Parse(xe.GetAttribute("v3"));
						short v4 = short.Parse(xe.GetAttribute("v4"));
						short Effect = short.Parse(xe.GetAttribute("Effect"));
						List<short> AddList = new List<short>{ i, sn, level, point, v1, v2, v3, v4, Effect };
						KartExcData.LevelList.Add(AddList);
					}
				}
			}
			if (File.Exists(@"Profile\PartsData.xml"))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(@"Profile\PartsData.xml");
				if (!(doc.GetElementsByTagName("Kart") == null))
				{
					XmlNodeList lis = doc.GetElementsByTagName("Kart");
					KartExcData.PartsList = new List<List<short>>();
					foreach (XmlNode xn in lis)
					{
						XmlElement xe = (XmlElement)xn;
						short i = short.Parse(xe.GetAttribute("id"));
						short sn = short.Parse(xe.GetAttribute("sn"));
						short Item_Id1 = short.Parse(xe.GetAttribute("Item_Id1"));
						byte Grade1 = byte.Parse(xe.GetAttribute("Grade1"));
						short PartsValue1 = short.Parse(xe.GetAttribute("PartsValue1"));
						short Item_Id2 = short.Parse(xe.GetAttribute("Item_Id2"));
						byte Grade2 = byte.Parse(xe.GetAttribute("Grade2"));
						short PartsValue2 = short.Parse(xe.GetAttribute("PartsValue2"));
						short Item_Id3 = short.Parse(xe.GetAttribute("Item_Id3"));
						byte Grade3 = byte.Parse(xe.GetAttribute("Grade3"));
						short PartsValue3 = short.Parse(xe.GetAttribute("PartsValue3"));
						short Item_Id4 = short.Parse(xe.GetAttribute("Item_Id4"));
						byte Grade4 = byte.Parse(xe.GetAttribute("Grade4"));
						short PartsValue4 = short.Parse(xe.GetAttribute("PartsValue4"));
						short partsCoating = byte.Parse(xe.GetAttribute("partsCoating"));
						short partsTailLamp = short.Parse(xe.GetAttribute("partsTailLamp"));
						List<short> AddList = new List<short>{ i, sn, Item_Id1, Grade1, PartsValue1, Item_Id2, Grade2, PartsValue2, Item_Id3, Grade3, PartsValue3, Item_Id4, Grade4, PartsValue4, partsCoating, partsTailLamp };
						KartExcData.PartsList.Add(AddList);
					}
				}
			}
			if (File.Exists(@"Profile\Parts12Data.xml"))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(@"Profile\Parts12Data.xml");
				if (!(doc.GetElementsByTagName("Kart") == null))
				{
					XmlNodeList lis = doc.GetElementsByTagName("Kart");
					KartExcData.Parts12List = new List<List<short>>();
					foreach (XmlNode xn in lis)
					{
						XmlElement xe = (XmlElement)xn;
						short i = short.Parse(xe.GetAttribute("id"));
						short sn = short.Parse(xe.GetAttribute("sn"));
						short Engine = short.Parse(xe.GetAttribute("Engine"));
						short Handle = short.Parse(xe.GetAttribute("Handle"));
						short Wheel = short.Parse(xe.GetAttribute("Wheel"));
						short Booster = short.Parse(xe.GetAttribute("Booster"));
						short Coating = short.Parse(xe.GetAttribute("Coating"));
						short TailLamp = short.Parse(xe.GetAttribute("TailLamp"));
						short BoosterWave = short.Parse(xe.GetAttribute("BoosterWave"));
						List<short> AddList = new List<short>{ i, sn, Engine, Handle, Wheel, Booster, Coating, TailLamp, BoosterWave };
						KartExcData.Parts12List.Add(AddList);
					}
				}
			}
			if (File.Exists(@"Profile\Level12Data.xml"))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(@"Profile\Level12Data.xml");
				if (!(doc.GetElementsByTagName("Kart") == null))
				{
					XmlNodeList lis = doc.GetElementsByTagName("Kart");
					KartExcData.Level12List = new List<List<short>>();
					foreach (XmlNode xn in lis)
					{
						XmlElement xe = (XmlElement)xn;
						short i = short.Parse(xe.GetAttribute("id"));
						short sn = short.Parse(xe.GetAttribute("sn"));
						short Level = short.Parse(xe.GetAttribute("Level"));
						short Skill1 = short.Parse(xe.GetAttribute("Skill1"));
						short SkillLevel1 = short.Parse(xe.GetAttribute("SkillLevel1"));
						short Skill2 = short.Parse(xe.GetAttribute("Skill2"));
						short SkillLevel2 = short.Parse(xe.GetAttribute("SkillLevel2"));
						short Skill3 = short.Parse(xe.GetAttribute("Skill3"));
						short SkillLevel3 = short.Parse(xe.GetAttribute("SkillLevel3"));
						short Point = short.Parse(xe.GetAttribute("Point"));
						List<short> AddList = new List<short> { i, sn, Level, Skill1, SkillLevel1, Skill2, SkillLevel2, Skill3, SkillLevel3, Point };
						KartExcData.Level12List.Add(AddList);
					}
				}
			}
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboBox1.SelectedItem != null)
			{
				Console.WriteLine(comboBox1.SelectedItem.ToString());
				if (comboBox1.SelectedItem.ToString() == "Integration")
				{
					config.SpeedType = 7;
				}
				else if(comboBox1.SelectedItem.ToString() == "S0")
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
	}
}
