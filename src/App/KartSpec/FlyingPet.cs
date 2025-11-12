using Launcher.App.ExcData;
using System.Xml;

namespace Launcher.App.KartSpec
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
                                DragFactor = value;
                            }
                            else
                            {
                                DragFactor = 0f;
                            }

                            if (petParamNode.Attributes != null && petParamNode.Attributes["ForwardAccelForce"] != null && float.TryParse(petParamNode.Attributes["ForwardAccelForce"].Value, out value))
                            {
                                ForwardAccelForce = value;
                            }
                            else
                            {
                                ForwardAccelForce = 0f;
                            }

                            if (petParamNode.Attributes != null && petParamNode.Attributes["DriftEscapeForce"] != null && float.TryParse(petParamNode.Attributes["DriftEscapeForce"].Value, out value))
                            {
                                DriftEscapeForce = value;
                            }
                            else
                            {
                                DriftEscapeForce = 0f;
                            }

                            if (petParamNode.Attributes != null && petParamNode.Attributes["CornerDrawFactor"] != null && float.TryParse(petParamNode.Attributes["CornerDrawFactor"].Value, out value))
                            {
                                CornerDrawFactor = value;
                            }
                            else
                            {
                                CornerDrawFactor = 0f;
                            }

                            if (petParamNode.Attributes != null && petParamNode.Attributes["NormalBoosterTime"] != null && float.TryParse(petParamNode.Attributes["NormalBoosterTime"].Value, out value))
                            {
                                NormalBoosterTime = value;
                            }
                            else
                            {
                                NormalBoosterTime = 0f;
                            }

                            if (petParamNode.Attributes != null && petParamNode.Attributes["ItemBoosterTime"] != null && float.TryParse(petParamNode.Attributes["ItemBoosterTime"].Value, out value))
                            {
                                ItemBoosterTime = value;
                            }
                            else
                            {
                                ItemBoosterTime = 0f;
                            }

                            if (petParamNode.Attributes != null && petParamNode.Attributes["TeamBoosterTime"] != null && float.TryParse(petParamNode.Attributes["TeamBoosterTime"].Value, out value))
                            {
                                TeamBoosterTime = value;
                            }
                            else
                            {
                                TeamBoosterTime = 0f;
                            }

                            if (petParamNode.Attributes != null && petParamNode.Attributes["StartForwardAccelItem"] != null && float.TryParse(petParamNode.Attributes["StartForwardAccelItem"].Value, out value))
                            {
                                StartForwardAccelForceItem = value;
                            }
                            else
                            {
                                StartForwardAccelForceItem = 0f;
                            }

                            if (petParamNode.Attributes != null && petParamNode.Attributes["StartForwardAccelSpeed"] != null && float.TryParse(petParamNode.Attributes["StartForwardAccelSpeed"].Value, out value))
                            {
                                StartForwardAccelForceSpeed = value;
                            }
                            else
                            {
                                StartForwardAccelForceSpeed = 0f;
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
            Console.WriteLine($"FlyingPet DragFactor:{DragFactor}");
            Console.WriteLine($"FlyingPet ForwardAccelForce:{ForwardAccelForce}");
            Console.WriteLine($"FlyingPet DriftEscapeForce:{DriftEscapeForce}");
            Console.WriteLine($"FlyingPet CornerDrawFactor:{CornerDrawFactor}");
            Console.WriteLine($"FlyingPet NormalBoosterTime:{NormalBoosterTime}");
            Console.WriteLine($"FlyingPet ItemBoosterTime:{ItemBoosterTime}");
            Console.WriteLine($"FlyingPet TeamBoosterTime:{TeamBoosterTime}");
            Console.WriteLine($"FlyingPet StartForwardAccelForceItem:{StartForwardAccelForceItem}");
            Console.WriteLine($"FlyingPet StartForwardAccelForceSpeed:{StartForwardAccelForceSpeed}");
            Console.WriteLine($"-------------------------------------------------------------");
            KartSpec.GetKartSpec();
        }

        public static void FlyingPet_Spec_Init()
        {
            DragFactor = 0f;
            ForwardAccelForce = 0f;
            DriftEscapeForce = 0f;
            CornerDrawFactor = 0f;
            NormalBoosterTime = 0f;
            ItemBoosterTime = 0f;
            TeamBoosterTime = 0f;
            StartForwardAccelForceItem = 0f;
            StartForwardAccelForceSpeed = 0f;
        }
    }
}