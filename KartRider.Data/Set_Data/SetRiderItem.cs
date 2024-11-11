using System;
using System.IO;

namespace Set_Data
{
	public static class SetRiderItem
	{
		public static short Set_Character = 3;
		public static short Set_Paint = 1;
		public static short Set_Kart = 0;
		public static short Set_Plate = 0;
		public static short Set_Goggle = 0;
		public static short Set_Balloon = 0;
		public static short Set_HeadBand = 0;
		public static short Set_HandGearL = 0;
		public static short Set_Uniform = 0;
		public static short Set_Decal = 0;
		public static short Set_Pet = 0;
		public static short Set_FlyingPet = 0;
		public static short Set_Aura = 0;
		public static short Set_SkidMark = 0;
		public static short Set_RidColor = 0;
		public static short Set_BonusCard = 0;
		public static short Set_Tachometer = 0;
		public static short Set_Dye = 1;
		public static short Set_KartSN = 0;
		public static short Set_slotBg = 0;

		public static void Save_SetRiderItem()
		{
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Character + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Character);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Paint + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Paint);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Kart + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Kart);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Plate + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Plate);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Goggle + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Goggle);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Balloon + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Balloon);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_HeadBand + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_HeadBand);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_HandGearL + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_HandGearL);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Uniform + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Uniform);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Decal + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Decal);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Pet + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Pet);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_FlyingPet + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_FlyingPet);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Aura + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Aura);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_SkidMark + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_SkidMark);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_RidColor + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_RidColor);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_BonusCard + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_BonusCard);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Tachometer + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Tachometer);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Dye + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_Dye);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_KartSN + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_KartSN);
			}
			using (StreamWriter streamWriter = new StreamWriter(FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_slotBg + FileName.Extension, false))
			{
				streamWriter.Write(SetRiderItem.Set_slotBg);
			}
			Console.WriteLine("SetRiderItem-------------------------------------------------");
			Console.WriteLine("Character: {0}", SetRiderItem.Set_Character);
			Console.WriteLine("Paint: {0}", SetRiderItem.Set_Paint);
			Console.WriteLine("Kart: {0}", SetRiderItem.Set_Kart);
			Console.WriteLine("Plate: {0}", SetRiderItem.Set_Plate);
			Console.WriteLine("Goggle: {0}", SetRiderItem.Set_Goggle);
			Console.WriteLine("Balloon: {0}", SetRiderItem.Set_Balloon);
			Console.WriteLine("HeadBand: {0}", SetRiderItem.Set_HeadBand);
			Console.WriteLine("HandGearL: {0}", SetRiderItem.Set_HandGearL);
			Console.WriteLine("Uniform: {0}", SetRiderItem.Set_Uniform);
			Console.WriteLine("Decal: {0}", SetRiderItem.Set_Decal);
			Console.WriteLine("Pet: {0}", SetRiderItem.Set_Pet);
			Console.WriteLine("FlyingPet: {0}", SetRiderItem.Set_FlyingPet);
			Console.WriteLine("Aura: {0}", SetRiderItem.Set_Aura);
			Console.WriteLine("SkidMark: {0}", SetRiderItem.Set_SkidMark);
			Console.WriteLine("RidColor: {0}", SetRiderItem.Set_RidColor);
			Console.WriteLine("BonusCard: {0}", SetRiderItem.Set_BonusCard);
			Console.WriteLine("Tachometer: {0}", SetRiderItem.Set_Tachometer);
			Console.WriteLine("Dye: {0}", SetRiderItem.Set_Dye);
			Console.WriteLine("KartSN: {0}", SetRiderItem.Set_KartSN);
			Console.WriteLine("slotBg: {0}", SetRiderItem.Set_slotBg);
		}

		public static void Load_SetRiderItem()
		{
			string Load_Character = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Character + FileName.Extension;
			if (File.Exists(Load_Character))
			{
				string textValue = System.IO.File.ReadAllText(Load_Character);
				SetRiderItem.Set_Character = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Character, false))
				{
					streamWriter.Write(SetRiderItem.Set_Character);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Paint = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Paint + FileName.Extension;
			if (File.Exists(Load_Paint))
			{
				string textValue = System.IO.File.ReadAllText(Load_Paint);
				SetRiderItem.Set_Paint = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Paint, false))
				{
					streamWriter.Write(SetRiderItem.Set_Paint);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Kart = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Kart + FileName.Extension;
			if (File.Exists(Load_Kart))
			{
				string textValue = System.IO.File.ReadAllText(Load_Kart);
				SetRiderItem.Set_Kart = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Kart, false))
				{
					streamWriter.Write(SetRiderItem.Set_Kart);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Plate = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Plate + FileName.Extension;
			if (File.Exists(Load_Plate))
			{
				string textValue = System.IO.File.ReadAllText(Load_Plate);
				SetRiderItem.Set_Plate = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Plate, false))
				{
					streamWriter.Write(SetRiderItem.Set_Plate);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Goggle = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Goggle + FileName.Extension;
			if (File.Exists(Load_Goggle))
			{
				string textValue = System.IO.File.ReadAllText(Load_Goggle);
				SetRiderItem.Set_Goggle = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Goggle, false))
				{
					streamWriter.Write(SetRiderItem.Set_Goggle);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Balloon = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Balloon + FileName.Extension;
			if (File.Exists(Load_Balloon))
			{
				string textValue = System.IO.File.ReadAllText(Load_Balloon);
				SetRiderItem.Set_Balloon = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Balloon, false))
				{
					streamWriter.Write(SetRiderItem.Set_Balloon);
				}
			}
			//-------------------------------------------------------------------------
			string Load_HeadBand = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_HeadBand + FileName.Extension;
			if (File.Exists(Load_HeadBand))
			{
				string textValue = System.IO.File.ReadAllText(Load_HeadBand);
				SetRiderItem.Set_HeadBand = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_HeadBand, false))
				{
					streamWriter.Write(SetRiderItem.Set_HeadBand);
				}
			}
			//-------------------------------------------------------------------------
			string Load_HandGearL = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_HandGearL + FileName.Extension;
			if (File.Exists(Load_HandGearL))
			{
				string textValue = System.IO.File.ReadAllText(Load_HandGearL);
				SetRiderItem.Set_HandGearL = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_HandGearL, false))
				{
					streamWriter.Write(SetRiderItem.Set_HandGearL);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Uniform = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Uniform + FileName.Extension;
			if (File.Exists(Load_Uniform))
			{
				string textValue = System.IO.File.ReadAllText(Load_Uniform);
				SetRiderItem.Set_Uniform = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Uniform, false))
				{
					streamWriter.Write(SetRiderItem.Set_Uniform);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Decal = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Decal + FileName.Extension;
			if (File.Exists(Load_Decal))
			{
				string textValue = System.IO.File.ReadAllText(Load_Decal);
				SetRiderItem.Set_Decal = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Decal, false))
				{
					streamWriter.Write(SetRiderItem.Set_Decal);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Pet = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Pet + FileName.Extension;
			if (File.Exists(Load_Pet))
			{
				string textValue = System.IO.File.ReadAllText(Load_Pet);
				SetRiderItem.Set_Pet = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Pet, false))
				{
					streamWriter.Write(SetRiderItem.Set_Pet);
				}
			}
			//-------------------------------------------------------------------------
			string Load_FlyingPet = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_FlyingPet + FileName.Extension;
			if (File.Exists(Load_FlyingPet))
			{
				string textValue = System.IO.File.ReadAllText(Load_FlyingPet);
				SetRiderItem.Set_FlyingPet = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_FlyingPet, false))
				{
					streamWriter.Write(SetRiderItem.Set_FlyingPet);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Aura = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Aura + FileName.Extension;
			if (File.Exists(Load_Aura))
			{
				string textValue = System.IO.File.ReadAllText(Load_Aura);
				SetRiderItem.Set_Aura = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Aura, false))
				{
					streamWriter.Write(SetRiderItem.Set_Aura);
				}
			}
			//-------------------------------------------------------------------------
			string Load_SkidMark = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_SkidMark + FileName.Extension;
			if (File.Exists(Load_SkidMark))
			{
				string textValue = System.IO.File.ReadAllText(Load_SkidMark);
				SetRiderItem.Set_SkidMark = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_SkidMark, false))
				{
					streamWriter.Write(SetRiderItem.Set_SkidMark);
				}
			}
			//-------------------------------------------------------------------------
			string Load_RidColor = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_RidColor + FileName.Extension;
			if (File.Exists(Load_RidColor))
			{
				string textValue = System.IO.File.ReadAllText(Load_RidColor);
				SetRiderItem.Set_RidColor = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_RidColor, false))
				{
					streamWriter.Write(SetRiderItem.Set_RidColor);
				}
			}
			//-------------------------------------------------------------------------
			string Load_BonusCard = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_BonusCard + FileName.Extension;
			if (File.Exists(Load_BonusCard))
			{
				string textValue = System.IO.File.ReadAllText(Load_BonusCard);
				SetRiderItem.Set_BonusCard = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_BonusCard, false))
				{
					streamWriter.Write(SetRiderItem.Set_BonusCard);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Tachometer = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Tachometer + FileName.Extension;
			if (File.Exists(Load_Tachometer))
			{
				string textValue = System.IO.File.ReadAllText(Load_Tachometer);
				SetRiderItem.Set_Tachometer = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Tachometer, false))
				{
					streamWriter.Write(SetRiderItem.Set_Tachometer);
				}
			}
			//-------------------------------------------------------------------------
			string Load_Dye = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_Dye + FileName.Extension;
			if (File.Exists(Load_Dye))
			{
				string textValue = System.IO.File.ReadAllText(Load_Dye);
				SetRiderItem.Set_Dye = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_Dye, false))
				{
					streamWriter.Write(SetRiderItem.Set_Dye);
				}
			}
			//-------------------------------------------------------------------------
			string Load_KartSN = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_KartSN + FileName.Extension;
			if (File.Exists(Load_KartSN))
			{
				string textValue = System.IO.File.ReadAllText(Load_KartSN);
				SetRiderItem.Set_KartSN = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_KartSN, false))
				{
					streamWriter.Write(SetRiderItem.Set_KartSN);
				}
			}
			//-------------------------------------------------------------------------
			string Load_slotBg = FileName.SetRiderItem_LoadFile + FileName.SetRiderItem_slotBg + FileName.Extension;
			if (File.Exists(Load_slotBg))
			{
				string textValue = System.IO.File.ReadAllText(Load_slotBg);
				SetRiderItem.Set_slotBg = short.Parse(textValue);
			}
			else
			{
				using (StreamWriter streamWriter = new StreamWriter(Load_slotBg, false))
				{
					streamWriter.Write(SetRiderItem.Set_slotBg);
				}
			}
		}
	}
}