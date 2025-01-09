using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO.Packet;
using KartRider;
using ExcData;
using Set_Data;
using System.Xml;
using System.Linq;

namespace RiderData
{
	public static class NewRider
	{
		public static void LoadItemData()
		{
			KartExcData.Tune_ExcData();
			KartExcData.Plant_ExcData();
			KartExcData.Level_ExcData();
			KartExcData.Parts_ExcData();
			KartExcData.Level12_ExcData();
			KartExcData.Parts12_ExcData();
			NewRider.character();
			NewRider.color();
			NewRider.plate();
			NewRider.slotChanger();
			NewRider.goggle();
			NewRider.balloon();
			NewRider.headBand();
			NewRider.headPhone();
			NewRider.ticket();
			NewRider.upgradeKit();
			NewRider.handGearL();
			NewRider.uniform();
			NewRider.decal();
			NewRider.pet();
			NewRider.initialCard();
			NewRider.card();
			NewRider.aura();
			NewRider.skidMark();
			NewRider.roomCard();
			NewRider.ridColor();
			NewRider.rpLucciBonus();
			NewRider.socket();
			NewRider.tune();
			NewRider.resetSocket();
			NewRider.tuneEnginePatch();
			NewRider.tuneHandle();
			NewRider.tuneWheel();
			NewRider.tuneSupportKit();
			NewRider.enchantProtect();
			NewRider.flyingPet();
			NewRider.enchantProtect2();
			NewRider.tachometer();
			NewRider.partsCoating();
			NewRider.partsTailLamp();
			NewRider.dye();
			NewRider.slotBg();
			NewRider.partsPiece();
			NewRider.partsEngine12();
			NewRider.partsHandle12();
			NewRider.partsWheel12();
			NewRider.partsBooster12();
			NewRider.partsCoating12();
			NewRider.partsTailLamp12();
			NewRider.partsBoosterEffect12();
			NewRider.ethisItem();
			NewRider.XUniquePartsData();
			NewRider.XLegendPartsData();
			NewRider.XRarePartsData();
			NewRider.XNormalPartsData();
			NewRider.V1UniquePartsData();
			NewRider.V1LegendPartsData();
			NewRider.V1RarePartsData();
			NewRider.V1NormalPartsData();
			NewRider.NewKart1();
			NewRider.NewKart2();
			NewRider.NewRiderData();//라이더 인식
		}

		public static void NewRiderData()
		{
			using (OutPacket oPacket = new OutPacket("PrGetRider"))
			{
				oPacket.WriteByte(1);
				oPacket.WriteByte(0);
				oPacket.WriteString(SetRider.Nickname);
				oPacket.WriteShort(0);
				oPacket.WriteShort(0);
				oPacket.WriteShort(SetRider.Emblem1);
				oPacket.WriteShort(SetRider.Emblem2);
				oPacket.WriteShort(0);
				GameSupport.GetRider(oPacket);
				oPacket.WriteShort(0);
				oPacket.WriteString(SetRider.Card);
				oPacket.WriteUInt(SetRider.Lucci);
				oPacket.WriteUInt(SetRider.RP);
				oPacket.WriteBytes(new byte[94]);
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void NewKart1()
		{
			short sn = 1;
			int range = 100;//分批次数
			int times = KartExcData.kart.Count / range + (KartExcData.kart.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = KartExcData.kart.GetRange(i * range, (i + 1) * range > KartExcData.kart.Count ? (KartExcData.kart.Count - i * range) : range);
				int Count = tempList.Count;
				using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
				{
					outPacket.WriteByte(1);
					outPacket.WriteInt(Count);
					foreach (var Kart in tempList)
					{
						outPacket.WriteShort(3);
						outPacket.WriteShort(Kart);
						outPacket.WriteShort(sn);
						outPacket.WriteShort(1);//数量
						outPacket.WriteShort(0);
						outPacket.WriteShort(-1);
						outPacket.WriteShort(0);
						outPacket.WriteShort(0);
						outPacket.WriteShort(0);
					}
					RouterListener.MySession.Client.Send(outPacket);
				}
			}
		}

		public static void NewKart2()
		{
			int range = 100;//分批次数
			int times = KartExcData.NewKart.Count / range + (KartExcData.NewKart.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = KartExcData.NewKart.GetRange(i * range, (i + 1) * range > KartExcData.NewKart.Count ? (KartExcData.NewKart.Count - i * range) : range);
				int Count = tempList.Count;
				using (OutPacket outPacket = new OutPacket("PrRequestKartInfoPacket"))
				{
					outPacket.WriteByte(1);
					outPacket.WriteInt(Count);
					foreach (var Kart in tempList)
					{
						outPacket.WriteShort(3);
						outPacket.WriteShort(Kart[0]);
						outPacket.WriteShort(Kart[1]);
						outPacket.WriteShort(1);//数量
						outPacket.WriteShort(0);
						outPacket.WriteShort(-1);
						outPacket.WriteShort(0);
						outPacket.WriteShort(0);
						outPacket.WriteShort(0);
					}
					RouterListener.MySession.Client.Send(outPacket);
				}
			}
		}

		public static void color()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.color)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(2, item);
		}

		public static void dye()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.dye)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(70, item);
		}

		public static void character()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.character)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(1, item);
		}

		public static void pet()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.pet)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(21, item);
		}

		public static void initialCard()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.initialCard)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(22, item);
		}

		public static void flyingPet()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.flyingPet)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(52, item);
		}

		public static void enchantProtect2()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.enchantProtect2)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(53, item);
		}

		public static void uniform()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.uniform)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(18, item);
		}

		public static void aura()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.aura)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(26, item);
		}

		public static void skidMark()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.skidMark)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(27, item);
		}

		public static void plate()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.plate)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(4, item);
		}

		public static void balloon()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.balloon)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(9, item);
		}

		public static void goggle()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.goggle)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(8, item);
		}

		public static void headBand()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.headBand)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(11, item);
		}

		public static void headPhone()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.headPhone)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(12, item);
		}

		public static void handGearL()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.handGearL)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(16, item);
		}

		public static void roomCard()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.roomCard)
			{
				short sn = 0;
				short num = 1;
				if (id != 50 && id != 37)
				{
					List<short> add = new List<short> { id, sn, num };
					item.Add(add);
				}
			}
			LoRpGetRiderItemPacket(28, item);
		}

		public static void ridColor()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.ridColor)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(31, item);
		}

		public static void rpLucciBonus()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.rpLucciBonus)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(32, item);
		}

		public static void slotChanger()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.slotChanger)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(7, item);
		}

		public static void slotBg()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.slotBg)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(71, item);
		}

		public static void decal()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.decal)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(20, item);
		}

		public static void card()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.card)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				if (id == 1 || id == 3 || id == 89 || id == 97 || id == 98 || id == 99 || id == 100 || id == 106)
				{
					List<short> add = new List<short> { id, sn, num };
					item.Add(add);
				}
			}
			LoRpGetRiderItemPacket(23, item);
		}

		public static void ticket()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.ticket)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(13, item);
		}

		public static void tachometer()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.tachometer)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(61, item);
		}

		public static void tuneEnginePatch()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.tuneEnginePatch)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(43, item);
		}

		public static void tuneHandle()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.tuneHandle)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(44, item);
		}

		public static void tuneWheel()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.tuneWheel)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(45, item);
		}

		public static void tuneSupportKit()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.tuneSupportKit)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(46, item);
		}

		public static void enchantProtect()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.enchantProtect)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(49, item);
		}

		public static void socket()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.socket)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(37, item);
		}

		public static void tune()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.tune)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(38, item);
		}

		public static void resetSocket()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.resetSocket)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(39, item);
		}

		public static void upgradeKit()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.upgradeKit)
			{
				short sn = 0;
				short num = 1;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(14, item);
		}

		public static void partsPiece()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.partsPiece)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(67, item);
		}

		public static void partsCoating()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.partsCoating)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(68, item);
		}

		public static void partsTailLamp()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.partsTailLamp)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(69, item);
		}

		public static void partsEngine12()
		{
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				int count = KartExcData.partsEngine12.Count;
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(count);
				for (int i = 0; i < count; i++)
				{
					short id = KartExcData.partsEngine12[i];
					oPacket.WriteShort(72);
					oPacket.WriteShort(id);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					if (id < 11)
					{
						oPacket.WriteByte(4);
					}
					else if (id < 21)
					{
						oPacket.WriteByte(3);
					}
					else if (id < 31)
					{
						oPacket.WriteByte(2);
					}
					else if (id < 41)
					{
						oPacket.WriteByte(1);
					}
					oPacket.WriteShort(V2Spec.Get12Parts(id));
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void partsHandle12()
		{
			int count = KartExcData.partsHandle12.Count;
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(count);
				for (int i = 0; i < count; i++)
				{
					short id = KartExcData.partsHandle12[i];
					oPacket.WriteShort(73);
					oPacket.WriteShort(id);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					if (id < 11)
					{
						oPacket.WriteByte(4);
					}
					else if (id < 21)
					{
						oPacket.WriteByte(3);
					}
					else if (id < 31)
					{
						oPacket.WriteByte(2);
					}
					else if (id < 41)
					{
						oPacket.WriteByte(1);
					}
					oPacket.WriteShort(V2Spec.Get12Parts(id));
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void partsWheel12()
		{
			int count = KartExcData.partsWheel12.Count;
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(count);
				for (int i = 0; i < count; i++)
				{
					short id = KartExcData.partsWheel12[i];
					oPacket.WriteShort(74);
					oPacket.WriteShort(id);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					if (id < 11)
					{
						oPacket.WriteByte(4);
					}
					else if (id < 21)
					{
						oPacket.WriteByte(3);
					}
					else if (id < 31)
					{
						oPacket.WriteByte(2);
					}
					else if (id < 41)
					{
						oPacket.WriteByte(1);
					}
					oPacket.WriteShort(V2Spec.Get12Parts(id));
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void partsBooster12()
		{
			int count = KartExcData.partsBooster12.Count;
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(count);
				for (int i = 0; i < count; i++)
				{
					short id = KartExcData.partsBooster12[i];
					oPacket.WriteShort(75);
					oPacket.WriteShort(id);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					if (id < 11)
					{
						oPacket.WriteByte(4);
					}
					else if (id < 21)
					{
						oPacket.WriteByte(3);
					}
					else if (id < 31)
					{
						oPacket.WriteByte(2);
					}
					else if (id < 41)
					{
						oPacket.WriteByte(1);
					}
					oPacket.WriteShort(V2Spec.Get12Parts(id));
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void partsCoating12()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.partsCoating12)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(76, item);
		}

		public static void partsTailLamp12()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.partsTailLamp12)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(77, item);
		}

		public static void partsBoosterEffect12()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.partsBoosterEffect12)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(78, item);
		}

		public static void ethisItem()
		{
			List<List<short>> item = new List<List<short>>();
			foreach (var id in KartExcData.ethisItem)
			{
				short sn = 0;
				short num = SetRider.SlotChanger;
				List<short> add = new List<short> { id, sn, num };
				item.Add(add);
			}
			LoRpGetRiderItemPacket(79, item);
		}

		public static void XUniquePartsData()
		{
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(40);
				byte Grade = 1;
				//-----------------------------------------------------------------X 유니크 파츠
				for (short i = 1053; i <= 1080; i += 3)
				{
					oPacket.WriteShort(63);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1053; i <= 1080; i += 3)
				{
					oPacket.WriteShort(64);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1053; i <= 1080; i += 3)
				{
					oPacket.WriteShort(65);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1053; i <= 1080; i += 3)
				{
					oPacket.WriteShort(66);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void XLegendPartsData()
		{
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(40);
				byte Grade = 2;
				//-----------------------------------------------------------------X 레전드 파츠
				for (short i = 1005; i <= 1050; i += 5)
				{
					oPacket.WriteShort(63);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1005; i <= 1050; i += 5)
				{
					oPacket.WriteShort(64);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1005; i <= 1050; i += 5)
				{
					oPacket.WriteShort(65);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1005; i <= 1050; i += 5)
				{
					oPacket.WriteShort(66);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void XRarePartsData()
		{
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(40);
				byte Grade = 3;
				//-----------------------------------------------------------------X 레어 파츠
				for (short i = 910; i <= 1000; i += 10)
				{
					oPacket.WriteShort(63);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 910; i <= 1000; i += 10)
				{
					oPacket.WriteShort(64);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 910; i <= 1000; i += 10)
				{
					oPacket.WriteShort(65);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 910; i <= 1000; i += 10)
				{
					oPacket.WriteShort(66);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void XNormalPartsData()
		{
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(40);
				byte Grade = 4;
				//-----------------------------------------------------------------X 일반 파츠
				for (short i = 810; i <= 900; i += 10)
				{
					oPacket.WriteShort(63);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 810; i <= 900; i += 10)
				{
					oPacket.WriteShort(64);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 810; i <= 900; i += 10)
				{
					oPacket.WriteShort(65);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 810; i <= 900; i += 10)
				{
					oPacket.WriteShort(66);
					oPacket.WriteShort(1);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		//-----------------------------------------------------------------------------------------------V1 파츠 관련
		public static void V1UniquePartsData()
		{
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(40);
				byte Grade = 1;
				//-----------------------------------------------------------------V1 유니크 파츠
				for (short i = 1153; i <= 1180; i += 3)
				{
					oPacket.WriteShort(63);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1053; i <= 1080; i += 3)
				{
					oPacket.WriteShort(64);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1153; i <= 1180; i += 3)
				{
					oPacket.WriteShort(65);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1053; i <= 1080; i += 3)
				{
					oPacket.WriteShort(66);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void V1LegendPartsData()
		{
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(40);
				byte Grade = 2;
				//-----------------------------------------------------------------V1 레전드 파츠
				for (short i = 1105; i <= 1150; i += 5)
				{
					oPacket.WriteShort(63);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1005; i <= 1050; i += 5)
				{
					oPacket.WriteShort(64);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1105; i <= 1150; i += 5)
				{
					oPacket.WriteShort(65);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1005; i <= 1050; i += 5)
				{
					oPacket.WriteShort(66);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void V1RarePartsData()
		{
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(40);
				byte Grade = 3;
				//-----------------------------------------------------------------V1 레어 파츠
				for (short i = 1010; i <= 1100; i += 10)
				{
					oPacket.WriteShort(63);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 910; i <= 1000; i += 10)
				{
					oPacket.WriteShort(64);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 1010; i <= 1100; i += 10)
				{
					oPacket.WriteShort(65);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 910; i <= 1000; i += 10)
				{
					oPacket.WriteShort(66);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void V1NormalPartsData()
		{
			using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
			{
				oPacket.WriteInt(1);
				oPacket.WriteInt(1);
				oPacket.WriteInt(40);
				byte Grade = 4;
				//-----------------------------------------------------------------V1 일반 파츠
				for (short i = 910; i <= 1000; i += 10)
				{
					oPacket.WriteShort(63);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 810; i <= 900; i += 10)
				{
					oPacket.WriteShort(64);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 910; i <= 1000; i += 10)
				{
					oPacket.WriteShort(65);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				for (short i = 810; i <= 900; i += 10)
				{
					oPacket.WriteShort(66);
					oPacket.WriteShort(2);
					oPacket.WriteShort(0);
					oPacket.WriteShort(SetRider.SlotChanger);
					oPacket.WriteByte(0);
					oPacket.WriteByte(0);
					oPacket.WriteShort(-1);
					oPacket.WriteShort(-1);
					oPacket.WriteByte(1);
					oPacket.WriteByte(Grade);
					oPacket.WriteShort(i);
				}
				RouterListener.MySession.Client.Send(oPacket);
			}
		}

		public static void LoRpGetRiderItemPacket(short itemCat, List<List<short>> item)
		{
			int range = 100;//分批次数
			int times = item.Count / range + (item.Count % range > 0 ? 1 : 0);
			for (int i = 0; i < times; i++)
			{
				var tempList = item.GetRange(i * range, (i + 1) * range > item.Count ? (item.Count - i * range) : range);
				using (OutPacket oPacket = new OutPacket("LoRpGetRiderItemPacket"))
				{
					oPacket.WriteInt(1);
					oPacket.WriteInt(1);
					oPacket.WriteInt(tempList.Count);
					for (int f = 0; f < tempList.Count; f++)
					{
						oPacket.WriteShort(itemCat);
						oPacket.WriteShort(tempList[f][0]);
						oPacket.WriteShort(tempList[f][1]);
						oPacket.WriteShort(tempList[f][2]);
						oPacket.WriteByte((byte)((Program.PreventItem ? 1 : 0)));
						oPacket.WriteByte(0);
						oPacket.WriteShort(-1);
						oPacket.WriteShort(0);
						oPacket.WriteByte(0);
						oPacket.WriteByte(0);
						oPacket.WriteShort(0);
					}
					RouterListener.MySession.Client.Send(oPacket);
				}
			}
		}
	}
}
