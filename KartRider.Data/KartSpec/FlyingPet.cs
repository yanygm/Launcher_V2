using System;
using System.Xml;
using ExcData;
using KartRider;
using RiderData;

namespace KartRider
{
	public class FlyingPet
	{
		public static float DragFactor;
		public static float ForwardAccelForce;
		public static float DriftEscapeForce;
		public static float CornerDrawFactor;
		public static float NormalBoosterTime;
		public static float ItemBoosterTime;
		public static float TeamBoosterTime;
		public static float StartForwardAccelForceItem;
		public static float StartForwardAccelForceSpeed;

		public static void FlyingPet_Spec()
		{
			if (StartGameData.FlyingPet_id == 0)
			{
				FlyingPet_Spec_Init();
			}
			else
			{
				if (KartExcData.flyingName.ContainsKey(StartGameData.FlyingPet_id))
				{
					string Name = KartExcData.flyingName[StartGameData.FlyingPet_id];
					Console.WriteLine($"flying:{StartGameData.FlyingPet_id},Name:{Name}");
					if (KartExcData.flyingSpec.ContainsKey(Name))
					{
						XmlDocument Spec = KartExcData.flyingSpec[Name];
						foreach (XmlNode petParamNode in Spec)
						{
							float value;
							if (petParamNode.Attributes != null && petParamNode.Attributes["DragFactor"] != null && float.TryParse(petParamNode.Attributes["DragFactor"].Value, out value))
							{
								FlyingPet.DragFactor = value;
							}
							else
							{
								FlyingPet.DragFactor = 0f;
							}

							if (petParamNode.Attributes != null && petParamNode.Attributes["ForwardAccelForce"] != null && float.TryParse(petParamNode.Attributes["ForwardAccelForce"].Value, out value))
							{
								FlyingPet.ForwardAccelForce = value;
							}
							else
							{
								FlyingPet.ForwardAccelForce = 0f;
							}

							if (petParamNode.Attributes != null && petParamNode.Attributes["DriftEscapeForce"] != null && float.TryParse(petParamNode.Attributes["DriftEscapeForce"].Value, out value))
							{
								FlyingPet.DriftEscapeForce = value;
							}
							else
							{
								FlyingPet.DriftEscapeForce = 0f;
							}

							if (petParamNode.Attributes != null && petParamNode.Attributes["CornerDrawFactor"] != null && float.TryParse(petParamNode.Attributes["CornerDrawFactor"].Value, out value))
							{
								FlyingPet.CornerDrawFactor = value;
							}
							else
							{
								FlyingPet.CornerDrawFactor = 0f;
							}

							if (petParamNode.Attributes != null && petParamNode.Attributes["NormalBoosterTime"] != null && float.TryParse(petParamNode.Attributes["NormalBoosterTime"].Value, out value))
							{
								FlyingPet.NormalBoosterTime = value;
							}
							else
							{
								FlyingPet.NormalBoosterTime = 0f;
							}

							if (petParamNode.Attributes != null && petParamNode.Attributes["ItemBoosterTime"] != null && float.TryParse(petParamNode.Attributes["ItemBoosterTime"].Value, out value))
							{
								FlyingPet.ItemBoosterTime = value;
							}
							else
							{
								FlyingPet.ItemBoosterTime = 0f;
							}

							if (petParamNode.Attributes != null && petParamNode.Attributes["TeamBoosterTime"] != null && float.TryParse(petParamNode.Attributes["TeamBoosterTime"].Value, out value))
							{
								FlyingPet.TeamBoosterTime = value;
							}
							else
							{
								FlyingPet.TeamBoosterTime = 0f;
							}

							if (petParamNode.Attributes != null && petParamNode.Attributes["StartForwardAccelItem"] != null && float.TryParse(petParamNode.Attributes["StartForwardAccelItem"].Value, out value))
							{
								FlyingPet.StartForwardAccelForceItem = value;
							}
							else
							{
								FlyingPet.StartForwardAccelForceItem = 0f;
							}

							if (petParamNode.Attributes != null && petParamNode.Attributes["StartForwardAccelSpeed"] != null && float.TryParse(petParamNode.Attributes["StartForwardAccelSpeed"].Value, out value))
							{
								FlyingPet.StartForwardAccelForceSpeed = value;
							}
							else
							{
								FlyingPet.StartForwardAccelForceSpeed = 0f;
							}
							break;
						}
					}
					else
					{
						FlyingPet_Spec_Init();
					}
				}
				else
				{
					FlyingPet_Spec_Init();
				}
			}
			Console.WriteLine($"-------------------------------------------------------------");
			Console.WriteLine($"FlyingPet DragFactor:{FlyingPet.DragFactor}");
			Console.WriteLine($"FlyingPet ForwardAccelForce:{FlyingPet.ForwardAccelForce}");
			Console.WriteLine($"FlyingPet DriftEscapeForce:{FlyingPet.DriftEscapeForce}");
			Console.WriteLine($"FlyingPet CornerDrawFactor:{FlyingPet.CornerDrawFactor}");
			Console.WriteLine($"FlyingPet NormalBoosterTime:{FlyingPet.NormalBoosterTime}");
			Console.WriteLine($"FlyingPet ItemBoosterTime:{FlyingPet.ItemBoosterTime}");
			Console.WriteLine($"FlyingPet TeamBoosterTime:{FlyingPet.TeamBoosterTime}");
			Console.WriteLine($"FlyingPet StartForwardAccelForceItem:{FlyingPet.StartForwardAccelForceItem}");
			Console.WriteLine($"FlyingPet StartForwardAccelForceSpeed:{FlyingPet.StartForwardAccelForceSpeed}");
			Console.WriteLine($"-------------------------------------------------------------");
			KartSpec.GetKartSpec();
		}

		public static void FlyingPet_Spec_Init()
		{
			FlyingPet.DragFactor = 0f;
			FlyingPet.ForwardAccelForce = 0f;
			FlyingPet.DriftEscapeForce = 0f;
			FlyingPet.CornerDrawFactor = 0f;
			FlyingPet.NormalBoosterTime = 0f;
			FlyingPet.ItemBoosterTime = 0f;
			FlyingPet.TeamBoosterTime = 0f;
			FlyingPet.StartForwardAccelForceItem = 0f;
			FlyingPet.StartForwardAccelForceSpeed = 0f;
		}
	}
}