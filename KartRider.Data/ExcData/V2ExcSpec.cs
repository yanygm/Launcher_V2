using KartRider;
using Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KartRider.Logging;

namespace ExcData
{
    public class V2Specs
    {
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

        // 随机迅超越类型
        public static int[] ExceedTypes = { 2, 3, 4, 6, 7, 8, 9, 10 };

        public static List<short> GetSkills(string Nickname)
        {
            var Level12List = GetLevel12List(Nickname);
            List<short> skills = new List<short>();
            var config = ProfileService.GetProfileConfig(Nickname);
            if (config?.RiderItem == null)
            {
                Log.Info($"[Level12_ExcData] Warning: ProfileConfig or RiderItem is null for {Nickname}");
                return skills;
            }
            var existingLevel = Level12List.FirstOrDefault(Level12 => Level12.ID == config.RiderItem.Set_Kart && Level12.SN == config.RiderItem.Set_KartSN);
            if (existingLevel != null)
            {
                if (existingLevel.SkillGrade1 != 0)
                {
                    skills.Add(existingLevel.Skill1);
                }
                if (existingLevel.SkillGrade2 != 0)
                {
                    skills.Add(existingLevel.Skill2);
                }
                if (existingLevel.SkillGrade3 != 0)
                {
                    skills.Add(existingLevel.Skill3);
                }
                return skills;
            }
            return skills;
        }

        private sealed class CachedLevel12
        {
            public string Path;
            public DateTime Stamp;
            public List<Level12> List;
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, CachedLevel12> Level12Cache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, CachedLevel12>(StringComparer.Ordinal);

        /// <summary>
        /// 按文件时间戳缓存的 Level12 列表。
        /// 原实现每次发道具（RandomItemSkill → GetItemSkill → GetSkills）都要读盘 + 解析 JSON，
        /// 文件被占用/写入时解析失败会静默返回空列表，导致词条"时灵时不灵"。
        /// </summary>
        public static List<Level12> GetLevel12List(string Nickname)
        {
            if (string.IsNullOrEmpty(Nickname))
            {
                return new List<Level12>();
            }

            var filename = FileName.Load(Nickname);
            string path = filename?.Level12Data_LoadFile;
            if (string.IsNullOrEmpty(path))
            {
                return new List<Level12>();
            }

            DateTime stamp = DateTime.MinValue;
            try
            {
                if (File.Exists(path))
                {
                    stamp = File.GetLastWriteTimeUtc(path);
                }
            }
            catch
            {
            }

            if (Level12Cache.TryGetValue(Nickname, out CachedLevel12 cached)
                && string.Equals(cached.Path, path, StringComparison.OrdinalIgnoreCase)
                && cached.Stamp == stamp)
            {
                return cached.List;
            }

            List<Level12> list;
            if (stamp == DateTime.MinValue)
            {
                list = new List<Level12>();
            }
            else
            {
                list = JsonHelper.DeserializeNoBom<List<Level12>>(path) ?? new List<Level12>();
            }

            Level12Cache[Nickname] = new CachedLevel12 { Path = path, Stamp = stamp, List = list };
            return list;
        }

        /// <summary>存档被改写后调用（正常流程依赖文件时间戳自动失效，这里用于强制刷新）。</summary>
        public static void InvalidateLevel12(string Nickname)
        {
            if (!string.IsNullOrEmpty(Nickname))
            {
                Level12Cache.TryRemove(Nickname, out _);
            }
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
            int current = 201 + fullCycles * 23; // Each full cycle adds net +23 (30 -7)
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

        public void ExceedSpec(string Nickname, KartSpec Kart)
        {
            if (!FileName.FileNames.ContainsKey(Nickname))
            {
                FileName.Load(Nickname);
            }
            var filename = FileName.FileNames[Nickname];
            var Parts12List = new List<Parts12>();
            if (File.Exists(filename.Parts12Data_LoadFile))
            {
                Parts12List = JsonHelper.DeserializeNoBom<List<Parts12>>(filename.Parts12Data_LoadFile) ?? new List<Parts12>();
            }
            var Level12List = GetLevel12List(Nickname);

            if (Kart.defaultExceedType > 0)
            {
                var v2config = ProfileService.GetProfileConfig(Nickname);
                if (v2config?.RiderItem == null)
                {
                    Log.Info($"[Parts12_ExcData] Warning: ProfileConfig or RiderItem is null for {Nickname}");
                    return;
                }
                var KartAndSN = new { Kart = v2config.RiderItem.Set_Kart, SN = v2config.RiderItem.Set_KartSN };
                var existingParts = Parts12List.FirstOrDefault(parts => parts.ID == KartAndSN.Kart && parts.SN == KartAndSN.SN);

                // Handle exceed type
                short Engine = (short)Kart.defaultEngineType;
                short Handle = (short)Kart.defaultHandleType;
                short Wheel = (short)Kart.defaultWheelType;
                short Booster = (short)Kart.defaultBoosterType;

                if (existingParts != null)
                {
                    if (existingParts.ExceedType != 0)
                    {
                        Kart.defaultExceedType = existingParts.ExceedType;
                    }
                    else
                    {
                        existingParts.ExceedType = (short)Kart.defaultExceedType;
                    }
                    KartExcData.Save(filename.Parts12Data_LoadFile, Parts12List);

                    Engine = existingParts.Engine > 1 ? existingParts.Engine : Engine;
                    Handle = existingParts.Handle > 1 ? existingParts.Handle : Handle;
                    Wheel = existingParts.Wheel > 1 ? existingParts.Wheel : Wheel;
                    Booster = existingParts.Booster > 1 ? existingParts.Booster : Booster;
                }

                Log.Info($"-------------------------------------------------------------");

                V2Parts_TransAccelFactor = (float)((Get12Parts(Engine) * 1.0M - 800M) / 25000.0M + 0.4765M);
                V2Default_TransAccelFactor = (float)((Get12Parts((short)Kart.defaultEngineType) * 1.0M - 800M) / 25000.0M + 0.4765M);
                Log.Info($"[{Nickname}]V2Parts_TransAccelFactor: {V2Parts_TransAccelFactor}");

                V2Parts_SteerConstraint = (float)((Get12Parts(Handle) * 1.0M - 800M) / 250.0M + 2.7M);
                V2Default_SteerConstraint = (float)((Get12Parts((short)Kart.defaultHandleType) * 1.0M - 800M) / 250.0M + 2.7M);
                Log.Info($"[{Nickname}]V2Parts_SteerConstraint: {V2Parts_SteerConstraint}");

                V2Parts_DriftEscapeForce = (float)(Get12Parts(Wheel) * 2.0M);
                V2Default_DriftEscapeForce = (float)(Get12Parts((short)Kart.defaultWheelType) * 2.0M);
                Log.Info($"[{Nickname}]V2Parts_DriftEscapeForce: {V2Parts_DriftEscapeForce}");

                V2Parts_NormalBoosterTime = (float)(Get12Parts(Booster) * 1.0M - 260M);
                V2Default_NormalBoosterTime = (float)(Get12Parts((short)Kart.defaultBoosterType) * 1.0M - 260M);
                Log.Info($"[{Nickname}]V2Parts_NormalBoosterTime: {V2Parts_NormalBoosterTime}");

                Log.Info($"-------------------------------------------------------------");

                var existingLevel = Level12List.FirstOrDefault(level => level.ID == KartAndSN.Kart && level.SN == KartAndSN.SN);
                if (existingLevel != null)
                {
                    Log.Info($"-------------------------------------------------------------");

                    var skillData = new Dictionary<short, (string Name, float[] Values)>
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
                    if (skillData.TryGetValue(existingLevel.Skill1, out var data1))
                    {
                        typeof(V2Specs).GetProperty($"V2Level_{data1.Name}")?
                            .SetValue(this, data1.Values[existingLevel.SkillGrade1]);
                        Log.Info($"[{Nickname}]V2Level_{data1.Name}: {data1.Values[existingLevel.SkillGrade1]}");
                    }
                    if (skillData.TryGetValue(existingLevel.Skill2, out var data2))
                    {
                        typeof(V2Specs).GetProperty($"V2Level_{data2.Name}")?
                            .SetValue(this, data2.Values[existingLevel.SkillGrade2]);
                        Log.Info($"[{Nickname}]V2Level_{data2.Name}: {data2.Values[existingLevel.SkillGrade2]}");
                    }
                    if (skillData.TryGetValue(existingLevel.Skill3, out var data3))
                    {
                        typeof(V2Specs).GetProperty($"V2Level_{data3.Name}")?
                            .SetValue(this, data3.Values[existingLevel.SkillGrade3]);
                        Log.Info($"[{Nickname}]V2Level_{data3.Name}: {data3.Values[existingLevel.SkillGrade3]}");
                    }
                    Log.Info($"-------------------------------------------------------------");
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

                    Kart.chargeInstAccelGaugeByWallAdded = 0f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0f;
                    Kart.chargerSystemboosterUseCount = 0;
                    Kart.chargerSystemUseTime = 0;
                    Kart.chargeBoostBySpeedAdded = 0f;
                    Kart.driftGaugeFactor = 0f;
                    Kart.chargeAntiCollideBalance = 0f;

                    // 词条："比赛开始时获得[加速器]，[香蕉]，[乌云]，[定时水炸弹]中的一种"。
                    // 原实现用 RandomItemSkill(...,2) 从个人道具全表（14 种）随机，会给出词条之外的道具；
                    // 改为从 V2StartItem.json 的候选池里等概率取一个。
                    Kart.startItemId = V2StartItem.Pick(Nickname, v2config?.RiderItem?.Set_Kart ?? 0);
                }
                else if (Kart.defaultExceedType == 2)//S
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

                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 4;
                    Kart.chargerSystemUseTime = 3000;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                    Kart.chargeAntiCollideBalance = 0.8f;
                }
                else if (Kart.defaultExceedType == 3)//B
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.02f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.19f;
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

                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 5;
                    Kart.chargerSystemUseTime = 3750;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                    Kart.chargeAntiCollideBalance = 0.8f;
                }
                else if (Kart.defaultExceedType == 4)//L
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.02f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.16f;
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

                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 6;
                    Kart.chargerSystemUseTime = 4500;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                    Kart.chargeAntiCollideBalance = 0.8f;
                }
                else if (Kart.defaultExceedType == 5)//?
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

                    Kart.chargeInstAccelGaugeByWallAdded = 0f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0f;
                    Kart.chargerSystemboosterUseCount = 0;
                    Kart.chargerSystemUseTime = 0;
                    Kart.chargeBoostBySpeedAdded = 0f;
                    Kart.driftGaugeFactor = 0f;
                    Kart.chargeAntiCollideBalance = 0f;
                }
                else if (Kart.defaultExceedType == 6)//L时间
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.017f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.16f;
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

                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.02f;
                    Kart.chargerSystemboosterUseCount = 6;
                    Kart.chargerSystemUseTime = 4500;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                    Kart.chargeAntiCollideBalance = 0.8f;
                }
                else if (Kart.defaultExceedType == 7)//L赋能
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.02f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.16f;
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

                    Kart.chargeInstAccelGaugeByWallAdded = 0.1f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.04f;
                    Kart.chargerSystemboosterUseCount = 5;
                    Kart.chargerSystemUseTime = 4750;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                    Kart.chargeAntiCollideBalance = 0.8f;
                }
                else if (Kart.defaultExceedType == 8)//S加速
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.02f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.32f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 1040f;
                    Kart.instAccelGaugeMinUsable = 260f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 0;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 200f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 4;
                    Kart.chargerSystemUseTime = 2700;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                    Kart.chargeAntiCollideBalance = 0.8f;
                }
                else if (Kart.defaultExceedType == 9)//B碰撞
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.02f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.18f;
                    Kart.instAccelFactor = 1.19f;
                    Kart.instAccelGaugeCooldownTime = 2100;
                    Kart.instAccelGaugeLength = 2000f;
                    Kart.instAccelGaugeMinUsable = 400f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 0;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 200f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeInstAccelGaugeByWallAdded = 0.11f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 5;
                    Kart.chargerSystemUseTime = 3750;
                    Kart.chargeBoostBySpeedAdded = 350.0f;
                    Kart.driftGaugeFactor = 2.0f;
                    Kart.chargeAntiCollideBalance = 0.6f;
                }
                else if (Kart.defaultExceedType == 10)//B集气
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.03f;
                    Kart.chargeInstAccelGaugeByGrip = 0.07f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.1425f;
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

                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 5;
                    Kart.chargerSystemUseTime = 3750f;
                    Kart.chargeBoostBySpeedAdded = 350f;
                    Kart.driftGaugeFactor = 2f;
                    Kart.chargeAntiCollideBalance = 0.8f;
                }
                else if (Kart.defaultExceedType == 99)//小东B+
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.024f;
                    Kart.chargeInstAccelGaugeByGrip = 0.06f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.19f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 2400f;
                    Kart.instAccelGaugeMinUsable = 400f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 0;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 200f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 4;
                    Kart.chargerSystemUseTime = 3000f;
                    Kart.chargeBoostBySpeedAdded = 350f;
                    Kart.driftGaugeFactor = 2f;
                    Kart.chargeAntiCollideBalance = 0.8f;
                }
                else if (Kart.defaultExceedType == 100)//小东C
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.03f;
                    Kart.chargeInstAccelGaugeByGrip = 0.06f;
                    Kart.chargeInstAccelGaugeByWall = 0.1f;
                    Kart.instAccelFactor = 1.18f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 1500f;
                    Kart.instAccelGaugeMinUsable = 180f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = 0;
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 210f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeInstAccelGaugeByWallAdded = 0.09f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0.03f;
                    Kart.chargerSystemboosterUseCount = 4;
                    Kart.chargerSystemUseTime = 3000f;
                    Kart.chargeBoostBySpeedAdded = 350f;
                    Kart.driftGaugeFactor = 2f;
                    Kart.chargeAntiCollideBalance = 0.8f;
                }
                else
                {
                    Kart.chargeInstAccelGaugeByBoost = 0.02f;
                    Kart.chargeInstAccelGaugeByGrip = 0.06f;
                    Kart.chargeInstAccelGaugeByWall = 0.15f;
                    Kart.instAccelFactor = 1.11f;
                    Kart.instAccelGaugeCooldownTime = 3000;
                    Kart.instAccelGaugeLength = 2500f;
                    Kart.instAccelGaugeMinUsable = 750f;
                    Kart.instAccelGaugeMinVelBound = 0f;
                    Kart.instAccelGaugeMinVelLoss = 50f;
                    Kart.useExtendedAfterBoosterMore = (byte)(false ? 1 : 0);
                    Kart.wallCollGaugeCooldownTime = 3000;
                    Kart.wallCollGaugeMaxVelLoss = 200f;
                    Kart.wallCollGaugeMinVelBound = 200f;
                    Kart.wallCollGaugeMinVelLoss = 50f;

                    Kart.chargeInstAccelGaugeByWallAdded = 0f;
                    Kart.chargeInstAccelGaugeByBoostAdded = 0f;
                    Kart.chargerSystemboosterUseCount = 0;
                    Kart.chargerSystemUseTime = 0;
                    Kart.chargeBoostBySpeedAdded = 0f;
                    Kart.driftGaugeFactor = 0f;
                    Kart.chargeAntiCollideBalance = 1f;
                }
            }
        }

        public float V2Parts_TransAccelFactor { get; set; } = 0f;
        public float V2Default_TransAccelFactor { get; set; } = 0f;
        public float V2Parts_SteerConstraint { get; set; } = 0f;
        public float V2Default_SteerConstraint { get; set; } = 0f;
        public float V2Parts_DriftEscapeForce { get; set; } = 0f;
        public float V2Default_DriftEscapeForce { get; set; } = 0f;
        public float V2Parts_NormalBoosterTime { get; set; } = 0f;
        public float V2Default_NormalBoosterTime { get; set; } = 0f;

        public float V2Level_ForwardAccelForce { get; set; } = 0f;
        public float V2Level_CornerDrawFactor { get; set; } = 0f;
        public float V2Level_DragFactor { get; set; } = 0f;
        public float V2Level_NormalBoosterTime { get; set; } = 0f;
        public float V2Level_TeamBoosterTime { get; set; } = 0f;
        public float V2Level_StartBoosterTimeSpeed { get; set; } = 0f;
        public float V2Level_TransAccelFactor { get; set; } = 0f;
        public float V2Level_DriftEscapeForce { get; set; } = 0f;
        public float V2Level_DriftMaxGauge { get; set; } = 0f;
    }
}
