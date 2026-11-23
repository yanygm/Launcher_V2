using System;
using System.Collections.Generic;
using System.Linq;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;
using KartRider.Logging;
using KartRider;
using ExcData;
using Profile;

namespace RiderData
{
    public static class RiderSchool
    {
        public static byte catLevel = 0;
        public static byte maxStep = 0;
        public static List<byte> evenProStep = new List<byte>();
        public static List<byte> oddProStep = new List<byte>();

        /// <summary>每档驾照的真实关号区间（catLevel → 最小/最大 step + 关卡数），由 Rho.cs 解析 XML 填充。</summary>
        public struct LicenseRange
        {
            public byte Min;
            public byte Max;
            public byte Count;
        }

        public static readonly SortedDictionary<byte, LicenseRange> LicenseRanges = new SortedDictionary<byte, LicenseRange>();

        /// <summary>驾照等级 → 关卡数，全部由 Rho.cs 解析 riderSchoolLocale@{region}.xml 填充，不在代码里固化。</summary>
        public static readonly SortedDictionary<byte, byte> LicenseSteps = new SortedDictionary<byte, byte>();

        /// <summary>
        /// PRO 驾照（最后一档）的特殊设定，来自 riderSchoolLocale 的 proCategory：
        /// 徽章编号 + 3 条“达成赛道纪录”的时限（毫秒），客户端在 PRO 面板里显示这 3 个目标。
        /// </summary>
        public static ushort ProEmblemId = 0;
        public static readonly List<int> ProTimeLimits = new List<int>();

        /// <summary>PRO 徽章的三条纪录任务（来自 proCategory：赛道 ID + 速度档 + 时限）。</summary>
        public sealed class ProEmblemMission
        {
            public string Track;
            public byte GameSpeed = 7;
            public int TimeLimit;
        }

        public static readonly List<ProEmblemMission> ProMissions = new List<ProEmblemMission>();

        /// <summary>PRO 对数 = catLevel 6 的关卡数 / 2（TW=3、CN=6），以 evenProStep 为准。</summary>
        public static int ProPairCount
        {
            get
            {
                if (evenProStep.Count > 0)
                {
                    return evenProStep.Count;
                }
                int proSteps = LicenseSteps.TryGetValue(LicenseMaxLevel, out byte steps) ? steps : 0;
                return proSteps >= 2 ? proSteps / 2 : 1;
            }
        }

        /// <summary>驾照等级上限（PRO）：取客户端文件里的最大档位，数据未加载时为 0。</summary>
        public static byte LicenseMaxLevel
        {
            get
            {
                if (LicenseSteps.Count > 0)
                {
                    return LicenseSteps.Keys.Max();
                }
                // LicenseSteps 与 LicenseRanges 由 Rho.cs 同时填充，任一为空都说明还没读到文件
                return LicenseRanges.Count > 0 ? LicenseRanges.Keys.Max() : (byte)0;
            }
        }

        /// <summary>PRO 起点 = PRO 档第一关的关号 − 1（即 L1 最后一关，CN=30），读不到时按关数累加。</summary>
        public static byte ProBaseStep
        {
            get
            {
                byte maxLevel = LicenseMaxLevel;
                if (LicenseRanges.TryGetValue(maxLevel, out LicenseRange proRange) && proRange.Min > 1)
                {
                    return (byte)(proRange.Min - 1);
                }
                return (byte)GetStepsBeforeLevel(maxLevel);
            }
        }

        /// <summary>全部关卡总数 = 所有档位里最大的关号（CN=42、TW=36），静态表只作兜底。</summary>
        public static byte LicenseTotalSteps
        {
            get
            {
                int max = 0;
                foreach (LicenseRange range in LicenseRanges.Values)
                {
                    if (range.Max > max)
                    {
                        max = range.Max;
                    }
                }
                if (max == 0)
                {
                    max = LicenseSteps.Values.Sum(v => (int)v);
                    if (maxStep > max)
                    {
                        max = maxStep;
                    }
                }
                return max > byte.MaxValue ? byte.MaxValue : (byte)max;
            }
        }

        /// <summary>累计关卡 → 已完成的档数（0~LicenseMaxLevel）：按每档最大关号判断，打完 30→5、42→6。</summary>
        public static byte GetLevelFromSteps(byte totalSteps)
        {
            byte level = 0;
            if (LicenseRanges.Count > 0)
            {
                foreach (var item in LicenseRanges)
                {
                    if (totalSteps >= item.Value.Max)
                    {
                        level = item.Key;
                    }
                    else
                    {
                        break;
                    }
                }
                return level > LicenseMaxLevel ? LicenseMaxLevel : level;
            }

            // 兜底：数据没加载时按各档关数累加
            int sum = 0;
            foreach (var item in LicenseSteps)
            {
                sum += item.Value;
                if (totalSteps >= sum)
                {
                    level = item.Key;
                }
                else
                {
                    break;
                }
            }
            return level > LicenseMaxLevel ? LicenseMaxLevel : level;
        }

        /// <summary>
        /// 关卡号 → 所属档位（catLevel）。与 GetLevelFromSteps 不同：这里问“这一关属于哪一档”，
        /// 所以 25 关（刚进 L1）也算 5。PRO 到期退回按它算：CN 31~42→6，其余见 LicenseRanges。
        /// </summary>
        public static byte GetLevelOfStep(byte step)
        {
            if (LicenseRanges.Count > 0)
            {
                foreach (var item in LicenseRanges)
                {
                    if (step >= item.Value.Min && step <= item.Value.Max)
                    {
                        return item.Key > LicenseMaxLevel ? LicenseMaxLevel : item.Key;
                    }
                }
                // 落在第一档之前（还没开始考）→ 0；比所有档位的最大关号还大 → 满级
                return step > LicenseTotalSteps ? LicenseMaxLevel : (byte)0;
            }

            // 兜底：数据没加载时按各档关数累加
            int sum = 0;
            foreach (var item in LicenseSteps)
            {
                sum += item.Value;
                if (step <= sum)
                {
                    return item.Key;
                }
            }
            return LicenseMaxLevel;
        }

        /// <summary>某档最后一关的关号（L1→30、PRO→42），退回时用来把水位收回该档内。</summary>
        public static byte GetLevelMaxStep(byte level)
        {
            if (LicenseRanges.TryGetValue(level, out LicenseRange range) && range.Max > 0)
            {
                return range.Max;
            }
            int total = 0;
            foreach (var item in LicenseSteps)
            {
                if (item.Key > level)
                {
                    break;
                }
                total += item.Value;
            }
            return total > byte.MaxValue ? byte.MaxValue : (byte)total;
        }

        /// <summary>进入某个驾照等级所需的累计关卡数（= 该档第一关的关号 - 1，读不到时按关数累加）。</summary>
        public static int GetStepsBeforeLevel(byte level)
        {
            if (LicenseRanges.TryGetValue(level, out LicenseRange range) && range.Min > 0)
            {
                return range.Min - 1;
            }
            int sum = 0;
            foreach (var item in LicenseSteps)
            {
                if (item.Key >= level)
                {
                    break;
                }
                sum += item.Value;
            }
            return sum;
        }

        public static void PrStartRiderSchool(SessionGroup Parent, string Nickname)
        {
            using (OutPacket oPacket = new OutPacket("PrStartRiderSchool"))
            {
                oPacket.WriteByte(1);
                StartGameData.GetSchoolSpac(oPacket, Nickname);
                Parent.Client.Send(oPacket);
            }
        }

        /// <summary>车手学院（培训中心）数量、每个中心的关卡数、总关卡数。</summary>
        public const int TrainingCenterCount = 3;
        public const byte TrainingCenterSteps = 4;

        public static void PrRiderSchoolData(SessionGroup Parent)
        {
            // 先做状态校正：换赛道清完成位 / 90 天到期退回 L1
            SyncProLicense(Parent.Client.Nickname);

            var config = ProfileService.GetProfileConfig(Parent.Client.Nickname);
            GetProgress(Parent, out byte level, out byte step);
            byte[] centerStages = GetTrainingCenterStages(Parent);
            // 客户端的「PRO 考取日期」：按「日期 + 90 天 > 现在」判定有效，无效时必须发远古日期。
            DateTime licenseTime = IsProValid(config) ? GetProLicenseStart(config) : ProNoLicenseDate;

            using (OutPacket outPacket = new OutPacket("PrRiderSchoolDataPacket"))
            {
                // [u8 驾照档数][u8 已通关关卡][u16 时间(天)][u16 时间(刻)][u8][u32][u32 学院数量]
                // [i16 中心序号, i16 已通过关卡数]×3（条目必须是培训中心数 3，不能填驾照等级）
                // 第二字节 = LicenseStep：客户端用「关卡号 <= 它」判断已通关。
                outPacket.WriteByte(level);
                outPacket.WriteByte(step);
                outPacket.WriteDateTime(licenseTime);
                outPacket.WriteByte(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(TrainingCenterCount);
                for (byte center = 1; center <= TrainingCenterCount; center++)
                {
                    outPacket.WriteShort(center);
                    outPacket.WriteShort(centerStages[center - 1]);
                }
                Log.Info($"[RiderSchool] {Parent.Client.Nickname} 下发驾照/车手学院数据: 驾照等级={level}，最后通关关卡={step}/{LicenseTotalSteps}，"
                         + $"PRO驾照={(IsProValid(config) ? $"有效（{licenseTime:yyyy-MM-dd} 起，剩余 {GetProLicenseRemainDays(config)} 天）" : "无/已过期")}，"
                         + $"车手学院各中心={string.Join("/", centerStages)}，"
                         + $"字节={BitConverter.ToString(outPacket.ToArray()).Replace('-', ' ')}");
                Parent.Client.Send(outPacket);
            }
        }

        /// <summary>玩家当前进入的培训中心（由 PqStartTrainingCenter 记录，1~3）。</summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> CurrentTrainingCenter =
            new System.Collections.Concurrent.ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

        /// <summary>PqStartTrainingCenter：记住玩家当前所在的培训中心（通关上报只带关卡序号）。</summary>
        public static void BeginTrainingCenter(SessionGroup session, byte centerId)
        {
            string nickname = session?.Client?.Nickname;
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }
            byte center = centerId;
            if (center < 1 || center > TrainingCenterCount)
            {
                // 兼容 0 基编号
                center = (byte)(centerId + 1);
            }
            if (center < 1 || center > TrainingCenterCount)
            {
                center = 1;
            }
            CurrentTrainingCenter[nickname] = center;
            Log.Info($"[RiderSchool] {nickname} 进入车手学院 中心{center}（原始值 {centerId}）");
        }

        /// <summary>读取三个培训中心各自的已通过关卡数（每个 0~4）。</summary>
        public static byte[] GetTrainingCenterStages(SessionGroup session)
        {
            var result = new byte[TrainingCenterCount];
            string nickname = session?.Client?.Nickname;
            if (string.IsNullOrEmpty(nickname))
            {
                return result;
            }
            var config = ProfileService.GetProfileConfig(nickname);
            var stored = config?.Rider?.TrainingCenterStages;
            if (stored == null)
            {
                return result;
            }
            for (int i = 0; i < TrainingCenterCount && i < stored.Count; i++)
            {
                byte value = stored[i];
                result[i] = value > TrainingCenterSteps ? TrainingCenterSteps : value;
            }
            return result;
        }

        /// <summary>
        /// PqUpdateTrainingCenterDataPacket：车手学院通关上报（单字节，内容是该关卡所属的培训中心编号），无回包。
        /// 三个中心各自从第 1 关开始，进度必须按中心分别+1，不能累加到第一个中心。
        /// </summary>
        public static void RecordTrainingCenterClear(SessionGroup session, byte reportedValue)
        {
            string nickname = session?.Client?.Nickname;
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }
            var config = ProfileService.GetProfileConfig(nickname);
            if (config?.Rider == null)
            {
                return;
            }
            if (config.Rider.TrainingCenterStages == null || config.Rider.TrainingCenterStages.Count < TrainingCenterCount)
            {
                config.Rider.TrainingCenterStages = new List<byte> { 0, 0, 0 };
            }

            // 中心来自 PqStartTrainingCenter；上报值本身是该中心内的关卡序号
            if (!CurrentTrainingCenter.TryGetValue(nickname, out byte center))
            {
                center = 1;
            }
            int index = center - 1;
            byte current = config.Rider.TrainingCenterStages[index];

            byte next;
            if (reportedValue >= 1 && reportedValue <= TrainingCenterSteps)
            {
                // 上报的是中心内的关卡序号（1~4）：只前进不后退
                next = reportedValue > current ? reportedValue : current;
            }
            else
            {
                // 其它值（0、越界）按“通过一关”推进
                next = Math.Min((byte)(current + 1), TrainingCenterSteps);
            }
            Log.Info($"[RiderSchool] {nickname} 车手学院通关 中心{center} value={reportedValue} → 进度 {current} -> {next}/{TrainingCenterSteps}");
            if (next != current)
            {
                config.Rider.TrainingCenterStages[index] = next;
                ProfileService.Save(nickname, config);
            }
        }

        /// <summary>读取驾照进度：level = 已完成的档数（5=L1、6=PRO 有效期内），step = 下发的 LicenseStep。</summary>
        public static void GetProgress(SessionGroup session, out byte level, out byte step)
        {
            level = 0;
            step = 0;
            string nickname = session?.Client?.Nickname;
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }
            var config = ProfileService.GetProfileConfig(nickname);
            if (config?.Rider == null)
            {
                return;
            }

            level = config.Rider.LicenseLevel;
            step = GetEffectiveStep(config);
            if (level > LicenseMaxLevel)
            {
                level = LicenseMaxLevel;
            }

            if (IsProValid(config))
            {
                // 有徽章 + 当期两关都通过 → PRO 驾照
                level = LicenseMaxLevel;
                return;
            }

            // 等级不随关卡自动推进（必须点“获取驾照”），这里只把越级的旧数据压回
            // 「当前关卡所在档位」，并封顶到 L1（PRO 只能靠徽章 + 打通当期两关授予）。
            byte eligible = GetLevelOfStep(step);
            byte cap = (byte)Math.Max(0, LicenseMaxLevel - 1);
            if (eligible > cap)
            {
                eligible = cap;
            }
            if (level > eligible)
            {
                level = eligible;
            }
        }

        // ================= PRO 驾照（周期性两关，有效期 90 天） =================
        //
        // 规则：徽章（永久）= 挑战资格；打通当期计时赛 + 对决 → PRO 驾照，有效期 90 天；
        // 单数月 1 号换赛道（RouterListener.DataTime()），换赛道不清驾照，重打当期两关即续期；
        // 到期未续期 → 按 LicenseStep 所属档位退回（PRO 段退 L1）。
        //
        // PRO 段进度就存在 LicenseStep：客户端只有一条判定「关卡号 <= LicenseStep = 已通关」，
        // 所以通关后把 LicenseStep 更新成该关关号（41→42），换周期时压回 PRO 起点（30）。

        /// <summary>PRO 当期两关的完成位：计时赛。</summary>
        public const byte ProSpeedBit = 1;

        /// <summary>PRO 当期两关的完成位：对决。</summary>
        public const byte ProVersusBit = 2;

        /// <summary>PRO 当期两关都通过。</summary>
        public const byte ProAllCleared = ProSpeedBit | ProVersusBit;

        /// <summary>PRO 徽章应答固定带 3 条成绩（客户端包结构写死 i32×3），缺条目补 0。</summary>
        public const int ProMissionSlotCount = 3;

        /// <summary>PRO 驾照有效期（天）。</summary>
        public const int ProLicenseDays = 90;

        /// <summary>
        /// 下发给客户端的「没有 PRO 驾照」日期。客户端按「考取日期 + 90 天 > 现在」判有效，
        /// 用 1900-01-02 而不是 0xFFFF（后者当无符号数会变成 2079 年，反而“有效”）。
        /// </summary>
        public static readonly DateTime ProNoLicenseDate = new DateTime(1900, 1, 2);

        /// <summary>PRO 驾照是否有效：有徽章 + 未过期（ProLicenseUntil &gt; 现在）。</summary>
        public static bool IsProValid(ProfileConfig config)
        {
            if (config?.Rider == null || !config.Rider.ProEmblemEarned)
            {
                return false;
            }
            return config.Rider.ProLicenseUntil > DateTime.Now;
        }

        /// <summary>PRO 驾照的考取（续期）日期：有效期往前推 90 天，用于下发给客户端显示剩余天数。</summary>
        public static DateTime GetProLicenseStart(ProfileConfig config)
        {
            DateTime until = config?.Rider?.ProLicenseUntil ?? DateTime.MinValue;
            if (until <= DateTime.MinValue)
            {
                return DateTime.MinValue;
            }
            return until.AddDays(-ProLicenseDays);
        }

        /// <summary>剩余有效天数（已过期或没拿到返回 0）。</summary>
        public static int GetProLicenseRemainDays(ProfileConfig config)
        {
            DateTime until = config?.Rider?.ProLicenseUntil ?? DateTime.MinValue;
            if (until <= DateTime.Now)
            {
                return 0;
            }
            return (int)Math.Ceiling((until - DateTime.Now).TotalDays);
        }

        /// <summary>
        /// 下发给客户端的「上次通关的关卡」= 存档 LicenseStep（含 PRO 段 31~42），越界时截断。
        /// 客户端只按「关卡号 &lt;= 它」判已通关：30 → 两关都没过、41 → 计时赛过、42 → 两关都过。
        /// </summary>
        public static byte GetEffectiveStep(ProfileConfig config)
        {
            int step = config?.Rider?.LicenseStep ?? 0;
            if (step < 0)
            {
                step = 0;
            }
            if (step > LicenseTotalSteps)
            {
                step = LicenseTotalSteps;
            }
            return (byte)step;
        }

        /// <summary>
        /// 驾照状态校正（登录 / 打开驾照面板 / 通关上报时都会走一遍）：
        ///   1) 单数月 1 号换赛道 → 当期两关完成位清零（驾照有效期不变，通关可续期）；
        ///   2) 有效期到期（90 天未续期）→ 按当前 LicenseStep 所属档位退回（PRO 段 → L1），徽章保留可重考；
        ///   3) 有徽章但没考过 / 考过且未过期 → 等级与有效期保持一致。
        /// 返回 true 表示档案有变化，调用方需要保存。
        /// </summary>
        public static bool SyncProLicense(ProfileConfig config, out string message)
        {
            message = null;
            if (config?.Rider == null)
            {
                return false;
            }
            var rider = config.Rider;
            int period = RouterListener.DataTime();
            byte cap = (byte)Math.Max(0, LicenseMaxLevel - 1);
            var notes = new List<string>();
            bool changed = false;

            if (!rider.ProEmblemEarned)
            {
                // 没有徽章就没有 PRO：清掉有效期，等级压回 L1，水位压回 PRO 起点
                if (rider.ProLicenseUntil != DateTime.MinValue)
                {
                    rider.ProLicenseUntil = DateTime.MinValue;
                    changed = true;
                }
                if (rider.LicenseLevel > cap)
                {
                    rider.LicenseLevel = cap;
                    changed = true;
                    notes.Add("旧存档无徽章却持有 PRO，按新规则退回 L1（拿到徽章并打通当期两关后升级）");
                }
                if (rider.LicenseStep > ProBaseStep)
                {
                    rider.LicenseStep = ProBaseStep;
                    changed = true;
                }
                message = notes.Count == 0 ? null : string.Join("；", notes);
                return changed;
            }

            // 等级已是 PRO 却没有考取时间（旧版拿徽章直接给 PRO）→ 按“此刻考取”补上 90 天
            if (rider.ProLicenseUntil == DateTime.MinValue && rider.LicenseLevel >= LicenseMaxLevel)
            {
                rider.ProLicenseUntil = DateTime.Now.AddDays(ProLicenseDays);
                changed = true;
                notes.Add($"PRO 驾照补录考取时间 {DateTime.Now:yyyy-MM-dd}（有效期至 {rider.ProLicenseUntil:yyyy-MM-dd}，{ProLicenseDays} 天）");
            }

            // 旧存档迁移：没有考取时间但本期两关已打完 → 按此时重新计 90 天
            if (rider.ProLicenseUntil == DateTime.MinValue
                && rider.ProPeriodCleared == ProAllCleared
                && rider.ProLicensePeriod == period)
            {
                rider.ProLicenseUntil = DateTime.Now.AddDays(ProLicenseDays);
                changed = true;
                notes.Add("PRO 存档迁移：本期两关已通过，按考取处理并重新计 90 天");
            }

            // 换赛道：完成位只对当期有效，水位立即跟着换到新组的同一位置（41→31、42→32），
            // 不等玩家再打一关才更新，否则两张卡还带着上期的 41/42 显示成已通关。
            if (rider.ProLicensePeriod != period)
            {
                rider.LicenseStep = MapProStepToPeriod(rider.LicenseStep, rider.ProLicensePeriod, period);
                rider.ProLicensePeriod = period;
                rider.ProPeriodCleared = GetProFlagsOfStep(rider.LicenseStep);
                changed = true;
                notes.Add(IsProValid(config)
                    ? "换赛道：本期两关可重新挑战，通关即续 90 天"
                    : "换赛道：当期两关完成位清零");
            }

            // 到期退回：PRO 一档失效，等级与水一律退到 L1（L1 最后一关 = 30），PRO 两关回到未通关
            if (rider.ProLicenseUntil != DateTime.MinValue && rider.ProLicenseUntil <= DateTime.Now)
            {
                byte targetLevel = cap;                 // 5 = L1
                byte targetStep = GetLevelMaxStep(cap); // L1 最后一关（CN=30）

                string detail = $"PRO 驾照已于 {rider.ProLicenseUntil:yyyy-MM-dd} 到期（90 天未续期），退回 L1";
                if (rider.LicenseLevel != targetLevel)
                {
                    detail += $"（等级 {rider.LicenseLevel}→{targetLevel}）";
                    rider.LicenseLevel = targetLevel;
                    changed = true;
                }
                if (rider.LicenseStep != targetStep)
                {
                    detail += $"（水位 {rider.LicenseStep}→{targetStep}）";
                    rider.LicenseStep = targetStep;
                    changed = true;
                }

                // 完成位按退后的水位重算，否则后面的补偿逻辑会把水位顶回 PRO 段
                byte exFlags = GetProFlagsOfStep(rider.LicenseStep);
                if (rider.ProPeriodCleared != exFlags)
                {
                    rider.ProPeriodCleared = exFlags;
                    changed = true;
                }
                notes.Add(detail);
            }

            // 完成位与水位保持一致：旧存档可能出现“完成位=3 但水位停在 30”，这里补齐
            if (rider.ProPeriodCleared != 0)
            {
                GetProPeriodPair(out byte pSpeed, out byte pVersus);
                int target = (rider.ProPeriodCleared & ProVersusBit) != 0
                    ? pVersus
                    : ((rider.ProPeriodCleared & ProSpeedBit) != 0 ? pSpeed : ProBaseStep);
                if (rider.LicenseStep < target)
                {
                    rider.LicenseStep = (byte)Math.Min(target, LicenseTotalSteps);
                    changed = true;
                }
            }

            // 水位兜底：完成位为 0 表示本期一关未过，水位必须停在 L1 最后一关（30）。
            // 旧存档 / 上期残留的 41、42 会让客户端继续把 PRO 关卡判成已通关。
            if (rider.ProPeriodCleared == 0 && rider.LicenseStep > ProBaseStep)
            {
                rider.LicenseStep = ProBaseStep;
                changed = true;
            }

            if (IsProValid(config))
            {
                if (rider.LicenseLevel != LicenseMaxLevel)
                {
                    rider.LicenseLevel = LicenseMaxLevel;
                    changed = true;
                }
            }
            else if (rider.LicenseLevel > cap)
            {
                // 没考取 / 已过期 → 停在 L1（PRO 的上一档）
                rider.LicenseLevel = cap;
                changed = true;
            }

            message = notes.Count == 0 ? null : string.Join("；", notes);
            return changed;
        }

        /// <summary>按昵称做一次周期滚动，有变化就落盘。</summary>
        public static void SyncProLicense(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }
            var config = ProfileService.GetProfileConfig(nickname);
            if (config?.Rider == null)
            {
                return;
            }
            if (!SyncProLicense(config, out string message))
            {
                return;
            }
            ProfileService.Save(nickname, config);
            Log.Info($"[RiderSchool] {nickname} {message ?? "驾照状态已校正"}：周期={config.Rider.ProLicensePeriod}，"
                     + $"完成位={config.Rider.ProPeriodCleared}/{ProAllCleared}，驾照等级={config.Rider.LicenseLevel}，"
                     + $"PRO 有效期至 {(config.Rider.ProLicenseUntil == DateTime.MinValue ? "无" : config.Rider.ProLicenseUntil.ToString("yyyy-MM-dd"))}"
                     + $"（剩余 {GetProLicenseRemainDays(config)} 天）");
        }

        /// <summary>
        /// 解锁当期 PRO 挑战（领徽章时调用）：只置 ProEmblemEarned + 滚动周期，
        /// 不授予 PRO 驾照（等级保持 L1，打通当期两关才升 6）。
        /// </summary>
        private static void UnlockProChallenge(ProfileConfig config, string nickname, string reason)
        {
            int period = RouterListener.DataTime();
            var rider = config.Rider;
            bool changed = false;

            if (!rider.ProEmblemEarned)
            {
                rider.ProEmblemEarned = true;
                changed = true;
            }

            // 进入新周期才换水位，并同步完成位（重复领徽章不清掉本期进度）
            if (rider.ProLicensePeriod != period)
            {
                rider.LicenseStep = MapProStepToPeriod(rider.LicenseStep, rider.ProLicensePeriod, period);
                rider.ProLicensePeriod = period;
                rider.ProPeriodCleared = GetProFlagsOfStep(rider.LicenseStep);
                changed = true;
            }

            // 当期两关没打完就还是 L1（PRO 的上一档）；已打完则保持 6 不动
            if (!IsProValid(config))
            {
                byte cap = (byte)Math.Max(0, LicenseMaxLevel - 1);
                if (rider.LicenseLevel > cap)
                {
                    rider.LicenseLevel = cap;
                    changed = true;
                }
            }

            if (changed)
            {
                ProfileService.Save(nickname, config);
            }
            Log.Info($"[RiderSchool] {nickname} {reason} → PRO 挑战已解锁（周期={rider.ProLicensePeriod}，"
                     + $"完成位={rider.ProPeriodCleared}/{ProAllCleared}，等级={rider.LicenseLevel}，"
                     + $"徽章=永久保留，打通当期两关后获得 90 天 PRO 驾照）");
        }

        /// <summary>PRO 关卡通关：只记当期这一对的完成位（过计时赛关 → 41，过对决关 → 42），不累计。</summary>
        private static bool RecordProStageClear(ProfileConfig config, string nickname, byte stage)
        {
            var rider = config.Rider;
            GetProPeriodPair(out byte speedStage, out byte versusStage);

            if (!rider.ProEmblemEarned)
            {
                // 没有徽章就没有 PRO 挑战资格：不打乱档案，只记录日志
                Log.Info($"[RiderSchool] {nickname} 通过 PRO 关卡 {stage}，但尚未获得 PRO 徽章（徽章 = 挑战资格），忽略");
                return false;
            }

            if (stage != speedStage && stage != versusStage)
            {
                Log.Info($"[RiderSchool] {nickname} 通过 PRO 关卡 {stage}，不属于当期这一对 {speedStage}/{versusStage}（往期关卡），忽略");
                return false;
            }

            int period = RouterListener.DataTime();
            if (rider.ProLicensePeriod != period)
            {
                // 跨周期后才上报：先把水位从原组挪到新组的同一位置，再按本关更新
                rider.LicenseStep = MapProStepToPeriod(rider.LicenseStep, rider.ProLicensePeriod, period);
                rider.ProLicensePeriod = period;
                rider.ProPeriodCleared = GetProFlagsOfStep(rider.LicenseStep);
            }

            byte before = rider.ProPeriodCleared;
            byte beforeStep = rider.LicenseStep;

            // 客户端只按「关卡号 <= LicenseStep」判通关：通关后把 LicenseStep 更新成本关关号（只前进）
            if (stage > rider.LicenseStep)
            {
                rider.LicenseStep = stage;
            }

            // 完成位由 LicenseStep 推出（41=计时赛、42=对决），两者永远自洽
            byte flags = 0;
            if (rider.LicenseStep >= speedStage)
            {
                flags |= ProSpeedBit;
            }
            if (rider.LicenseStep >= versusStage)
            {
                flags |= ProVersusBit;
            }
            rider.ProPeriodCleared = flags;

            bool ok = rider.ProPeriodCleared == ProAllCleared;
            if (ok)
            {
                // 当期两关全部通过 → 获得 / 续期 PRO 驾照：有效期 = 现在起 90 天
                rider.LicenseLevel = LicenseMaxLevel;
                rider.ProLicenseUntil = DateTime.Now.AddDays(ProLicenseDays);
            }
            else
            {
                // 还差一关：本期还没考完，但已经在有效期内的不要因为“少打一关”掉级
                if (!IsProValid(config))
                {
                    byte cap = (byte)Math.Max(0, LicenseMaxLevel - 1);
                    if (rider.LicenseLevel > cap)
                    {
                        rider.LicenseLevel = cap;
                    }
                }
            }

            Log.Info($"[RiderSchool] {nickname} 通过 PRO 关卡 {stage}（当期 {speedStage}/{versusStage}）"
                     + $" → LicenseStep {beforeStep}->{rider.LicenseStep}，完成位 {before}->{rider.ProPeriodCleared}/{ProAllCleared}"
                     + (ok
                        ? $"，当期两关全部通过，PRO 驾照生效，有效期至 {rider.ProLicenseUntil:yyyy-MM-dd}（{ProLicenseDays} 天）"
                        : "，还差一关，本期未完成"));
            return rider.ProPeriodCleared != before || rider.LicenseStep != beforeStep;
        }

        public static void RecordStageClear(SessionGroup session, byte[] payload, int offset)
        {
            string nickname = session?.Client?.Nickname;
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }
            var config = ProfileService.GetProfileConfig(nickname);
            if (config?.Rider == null)
            {
                return;
            }
            // 周期滚动：跨周期时先清掉上期的完成位（两关没打完就退回 L1）
            bool dirty = SyncProLicense(config, out string syncMessage);
            if (dirty && !string.IsNullOrEmpty(syncMessage))
            {
                Log.Info($"[RiderSchool] {nickname} {syncMessage}（周期={config.Rider.ProLicensePeriod}，等级={config.Rider.LicenseLevel}）");
            }

            byte total = LicenseTotalSteps;
            byte proBase = ProBaseStep;
            byte level = config.Rider.LicenseLevel > LicenseMaxLevel ? LicenseMaxLevel : config.Rider.LicenseLevel;
            byte step = config.Rider.LicenseStep > total ? total : config.Rider.LicenseStep;
            byte baseStep = step > proBase ? proBase : step;   // 非 PRO 段的“有效进度”，仅用于判断是否需要累计

            TryParseStageReport(payload, offset, total, out uint missionId, out byte reportedStep, out int elapsedMs);
            Log.Info($"[RiderSchool] {nickname} 通关上报 mission={missionId} step={reportedStep} 用时={elapsedMs}ms "
                     + $"payload={ToHex(payload, offset)}（当前 等级{level} 上次通关{step}/{total}，PRO 起点 {proBase}）");

            // PRO 段（起点之后）：关卡号固定、按周期轮换，不能累加，只记当期这一对的完成位
            if (reportedStep > proBase && reportedStep <= total)
            {
                if (RecordProStageClear(config, nickname, reportedStep))
                {
                    dirty = true;
                }
                if (dirty)
                {
                    ProfileService.Save(nickname, config);
                }
                return;
            }

            byte newStep;
            if (reportedStep >= 1 && reportedStep <= proBase)
            {
                // 上报的是累计关卡序号：只前进不后退
                newStep = reportedStep > baseStep ? reportedStep : baseStep;
            }
            else
            {
                // 解析结果不可信：按“通过一关”推进（不超过 PRO 起点）
                newStep = (byte)Math.Min(baseStep + 1, proBase);
            }

            if (newStep > config.Rider.LicenseStep)
            {
                // 只累计关卡数；等级等玩家点“获取驾照”（PqUpdateRiderSchoolLevelPacket）时才前进
                config.Rider.LicenseStep = newStep;
                dirty = true;
                Log.Info($"[RiderSchool] {nickname} 驾照关卡进度更新为 累计{newStep}/{total}（等级仍为 {level}，待领取驾照）");
            }

            if (dirty)
            {
                ProfileService.Save(nickname, config);
            }
        }

        /// <summary>
        /// 从上报包里取出（missionId, 已通过关卡数, 用时）。
        /// 兼容两种常见布局：无前置上下文 [u32][u8][i32]，以及带 18 字节上下文 [18B][u32][u8][i32]。
        /// 只接受“关卡数落在 1..limit+1 且用时非负”的解析结果。
        /// </summary>
        private static void TryParseStageReport(byte[] payload, int offset, byte limit, out uint missionId, out byte reportedStep, out int elapsedMs)
        {
            missionId = 0;
            reportedStep = 0;
            elapsedMs = 0;
            if (payload == null || offset < 0)
            {
                return;
            }

            foreach (int contextLength in new[] { 0, 18 })
            {
                int start = offset + contextLength;
                if (payload.Length - start < 9)
                {
                    continue;
                }
                uint mission = BitConverter.ToUInt32(payload, start);
                byte step = payload[start + 4];
                int elapsed = BitConverter.ToInt32(payload, start + 5);
                if (step >= 1 && step <= limit && elapsed >= 0 && elapsed <= 7200000)
                {
                    missionId = mission;
                    reportedStep = step;
                    elapsedMs = elapsed;
                    return;
                }
            }
        }

        private static string ToHex(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset >= data.Length)
            {
                return "";
            }
            return BitConverter.ToString(data, offset, Math.Min(data.Length - offset, 64)).Replace('-', ' ');
        }

        /// <summary>
        /// PqUpdateRiderSchoolLevelPacket：整级通关后的晋级上报（[u8 新等级]），返回当前等级。
        /// grantDate = 该等级的“获得日期”（应答包时间字段）：PRO 取存档考取日，普通驾照为此刻。
        /// </summary>
        public static byte RecordPromotion(SessionGroup session, byte reportedValue, out DateTime grantDate)
        {
            grantDate = DateTime.Now;
            string nickname = session?.Client?.Nickname;
            if (string.IsNullOrEmpty(nickname))
            {
                return reportedValue;
            }
            var config = ProfileService.GetProfileConfig(nickname);
            if (config?.Rider == null)
            {
                return reportedValue;
            }

            byte total = LicenseTotalSteps;
            byte step = GetEffectiveStep(config);
            byte level = config.Rider.LicenseLevel > LicenseMaxLevel ? LicenseMaxLevel : config.Rider.LicenseLevel;

            byte eligible = GetLevelFromSteps(step);
            if (IsProValid(config))
            {
                // 徽章 + 当期两关都已通过：PRO 已经生效，这里只是保持一致
                eligible = LicenseMaxLevel;
            }
            else
            {
                // PRO 一档只能靠打通当期两关授予，点“获取驾照”最高到 L1
                byte cap = (byte)Math.Max(0, LicenseMaxLevel - 1);
                if (eligible > cap)
                {
                    eligible = cap;
                }
            }
            Log.Info($"[RiderSchool] {nickname} 获取驾照上报 value={reportedValue}（当前 等级{level} 累计{step}/{total} 可晋级到{eligible}）");

            // 每次点击最多前进一档，且不能超过“已通关卡允许的等级”：
            // 这样重复点击不会连加，没拿到足够的关卡也拿不到驾照。
            if (level < eligible)
            {
                level = (byte)(level + 1);
                config.Rider.LicenseLevel = level;

                // 升到 PRO 时同步写考取时间（截止 = 现在 + 90 天），避免“等级 6、有效期为空”的错位
                if (level >= LicenseMaxLevel)
                {
                    if (config.Rider.ProEmblemEarned)
                    {
                        config.Rider.ProLicenseUntil = DateTime.Now.AddDays(ProLicenseDays);
                        config.Rider.ProLicensePeriod = RouterListener.DataTime();
                        Log.Info($"[RiderSchool] {nickname} 获得 PRO 驾照，考取时间 {DateTime.Now:yyyy-MM-dd}，"
                                 + $"有效期至 {config.Rider.ProLicenseUntil:yyyy-MM-dd}（{ProLicenseDays} 天）");
                    }
                    else
                    {
                        Log.Info($"[RiderSchool] {nickname} 等级升到 PRO 但没有赛车大师徽章，不写考取时间");
                    }
                }

                ProfileService.Save(nickname, config);
                Log.Info($"[RiderSchool] {nickname} 获得驾照 等级{level}（累计 {step}/{total}，关卡进度保留）");
            }
            else
            {
                Log.Info($"[RiderSchool] {nickname} 暂不可晋级（等级{level}，可晋级到{eligible}），保持原等级");
            }

            // 应答时间字段用存档里的考取日反推，重复点“获取驾照”不能刷新 90 天倒计时
            if (level >= LicenseMaxLevel && IsProValid(config))
            {
                grantDate = GetProLicenseStart(config);
            }
            return level;
        }

        /// <summary>
        /// 领取 PRO 徽章（达成 3 条 proCategory 赛道纪录后由客户端发起）：
        /// 徽章只是挑战资格，不直接给 PRO 驾照，当期两关必须实际打通。
        /// </summary>
        public static void RecordProEmblem(SessionGroup session, ushort reportedEmblem)
        {
            string nickname = session?.Client?.Nickname;
            if (string.IsNullOrEmpty(nickname))
            {
                return;
            }
            var config = ProfileService.GetProfileConfig(nickname);
            if (config?.Rider == null)
            {
                return;
            }
            Log.Info($"[RiderSchool] {nickname} 领取 PRO 徽章 请求={reportedEmblem}（档案徽章={ProEmblemId}，"
                     + $"当前 等级{config.Rider.LicenseLevel} 累计{config.Rider.LicenseStep}/{LicenseTotalSteps}）");
            // 徽章是 PRO 驾照的授予条件：3 条赛道成绩全部达标才能获得
            bool prerequisitesMet = CheckProEmblemPrerequisites(nickname, out string detail);
            Log.Info($"[RiderSchool] {nickname} PRO 徽章前置条件: {detail}");
            if (!prerequisitesMet)
            {
                Log.Info($"[RiderSchool] {nickname} 拒绝领取 PRO 徽章：3 条成绩未全部达标");
                return;
            }
            UnlockProChallenge(config, nickname, "领取 PRO 徽章");
        }

        /// <summary>PRO 面板第 2/4 字节：当期计时赛关、上次通关的关卡（= LicenseStep）。</summary>
        public static void GetProPanelStages(ProfileConfig config, out byte currentStage, out byte lastCleared)
        {
            GetProPeriodPair(out currentStage, out _);
            lastCleared = GetEffectiveStep(config);
        }

        /// <summary>PRO 徽章应答的两个字节：当期计时赛关 + 上次通关的关卡（与面板同语义）。</summary>
        public static byte GetNextProPairStep(SessionGroup session, bool speedTrack)
        {
            string nickname = session?.Client?.Nickname;
            var config = string.IsNullOrEmpty(nickname) ? null : ProfileService.GetProfileConfig(nickname);
            GetProPanelStages(config, out byte currentStage, out byte lastCleared);
            return speedTrack ? currentStage : lastCleared;
        }

        /// <summary>
        /// PRO 赛道轮换下标：周期 = 从 1900 年起的奇数月计数，长度 = PRO 对数（CN 6、TW 3）。
        /// 轮换 ≠ 掉级：驾照按 ProLicenseUntil 保持有效，重打当期两关即续 90 天。
        /// </summary>
        public static int GetProPeriodIndex()
        {
            return GetProPeriodIndex(RouterListener.DataTime());
        }

        /// <summary>指定周期对应的轮换下标（周期 0 或数据缺失时取第一对）。</summary>
        public static int GetProPeriodIndex(int period)
        {
            int pairs = ProPairCount;
            if (pairs <= 0)
            {
                return 0;
            }
            int index = (period - 1) % pairs;
            return index < 0 ? 0 : index;
        }

        /// <summary>当期这一对 PRO 关卡的绝对编号（CN：31/32 … 41/42），来自 catLevel 6 两两成对。</summary>
        public static void GetProPeriodPair(out byte speedStage, out byte versusStage)
        {
            GetProPairOfPeriod(RouterListener.DataTime(), out speedStage, out versusStage);
        }

        /// <summary>指定周期对应的那一对 PRO 关卡（计时赛关 / 对决关）。</summary>
        public static void GetProPairOfPeriod(int period, out byte speedStage, out byte versusStage)
        {
            int index = GetProPeriodIndex(period);
            speedStage = index < evenProStep.Count ? evenProStep[index] : (byte)0;
            versusStage = index < oddProStep.Count ? oddProStep[index] : (byte)0;

            // 没读到 riderSchoolLocale（evenProStep 为空）时退化成第一对，避免下发 0 让客户端崩溃
            if (speedStage == 0 || versusStage == 0)
            {
                speedStage = (byte)(ProBaseStep + 1);
                versusStage = (byte)Math.Min(ProBaseStep + 2, LicenseTotalSteps);
            }
        }

        /// <summary>
        /// 换周期时把水位从原组这一对挪到新组的同一位置：41/42 → 31/32 时 41→31、42→32。
        /// 非 PRO 段（≤ L1 最后一关）原样保留；落在 PRO 段却对不上原组（旧存档残留）按未通关处理。
        /// </summary>
        public static byte MapProStepToPeriod(byte step, int oldPeriod, int newPeriod)
        {
            if (step <= ProBaseStep)
            {
                return step;
            }
            if (oldPeriod == newPeriod)
            {
                return step;
            }
            if (oldPeriod <= 0)
            {
                return ProBaseStep;   // 不知道原组，按本期未通关处理
            }

            GetProPairOfPeriod(oldPeriod, out byte oldSpeed, out byte oldVersus);
            if (step == oldSpeed || step == oldVersus)
            {
                GetProPairOfPeriod(newPeriod, out byte newSpeed, out byte newVersus);
                return step == oldSpeed ? newSpeed : newVersus;
            }
            return ProBaseStep;
        }

        /// <summary>水位 → 当期完成位（关卡号 ≤ 水位 = 已通关）。</summary>
        private static byte GetProFlagsOfStep(byte step)
        {
            GetProPeriodPair(out byte speedStage, out byte versusStage);
            byte flags = 0;
            if (step >= speedStage)
            {
                flags |= ProSpeedBit;
            }
            if (step >= versusStage)
            {
                flags |= ProVersusBit;
            }
            return flags;
        }

        /// <summary>PRO 徽章成绩取的是标准模式（速度档 7）+ 个人赛（0）的成绩。</summary>
        public const byte ProRecordSpeedType = 7;
        public const byte ProRecordGameType = 0;

        /// <summary>
        /// PRO 徽章第 index 条任务的玩家最好成绩（毫秒）：
        /// 按赛道 hash 去 TrackRank 里读「标准模式 / 个人赛」这一份成绩，没有则返回 0。
        /// </summary>
        public static uint GetProEmblemRecord(string nickname, int index)
        {
            if (string.IsNullOrEmpty(nickname) || index < 0 || index >= ProMissions.Count)
            {
                return 0;
            }
            ProEmblemMission mission = ProMissions[index];
            if (string.IsNullOrEmpty(mission?.Track))
            {
                return 0;
            }
            uint hash = Adler32Helper.GenerateAdler32_UNICODE(mission.Track, 0);
            return TrackRankData.GetBestTime(hash, ProRecordSpeedType, ProRecordGameType, nickname);
        }

        /// <summary>
        /// PRO 徽章的前置条件：3 条赛道成绩都要有记录且不慢于时限（record > 0 && record <= limit）。
        /// </summary>
        public static bool CheckProEmblemPrerequisites(string nickname, out string detail)
        {
            if (ProMissions.Count == 0)
            {
                // 没有 proCategory 配置（区服数据缺失）时不拦，否则玩家永远拿不到徽章
                detail = "未读取到 proCategory 配置，跳过前置校验";
                return true;
            }
            var parts = new List<string>();
            bool ok = true;
            for (int i = 0; i < ProMissions.Count; i++)
            {
                ProEmblemMission mission = ProMissions[i];
                uint record = GetProEmblemRecord(nickname, i);
                int limit = mission?.TimeLimit ?? 0;
                bool passed = record > 0 && (limit <= 0 || record <= limit);
                ok &= passed;
                parts.Add($"{mission?.Track}={record}/{(limit > 0 ? limit.ToString() : "-")}{(passed ? "✓" : "✗")}");
            }
            detail = parts.Count == 0 ? "未读取到 proCategory 配置" : string.Join(" ", parts);
            return ok;
        }

        /// <summary>
        /// PRO 驾照面板（PrRiderSchoolProPacket）：u8 徽章、u8 当期计时赛关、u8 等级、u8 上次通关关、i32×3 成绩。
        /// 第 4 字节「关卡号 &lt;= 它 = 已通关」是两张卡是否盖章的唯一依据；第 3 字节发真实等级（6/PRO、5/L1）。
        /// </summary>
        public static void PrRiderSchoolPro(SessionGroup Parent)
        {
            string nickname = Parent.Client.Nickname;
            SyncProLicense(nickname);

            var config = ProfileService.GetProfileConfig(nickname);
            bool emblemEarned = config?.Rider?.ProEmblemEarned == true;
            bool proValid = IsProValid(config);
            byte cleared = config?.Rider?.ProPeriodCleared ?? 0;

            GetProgress(Parent, out byte level, out byte step);
            GetProPanelStages(config, out byte speedStage, out byte lastCleared);   // 当期计时赛关 / 上次通关关

            byte emblemByte = emblemEarned ? (byte)1 : (byte)0;

            var records = new List<uint>();
            for (int i = 0; i < ProMissionSlotCount; i++)
            {
                records.Add(GetProEmblemRecord(nickname, i));
            }

            using (OutPacket oPacket = new OutPacket("PrRiderSchoolProPacket"))
            {
                oPacket.WriteByte(emblemByte);
                oPacket.WriteByte(speedStage);
                oPacket.WriteByte(level);
                oPacket.WriteByte(lastCleared);
                for (int i = 0; i < ProMissionSlotCount; i++)
                {
                    oPacket.WriteUInt(records[i]);
                }
                Log.Info($"[RiderSchool] {nickname} PRO 包字节={BitConverter.ToString(oPacket.ToArray()).Replace('-', ' ')}");
                Parent.Client.Send(oPacket);
            }
            Log.Info($"[RiderSchool] {nickname} 下发 PRO 数据: 勋章{emblemByte} 当期第{GetProPeriodIndex() + 1}/{ProPairCount}对 "
                     + $"计时赛关{speedStage} 上次通关关{lastCleared}（LicenseStep={config?.Rider?.LicenseStep}），完成位{cleared}/{ProAllCleared} 水位{step} "
                     + $"PRO{(proValid ? $"有效（剩余{GetProLicenseRemainDays(config)}天）" : "未考取/已过期(当前L1)")} 等级{level}，"
                     + $"成绩={string.Join(",", records)}");
        }
    }
}
