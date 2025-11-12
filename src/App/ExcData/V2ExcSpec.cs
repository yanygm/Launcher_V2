using Launcher.App.KartSpec;
using Launcher.App.Profile;
using Launcher.App.Utility;

namespace Launcher.App.ExcData
{
    public class V2Spec
    {
        public static float V2Parts_TransAccelFactor = 0f;
        public static float V2Default_TransAccelFactor = 0f;
        public static float V2Parts_SteerConstraint = 0f;
        public static float V2Default_SteerConstraint = 0f;
        public static float V2Parts_DriftEscapeForce = 0f;
        public static float V2Default_DriftEscapeForce = 0f;
        public static float V2Parts_NormalBoosterTime = 0f;
        public static float V2Default_NormalBoosterTime = 0f;

        public static float V2Level_ForwardAccelForce = 0f;
        public static float V2Level_CornerDrawFactor = 0f;
        public static float V2Level_DragFactor = 0f;
        public static float V2Level_NormalBoosterTime = 0f;
        public static float V2Level_TeamBoosterTime = 0f;
        public static float V2Level_StartBoosterTimeSpeed = 0f;
        public static float V2Level_TransAccelFactor = 0f;
        public static float V2Level_DriftEscapeForce = 0f;
        public static float V2Level_DriftMaxGauge = 0f;

        /// <summary>
        /// 迅道具技能
        /// </summary>
		public static Dictionary<short, Dictionary<short, short>> itemSkill = new Dictionary<short, Dictionary<short, short>>
        {
			//{ 13, new Dictionary<short, short> { {3, 10} } },
			//{ 14, new Dictionary<short, short> { {5, 6} } },
			{ 15, new Dictionary<short, short> { {10, 18} } },
            { 16, new Dictionary<short, short> { {6, 18} } },
            { 18, new Dictionary<short, short> { {9, 27} } },
            { 19, new Dictionary<short, short> { {7, 32} } },
            { 20, new Dictionary<short, short> { {6, 24} } },
            { 21, new Dictionary<short, short> { {8, 37} } }
        };

        public static List<short> GetSkills()
        {
            int[] skillID = { 4, 6, 8 };
            List<short> skills = new List<short>();
            var existingLevel = KartExcData.Level12List.FirstOrDefault(list => list[0] == ProfileService.ProfileConfig.RiderItem.Set_Kart && list[1] == ProfileService.ProfileConfig.RiderItem.Set_KartSN);
            if (existingLevel != null)
            {
                for (int i = 0; i < skillID.Length; i++)
                {
                    if (existingLevel[skillID[i]] != 0)
                    {
                        skills.Add(existingLevel[skillID[i] - 1]);
                    }
                }
                return skills;
            }
            return new List<short>();
        }

        public static byte GetGrade(byte level)
        {
            if (level > 30) return 1;
            if (level > 20) return 2;
            if (level > 10) return 3;
            if (level > 1) return 4;
            return 0;
        }

        public static short Get12Parts(short input)
        {
            int fullCycles = (input - 1) / 10;
            int positionInCycle = (input - 1) % 10;
            int current = 201 + (fullCycles * 23); // Each full cycle adds net +23 (30 -7)
            for (int i = 1; i <= positionInCycle; i++)
            {
                if (i <= 2)
                    current += 2;
                else if (i <= 5)
                    current += 3;
                else if (i <= 8)
                    current += 4;
                else if (i == 9)
                    current += 5;
            }
            return (short)current;
        }

        public static void Reset_V2Level_SpecData()
        {
            V2Level_ForwardAccelForce = 0f;
            V2Level_CornerDrawFactor = 0f;
            V2Level_DragFactor = 0f;
            V2Level_NormalBoosterTime = 0f;
            V2Level_TeamBoosterTime = 0f;
            V2Level_StartBoosterTimeSpeed = 0f;
            V2Level_TransAccelFactor = 0f;
            V2Level_DriftEscapeForce = 0f;
            V2Level_DriftMaxGauge = 0f;
        }

        public static void Reset_V2Parts_SpecData()
        {
            V2Parts_TransAccelFactor = 0f;
            V2Parts_SteerConstraint = 0f;
            V2Parts_DriftEscapeForce = 0f;
            V2Parts_NormalBoosterTime = 0f;
            V2Default_TransAccelFactor = 0f;
            V2Default_SteerConstraint = 0f;
            V2Default_DriftEscapeForce = 0f;
            V2Default_NormalBoosterTime = 0f;
        }

        public void ExceedSpec()
        {
            if (Kart.defaultExceedType > 0)
            {
                var KartAndSN = new { Kart = ProfileService.ProfileConfig.RiderItem.Set_Kart, SN = ProfileService.ProfileConfig.RiderItem.Set_KartSN };
                var existingParts = KartExcData.Parts12List.FirstOrDefault(list => list[0] == KartAndSN.Kart && list[1] == KartAndSN.SN);

                // Handle exceed type
                if (existingParts != null)
                {
                    if (existingParts[17] != 0)
                    {
                        Kart.defaultExceedType = existingParts[17];
                    }
                    else
                    {
                        existingParts[17] = (short)Kart.defaultExceedType;
                    }
                }

                Console.WriteLine("-------------------------------------------------------------");

                // Helper function to process parts
                short ProcessPart(Func<short> getDefaultValue, int typeIndex, int valueIndex, ref byte kartType, byte defaultKartType)
                {
                    short value;
                    if (existingParts == null || existingParts[valueIndex] < 1)
                    {
                        value = getDefaultValue();
                        if (existingParts != null)
                        {
                            existingParts[typeIndex] = defaultKartType;
                        }
                    }
                    else
                    {
                        kartType = (byte)existingParts[typeIndex - 1];
                        existingParts[typeIndex] = defaultKartType;
                        value = existingParts[valueIndex];
                    }
                    return value;
                }

                // Process each part
                var Parts_TransAccelFactor = ProcessPart(() => Get12Parts(Kart.defaultEngineType), 3, 4, ref Kart.EngineType, Kart.defaultEngineType);
                V2Parts_TransAccelFactor = (float)((((Parts_TransAccelFactor * 1.0M) - 800M) / 25000.0M) + 0.4765M);
                V2Default_TransAccelFactor = (float)((((Get12Parts(Kart.defaultEngineType) * 1.0M) - 800M) / 25000.0M) + 0.4765M);
                Console.WriteLine($"V2Parts_TransAccelFactor: {V2Parts_TransAccelFactor}");

                var Parts_SteerConstraint = ProcessPart(() => Get12Parts(Kart.defaultHandleType), 6, 7, ref Kart.HandleType, Kart.defaultHandleType);
                V2Parts_SteerConstraint = (float)((((Parts_SteerConstraint * 1.0M) - 800M) / 250.0M) + 2.7M);
                V2Default_SteerConstraint = (float)((((Get12Parts(Kart.defaultHandleType) * 1.0M) - 800M) / 250.0M) + 2.7M);
                Console.WriteLine($"V2Parts_SteerConstraint: {V2Parts_SteerConstraint}");

                var Parts_DriftEscapeForce = ProcessPart(() => Get12Parts(Kart.defaultWheelType), 9, 10, ref Kart.WheelType, Kart.defaultWheelType);
                V2Parts_DriftEscapeForce = (float)(Parts_DriftEscapeForce * 2.0M);
                V2Default_DriftEscapeForce = (float)(Get12Parts(Kart.defaultWheelType) * 2.0M);
                Console.WriteLine($"V2Parts_DriftEscapeForce: {V2Parts_DriftEscapeForce}");

                var Parts_NormalBoosterTime = ProcessPart(() => Get12Parts(Kart.defaultBoosterType), 12, 13, ref Kart.BoosterType, Kart.defaultBoosterType);
                V2Parts_NormalBoosterTime = (float)((Parts_NormalBoosterTime * 1.0M) - 260M);
                V2Default_NormalBoosterTime = (float)((Get12Parts(Kart.defaultBoosterType) * 1.0M) - 260M);
                Console.WriteLine($"V2Parts_NormalBoosterTime: {V2Parts_NormalBoosterTime}");

                Console.WriteLine("-------------------------------------------------------------");
                KartExcData.SaveParts12List(KartExcData.Parts12List);

                Reset_V2Level_SpecData();
                var existingLevel = KartExcData.Level12List.FirstOrDefault(list => list[0] == KartAndSN.Kart && list[1] == KartAndSN.SN);
                if (existingLevel != null)
                {
                    // Create skill pairs (ID, Level) from non-zero entries
                    var skills = Enumerable.Range(0, 3)
                        .Select(i => new { Index = 3 + (i * 2), ID = existingLevel[3 + (i * 2)], Level = existingLevel[4 + (i * 2)] })
                        .Where(s => s.ID != 0)
                        .Select(s => new { s.ID, s.Level })
                        .ToList();

                    if (skills.Count > 0)
                    {
                        Console.WriteLine("-------------------------------------------------------------");

                        // Define all skill data in a dictionary
                        var skillData = new Dictionary<int, (string Name, float[] Values)>
                        {
                            { 1, ("ForwardAccelForce", new[] { 0f, 1.5f, 1.7f, 2f, 2.5f, 3.5f }) },
                            { 2, ("CornerDrawFactor", new[] { 0f, 0.0007f, 0.0008f, 0.001f, 0.0012f, 0.002f }) },
                            { 3, ("DragFactor", new[] { 0f, -0.0008f, -0.001f, -0.0013f, -0.0017f, -0.00225f }) },
                            { 4, ("NormalBoosterTime", new[] { 0f, 50f, 70f, 90f, 120f, 190f }) },
                            { 5, ("TeamBoosterTime", new[] { 0f, 100f, 110f, 130f, 150f, 250f }) },
                            { 6, ("StartBoosterTimeSpeed", new[] { 0f, 150f, 200f, 300f, 450f, 800f }) },
                            { 7, ("TransAccelFactor", new[] { 0f, 0.003f, 0.004f, 0.005f, 0.007f, 0.02f }) },
                            { 8, ("DriftEscapeForce", new[] { 0f, 35f, 50f, 65f, 90f, 210f }) },
                            { 9, ("DriftMaxGauge", new[] { 0f, -50f, -60f, -70f, -90f, -200f }) }
                        };

                        // Process each skill
                        foreach (var skill in skills)
                        {
                            if (skillData.TryGetValue(skill.ID, out var data))
                            {
                                // Set the value using reflection (alternative would be a switch statement)
                                typeof(V2Spec).GetField($"V2Level_{data.Name}")?
                                    .SetValue(this, data.Values[skill.Level]);

                                Console.WriteLine($"V2Level_{data.Name}: {data.Values[skill.Level]}");
                            }
                        }

                        Console.WriteLine("-------------------------------------------------------------");
                    }
                }
                if (Kart.defaultExceedType == 1)//item S
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.016f;
                    Kart.chargeInstAccelGaugeByGrip = 0.06f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.3f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 1000f;
                    Kart.instAccelGaugeMinUsable = 300f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 1;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 160f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeAntiCollideBalance = 0f;
                    Kart.chargeInstAccelGaugeByWallAdded = 0f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0f;
                    Kart.chargerSystemboosterUseCount = 0;
                    Kart.chargerSystemUseTime = 0;
                    Kart.chargeBoostBySpeedAdded = 0f;
                    Kart.driftGaugeFactor = 0f;

                    Kart.startItemId = GameSupport.RandomItemSkill(2);
                }
                if (Kart.defaultExceedType == 2)//S
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.02f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.29f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 1040f;
                    Kart.instAccelGaugeMinUsable = 208f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 0;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 200f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeAntiCollideBalance = 0.8f;
                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 4;
                    Kart.chargerSystemUseTime = 3000;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                }
                if (Kart.defaultExceedType == 3)//B
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.02f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.18f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 2000f;
                    Kart.instAccelGaugeMinUsable = 400f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 0;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 200f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeAntiCollideBalance = 0.8f;
                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 5;
                    Kart.chargerSystemUseTime = 3750;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                }
                if (Kart.defaultExceedType == 4)//L
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.02f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.14f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 2500f;
                    Kart.instAccelGaugeMinUsable = 500f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 0;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 200f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeAntiCollideBalance = 0.8f;
                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 6;
                    Kart.chargerSystemUseTime = 4500;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                }
                if (Kart.defaultExceedType == 5)//?
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.016f;
                    Kart.chargeInstAccelGaugeByGrip = 0.06f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.3f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 1000f;
                    Kart.instAccelGaugeMinUsable = 300f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 0;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 200;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeAntiCollideBalance = 0f;
                    Kart.chargeInstAccelGaugeByWallAdded = 0f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0f;
                    Kart.chargerSystemboosterUseCount = 0;
                    Kart.chargerSystemUseTime = 0;
                    Kart.chargeBoostBySpeedAdded = 0f;
                    Kart.driftGaugeFactor = 0f;
                }
                if (Kart.defaultExceedType == 6)//L时间
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.017f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.14f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 3000f;
                    Kart.instAccelGaugeMinUsable = 600f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 0;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 200f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeAntiCollideBalance = 0.8f;
                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.02f;
                    Kart.chargerSystemboosterUseCount = 6;
                    Kart.chargerSystemUseTime = 4500;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                }
                if (Kart.defaultExceedType == 7)//L赋能
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.02f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.14f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 2500f;
                    Kart.instAccelGaugeMinUsable = 500f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 0;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 200f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeAntiCollideBalance = 0.8f;
                    Kart.chargeInstAccelGaugeByWallAdded = 0.1f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.04f;
                    Kart.chargerSystemboosterUseCount = 5;
                    Kart.chargerSystemUseTime = 4750;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                }
            }
            else
            {
                Reset_V2Parts_SpecData();
                Reset_V2Level_SpecData();
            }
        }
    }
}
