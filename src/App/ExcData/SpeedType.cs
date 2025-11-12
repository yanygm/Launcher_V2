using Launcher.App.KartSpec;
using Launcher.App.Profile;
using Launcher.App.Utility;

namespace Launcher.App.ExcData
{
    public class SpeedType
    {
        public static Dictionary<string, byte> speedNames = new Dictionary<string, byte>
        {
            { "标准", 7 },
            { "慢速S0", 3 },
            { "普通S1", 0 },
            { "快速S2", 1 },
            { "高速S3", 2 },
            { "复古初级&S1", 10 },
            { "复古L3&S2", 11 },
            { "复古L2", 12 },
            { "复古L1&S3", 13 },
            { "复古Pro", 14 }
        };

        public static float AddSpec_TransAccelFactor = 0f;
        public static float AddSpec_SteerConstraint = 0f;
        public static float AddSpec_DriftEscapeForce = 0f;

        public static float DragFactor = 0f;
        public static float ForwardAccelForce = 0f;
        public static float BackwardAccelForce = 0f;
        public static float GripBrakeForce = 0f;
        public static float SlipBrakeForce = 0f;
        public static float SteerConstraint = 0f;
        public static float DriftEscapeForce = 0f;
        public static float CornerDrawFactor = 0f;
        public static float DriftMaxGauge = 0f;
        public static float TransAccelFactor = 0f;
        public static float BoostAccelFactor = 0f;
        public static float StartForwardAccelForceItem = 0f;
        public static float StartForwardAccelForceSpeed = 0f;

        public static void SpeedTypeData()
        {
            if (ProfileService.ProfileConfig.GameOption.SpeedType == 3)//S0 보통
            {
                AddSpec_SteerConstraint = -0.3f;
                AddSpec_DriftEscapeForce = -350f;
                AddSpec_TransAccelFactor = -0.015f;
                DragFactor = -0.05f;
                ForwardAccelForce = -530f;
                BackwardAccelForce = -225f;
                GripBrakeForce = -570f;
                SlipBrakeForce = -215f;
                SteerConstraint = -2.25f;
                DriftEscapeForce = -750f;
                CornerDrawFactor = -0.05f;
                DriftMaxGauge = 750f;
                TransAccelFactor = -0.2155f;
                BoostAccelFactor = 0.006f;
                StartForwardAccelForceItem = -530f;
                StartForwardAccelForceSpeed = -950f;
                Console.WriteLine("SpeedType:S0");
            }
            else if (ProfileService.ProfileConfig.GameOption.SpeedType == 0)//S1 빠름
            {
                AddSpec_SteerConstraint = 1.7f;
                AddSpec_DriftEscapeForce = 150f;
                AddSpec_TransAccelFactor = 0.199f;
                DragFactor = -0.015f;
                ForwardAccelForce = -200f;
                BackwardAccelForce = -225f;
                GripBrakeForce = -270f;
                SlipBrakeForce = -165f;
                SteerConstraint = -0.25f;
                DriftEscapeForce = -250f;
                CornerDrawFactor = -0.03f;
                DriftMaxGauge = -330f;
                TransAccelFactor = -0.0015f;
                BoostAccelFactor = 0.006f;
                StartForwardAccelForceItem = -200f;
                StartForwardAccelForceSpeed = -360f;
                Console.WriteLine("SpeedType:S1");
            }
            else if (ProfileService.ProfileConfig.GameOption.SpeedType == 1)//S2 매우빠름
            {
                AddSpec_SteerConstraint = 2.2f;
                AddSpec_DriftEscapeForce = 1100f;
                AddSpec_TransAccelFactor = 0.202f;
                DragFactor = 0.0121f;
                ForwardAccelForce = 200f;
                BackwardAccelForce = 225f;
                GripBrakeForce = 270f;
                SlipBrakeForce = 165f;
                SteerConstraint = 0.25f;
                DriftEscapeForce = 700f;
                CornerDrawFactor = 0f;
                DriftMaxGauge = 580f;
                TransAccelFactor = 0.0015f;
                BoostAccelFactor = 0.006f;
                StartForwardAccelForceItem = 200f;
                StartForwardAccelForceSpeed = 360f;
                Console.WriteLine("SpeedType:S2");
            }
            else if (ProfileService.ProfileConfig.GameOption.SpeedType == 2)//S3 가장빠름
            {
                AddSpec_SteerConstraint = 2.7f;
                AddSpec_DriftEscapeForce = 1500f;
                AddSpec_TransAccelFactor = 0.2f;
                DragFactor = 0.04f;
                ForwardAccelForce = 750f;
                BackwardAccelForce = 450f;
                GripBrakeForce = 540f;
                SlipBrakeForce = 325f;
                SteerConstraint = 0.75f;
                DriftEscapeForce = 1100f;
                CornerDrawFactor = -0.02f;
                DriftMaxGauge = 1700f;
                TransAccelFactor = -0.0005f;
                BoostAccelFactor = 0.006f;
                StartForwardAccelForceItem = 750f;
                StartForwardAccelForceSpeed = 1350f;
                Console.WriteLine("SpeedType:S3");
            }
            else if (ProfileService.ProfileConfig.GameOption.SpeedType == 4 || ProfileService.ProfileConfig.GameOption.SpeedType == 6 || ProfileService.ProfileConfig.GameOption.SpeedType == 7)//무부, 통합
            {
                AddSpec_SteerConstraint = 1.95f;
                AddSpec_DriftEscapeForce = 400f;
                AddSpec_TransAccelFactor = 0.2005f;
                DragFactor = 0f;
                ForwardAccelForce = 0f;
                BackwardAccelForce = 0f;
                GripBrakeForce = 0f;
                SlipBrakeForce = 0f;
                SteerConstraint = 0f;
                DriftEscapeForce = 0f;
                CornerDrawFactor = 0f;
                DriftMaxGauge = 0f;
                TransAccelFactor = 0f;
                BoostAccelFactor = 0f;
                StartForwardAccelForceItem = 0f;
                StartForwardAccelForceSpeed = 0f;
                Console.WriteLine("SpeedType:Integration");
            }
            else if (ProfileService.ProfileConfig.GameOption.SpeedType == 10)//Rookie, S1
            {
                AddSpec_SteerConstraint = 0f;
                AddSpec_DriftEscapeForce = 0f;
                AddSpec_TransAccelFactor = 0f;
                DragFactor = -0.01f;
                ForwardAccelForce = -150f;
                BackwardAccelForce = -225f;
                GripBrakeForce = -270f;
                SlipBrakeForce = -215f;
                SteerConstraint = -0.25f;
                DriftEscapeForce = -100f;
                CornerDrawFactor = 0.02f;
                DriftMaxGauge = -300f;
                TransAccelFactor = 0.0045f;
                BoostAccelFactor = 0.006f;
                StartForwardAccelForceItem = -270f;
                StartForwardAccelForceSpeed = -270f;
            }
            else if (ProfileService.ProfileConfig.GameOption.SpeedType == 11)//L3, S2
            {
                AddSpec_SteerConstraint = 0f;
                AddSpec_DriftEscapeForce = 0f;
                AddSpec_TransAccelFactor = 0f;
                DragFactor = 0.013f;
                ForwardAccelForce = 250f;
                BackwardAccelForce = 225f;
                GripBrakeForce = 270f;
                SlipBrakeForce = 145f;
                SteerConstraint = 0.55f;
                DriftEscapeForce = 700f;
                CornerDrawFactor = 0.02f;
                DriftMaxGauge = 700f;
                TransAccelFactor = 0.0045f;
                BoostAccelFactor = 0.006f;
                StartForwardAccelForceItem = 450f;
                StartForwardAccelForceSpeed = 450f;
            }
            else if (ProfileService.ProfileConfig.GameOption.SpeedType == 12)//L2
            {
                AddSpec_SteerConstraint = 0f;
                AddSpec_DriftEscapeForce = 0f;
                AddSpec_TransAccelFactor = 0f;
                DragFactor = -0.007f;
                ForwardAccelForce = 350f;
                BackwardAccelForce = 375f;
                GripBrakeForce = 330f;
                SlipBrakeForce = 195f;
                SteerConstraint = 0.57f;
                DriftEscapeForce = 800f;
                CornerDrawFactor = 0.02f;
                DriftMaxGauge = 800f;
                TransAccelFactor = 0.0045f;
                BoostAccelFactor = 0.006f;
                StartForwardAccelForceItem = 400f;
                StartForwardAccelForceSpeed = 400f;
            }
            else if (ProfileService.ProfileConfig.GameOption.SpeedType == 13)//L1, S3
            {
                AddSpec_SteerConstraint = 0f;
                AddSpec_DriftEscapeForce = 0f;
                AddSpec_TransAccelFactor = 0f;
                DragFactor = 0.051f;
                ForwardAccelForce = 750f;
                BackwardAccelForce = 450f;
                GripBrakeForce = 540f;
                SlipBrakeForce = 325f;
                SteerConstraint = 0.75f;
                DriftEscapeForce = 1100f;
                CornerDrawFactor = 0.02f;
                DriftMaxGauge = 1700f;
                TransAccelFactor = 0.0045f;
                BoostAccelFactor = 0.006f;
                StartForwardAccelForceItem = 1350f;
                StartForwardAccelForceSpeed = 1350f;
            }
            else if (ProfileService.ProfileConfig.GameOption.SpeedType == 14)//Pro
            {
                AddSpec_SteerConstraint = 0f;
                AddSpec_DriftEscapeForce = 0f;
                AddSpec_TransAccelFactor = 0f;
                DragFactor = 0.06f;
                ForwardAccelForce = 1650f;
                BackwardAccelForce = 1125f;
                GripBrakeForce = 1350f;
                SlipBrakeForce = 865f;
                SteerConstraint = 1.15f;
                DriftEscapeForce = 2100f;
                CornerDrawFactor = 0.02f;
                DriftMaxGauge = 3700f;
                TransAccelFactor = 0.0045f;
                BoostAccelFactor = 0.006f;
                StartForwardAccelForceItem = 1800f;
                StartForwardAccelForceSpeed = 1800f;
            }
            else
            {
                GameSupport.OnDisconnect();
                Console.WriteLine("SpeedType:null");
            }
            FlyingPet.FlyingPet_Spec();
        }
    }
}
