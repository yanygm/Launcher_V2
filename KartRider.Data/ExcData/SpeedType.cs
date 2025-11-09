using System;
using KartRider;
using System.Collections.Generic;
using Profile;
using System.Net;

namespace ExcData
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

        public float AddSpec_TransAccelFactor { get; set; } = 0f;
        public float AddSpec_SteerConstraint { get; set; } = 0f;
        public float AddSpec_DriftEscapeForce { get; set; } = 0f;

        public float DragFactor { get; set; } = 0f;
        public float ForwardAccelForce { get; set; } = 0f;
        public float BackwardAccelForce { get; set; } = 0f;
        public float GripBrakeForce { get; set; } = 0f;
        public float SlipBrakeForce { get; set; } = 0f;
        public float SteerConstraint { get; set; } = 0f;
        public float DriftEscapeForce { get; set; } = 0f;
        public float CornerDrawFactor { get; set; } = 0f;
        public float DriftMaxGauge { get; set; } = 0f;
        public float TransAccelFactor { get; set; } = 0f;
        public float BoostAccelFactor { get; set; } = 0f;
        public float StartForwardAccelForceItem { get; set; } = 0f;
        public float StartForwardAccelForceSpeed { get; set; } = 0f;

        public void SpeedTypeData(byte SpeedType)
        {
            if (SpeedType == 3)//S0 보통
            {
                Console.WriteLine("SpeedType:S0");
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
            }
            else if (SpeedType == 0)//S1 빠름
            {
                Console.WriteLine("SpeedType:S1");
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
            }
            else if (SpeedType == 1)//S2 매우빠름
            {
                Console.WriteLine("SpeedType:S2");
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
            }
            else if (SpeedType == 2)//S3 가장빠름
            {
                Console.WriteLine("SpeedType:S3");
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
            }
            else if (SpeedType == 4 || SpeedType == 6 || SpeedType == 7)//무부, 통합
            {
                Console.WriteLine("SpeedType:Integration");
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
            }
            else if (SpeedType == 10)//Rookie, S1
            {
                Console.WriteLine("SpeedType:Rookie");
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
            else if (SpeedType == 11)//L3, S2
            {
                Console.WriteLine("SpeedType:L3");
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
            else if (SpeedType == 12)//L2
            {
                Console.WriteLine("SpeedType:L2");
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
            else if (SpeedType == 13)//L1, S3
            {
                Console.WriteLine("SpeedType:L1");
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
            else if (SpeedType == 14)//Pro
            {
                Console.WriteLine("SpeedType:Pro");
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
                Console.WriteLine("SpeedType:Integration");
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
            }
        }
    }
}
