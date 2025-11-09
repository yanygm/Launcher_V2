using ExcData;
using KartRider;
using Profile;
using RiderData;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;

namespace KartRider
{
    public class FlyingPet
    {
        public static Dictionary<int, string> flyingName = new Dictionary<int, string>();
        public static Dictionary<string, XmlDocument> flyingSpec = new Dictionary<string, XmlDocument>();
    }

    public class FlyingPetSpec
    {
        public float DragFactor { get; set; } = 0f;
        public float ForwardAccelForce { get; set; } = 0f;
        public float DriftEscapeForce { get; set; } = 0f;
        public float CornerDrawFactor { get; set; } = 0f;
        public float NormalBoosterTime { get; set; } = 0f;
        public float ItemBoosterTime { get; set; } = 0f;
        public float TeamBoosterTime { get; set; } = 0f;
        public float StartForwardAccelForceItem { get; set; } = 0f;
        public float StartForwardAccelForceSpeed { get; set; } = 0f;

        public void FlyingPet_Spec(string Nickname)
        {
            short FlyingPetID = ProfileService.ProfileConfigs[Nickname].RiderItem.Set_FlyingPet;
            if (FlyingPet.flyingName.ContainsKey(FlyingPetID))
            {
                string Name = FlyingPet.flyingName[FlyingPetID];
                Console.WriteLine($"flying:{FlyingPetID},Name:{Name}");
                if (FlyingPet.flyingSpec.ContainsKey(Name))
                {
                    XmlDocument Spec = FlyingPet.flyingSpec[Name];
                    foreach (XmlNode petParamNode in Spec)
                    {
                        float value;
                        if (petParamNode.Attributes != null && petParamNode.Attributes["DragFactor"] != null && float.TryParse(petParamNode.Attributes["DragFactor"].Value, out value))
                        {
                            this.DragFactor = value;
                        }
                        else
                        {
                            this.DragFactor = 0f;
                        }

                        if (petParamNode.Attributes != null && petParamNode.Attributes["ForwardAccelForce"] != null && float.TryParse(petParamNode.Attributes["ForwardAccelForce"].Value, out value))
                        {
                            this.ForwardAccelForce = value;
                        }
                        else
                        {
                            this.ForwardAccelForce = 0f;
                        }

                        if (petParamNode.Attributes != null && petParamNode.Attributes["DriftEscapeForce"] != null && float.TryParse(petParamNode.Attributes["DriftEscapeForce"].Value, out value))
                        {
                            this.DriftEscapeForce = value;
                        }
                        else
                        {
                            this.DriftEscapeForce = 0f;
                        }

                        if (petParamNode.Attributes != null && petParamNode.Attributes["CornerDrawFactor"] != null && float.TryParse(petParamNode.Attributes["CornerDrawFactor"].Value, out value))
                        {
                            this.CornerDrawFactor = value;
                        }
                        else
                        {
                            this.CornerDrawFactor = 0f;
                        }

                        if (petParamNode.Attributes != null && petParamNode.Attributes["NormalBoosterTime"] != null && float.TryParse(petParamNode.Attributes["NormalBoosterTime"].Value, out value))
                        {
                            this.NormalBoosterTime = value;
                        }
                        else
                        {
                            this.NormalBoosterTime = 0f;
                        }

                        if (petParamNode.Attributes != null && petParamNode.Attributes["ItemBoosterTime"] != null && float.TryParse(petParamNode.Attributes["ItemBoosterTime"].Value, out value))
                        {
                            this.ItemBoosterTime = value;
                        }
                        else
                        {
                            this.ItemBoosterTime = 0f;
                        }

                        if (petParamNode.Attributes != null && petParamNode.Attributes["TeamBoosterTime"] != null && float.TryParse(petParamNode.Attributes["TeamBoosterTime"].Value, out value))
                        {
                            this.TeamBoosterTime = value;
                        }
                        else
                        {
                            this.TeamBoosterTime = 0f;
                        }

                        if (petParamNode.Attributes != null && petParamNode.Attributes["StartForwardAccelItem"] != null && float.TryParse(petParamNode.Attributes["StartForwardAccelItem"].Value, out value))
                        {
                            this.StartForwardAccelForceItem = value;
                        }
                        else
                        {
                            this.StartForwardAccelForceItem = 0f;
                        }

                        if (petParamNode.Attributes != null && petParamNode.Attributes["StartForwardAccelSpeed"] != null && float.TryParse(petParamNode.Attributes["StartForwardAccelSpeed"].Value, out value))
                        {
                            this.StartForwardAccelForceSpeed = value;
                        }
                        else
                        {
                            this.StartForwardAccelForceSpeed = 0f;
                        }
                        break;
                    }
                }
            }
            Console.WriteLine($"-------------------------------------------------------------");
            Console.WriteLine($"FlyingPet DragFactor:{this.DragFactor}");
            Console.WriteLine($"FlyingPet ForwardAccelForce:{this.ForwardAccelForce}");
            Console.WriteLine($"FlyingPet DriftEscapeForce:{this.DriftEscapeForce}");
            Console.WriteLine($"FlyingPet CornerDrawFactor:{this.CornerDrawFactor}");
            Console.WriteLine($"FlyingPet NormalBoosterTime:{this.NormalBoosterTime}");
            Console.WriteLine($"FlyingPet ItemBoosterTime:{this.ItemBoosterTime}");
            Console.WriteLine($"FlyingPet TeamBoosterTime:{this.TeamBoosterTime}");
            Console.WriteLine($"FlyingPet StartForwardAccelForceItem:{this.StartForwardAccelForceItem}");
            Console.WriteLine($"FlyingPet StartForwardAccelForceSpeed:{this.StartForwardAccelForceSpeed}");
            Console.WriteLine($"-------------------------------------------------------------");
        }
    }
}
