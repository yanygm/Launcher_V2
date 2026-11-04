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

        /// <summary>
        /// 驾照等级（0 基：0=新手、1=初级、2=L3、3=L2、4=L1、5=Pro）→ 该等级的关卡数。
        /// 与“车手学院”完全独立：这里是驾照，条目部分是培训中心。
        /// </summary>
        public static readonly SortedDictionary<byte, byte> LicenseSteps = new SortedDictionary<byte, byte>
        {
            { 0, 6 }, { 1, 6 }, { 2, 6 }, { 3, 6 }, { 4, 6 }, { 5, 12 }
        };

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


        /// <summary>PRO 关卡是成对的（计时赛 + 对决），一对 2 关。</summary>
        public static int ProPairCount => evenProStep.Count > 0 ? evenProStep.Count : 6;

        /// <summary>驾照等级上限（0 基）。</summary>
        public static byte LicenseMaxLevel => LicenseSteps.Keys.Max();

        /// <summary>全部驾照关卡总数（各等级累计，6+6+6+6+6+12 = 42）。</summary>
        public static byte LicenseTotalSteps => (byte)LicenseSteps.Values.Sum(v => (int)v);

        /// <summary>累计关卡数 → 已完成的驾照等级数（0 基，最多到上限）。</summary>
        public static byte GetLevelFromSteps(byte totalSteps)
        {
            int sum = 0;
            byte level = 0;
            foreach (var item in LicenseSteps)
            {
                sum += item.Value;
                if (totalSteps >= sum)
                {
                    level = (byte)(item.Key + 1);
                }
                else
                {
                    break;
                }
            }
            return level > LicenseMaxLevel ? LicenseMaxLevel : level;
        }

        /// <summary>累计关卡 → PRO 内的关卡序号（1~12，未到 PRO 时为 0）。</summary>
        public static byte GetProStage(byte totalSteps)
        {
            int before = GetStepsBeforeLevel(LicenseMaxLevel);
            int stage = totalSteps - before;
            return stage <= 0 ? (byte)0 : (byte)Math.Min(stage, LicenseSteps[LicenseMaxLevel]);
        }

        /// <summary>PRO 关卡序号 → 第几对（0 基，一对 2 关）。</summary>
        public static int GetProPairIndex(byte proStage)
        {
            // 每对 = 计时赛 1 关 + 对决 1 关，通关 2 关才换下一对
            int pair = proStage / 2;
            if (pair < 0)
            {
                pair = 0;
            }
            return pair >= ProPairCount ? ProPairCount - 1 : pair;
        }

        /// <summary>进入某个驾照等级所需的累计关卡数（等级 0 需要 0）。</summary>
        public static int GetStepsBeforeLevel(byte level)
        {
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
            GetProgress(Parent, out byte level, out byte step);
            byte[] centerStages = GetTrainingCenterStages(Parent);
            using (OutPacket outPacket = new OutPacket("PrRiderSchoolDataPacket"))
            {
                // 本客户端的读取顺序（15 字节头 + 3 条车手学院条目）：
                // u8 已完成的驾照数、u8 当前等级内已通过关卡、u16 时间(天)、u16 时间(刻)、
                // u8 保留、u32 保留、u32 车手学院数量、(i16 中心序号, i16 已通过关卡数)×3
                // 注意：驾照进度和车手学院进度是两套数据，条目不能填驾照等级，
                // 条数必须是 3（培训中心数），否则客户端解析错位，驾照面板会显示成全部完成。
                // 等级 0 基（0=新手，5=PRO）；第二字节是**全局最后通关关卡**（1..42，0=未通关）。
                // 客户端用「关卡编号 <= 该值」判断某关已通过、用「编号 = 该值 + 1」判断可挑战，
                // 所以这里必须下发累计关卡（升级不清零），下发档内进度会让后续关卡全部锁死。
                outPacket.WriteByte(level);
                outPacket.WriteByte(step);
                outPacket.WriteDateTime(DateTime.Now);
                outPacket.WriteByte(0);
                outPacket.WriteInt(0);
                outPacket.WriteInt(TrainingCenterCount);
                for (byte center = 1; center <= TrainingCenterCount; center++)
                {
                    outPacket.WriteShort(center);
                    outPacket.WriteShort(centerStages[center - 1]);
                }
                Log.Info($"[RiderSchool] {Parent.Client.Nickname} 下发驾照/车手学院数据: 驾照等级={level}，最后通关关卡={step}/{LicenseTotalSteps}，"
                         + $"车手学院各中心={string.Join("/", centerStages)}，"
                         + $"字节={BitConverter.ToString(outPacket.ToArray()).Replace('-', ' ')}");
                Parent.Client.Send(outPacket);
            }
        }

        /// <summary>玩家当前进入的培训中心（由 PqStartTrainingCenter 记录，1~3）。</summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> CurrentTrainingCenter =
            new System.Collections.Concurrent.ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

        /// <summary>
        /// PqStartTrainingCenter：客户端进入某个培训中心时上报，包里带中心编号。
        /// 通关上报（PqUpdateTrainingCenterDataPacket）只有关卡序号，所以必须先记住中心，否则会全部累加到第一个中心。
        /// </summary>
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

        /// <summary>读取玩家自己的驾照进度（0 = 一关都没过）。</summary>
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
            step = config.Rider.LicenseStep;
            if (level > LicenseMaxLevel)
            {
                level = LicenseMaxLevel;
            }
            if (step > LicenseTotalSteps)
            {
                step = LicenseTotalSteps;
            }
            // 等级不会被关卡进度自动推进（必须玩家点“获取驾照”）：
            // 这里只把等级收敛到“已完成关卡允许的最高等级”以内，防止旧数据越级。
            byte eligible = GetLevelFromSteps(step);
            if (level > eligible)
            {
                level = eligible;
            }
        }

        /// <summary>
        /// LoRqUpdateRiderSchoolDataPacket：客户端每通过一关上报一次，无回包。
        ///
        /// 该包体较短，各版本客户端布局略有差别（有无前置上下文、字段宽度不同），
        /// 因此这里解析所有候选布局并做合法性校验，同时**只允许逐关推进**（最多 +1）：
        /// 任何解析错位得到的值（例如 6）都不会再把整档关卡记成已完成。
        /// </summary>
        /// <summary>
        /// PRO 关卡按“计时赛(奇)/对决(偶)”两条轨分别记进度，客户端 PRO 面板要用这两个数字。
        /// </summary>
        private static void UpdateProTrackProgress(ProfileConfig config, byte cumulativeStep, byte reportedStep)
        {
            int proBase = GetStepsBeforeLevel(LicenseMaxLevel);
            if (cumulativeStep <= proBase)
            {
                return;
            }

            int step = reportedStep > proBase && reportedStep <= LicenseTotalSteps ? reportedStep : cumulativeStep;
            int index = (step - proBase - 1) / 2 + 1;
            if (index < 0)
            {
                index = 0;
            }
            if (index > ProPairCount)
            {
                index = ProPairCount;
            }

            if ((step - proBase) % 2 == 1)
            {
                if (index > config.Rider.ProSpeedCleared)
                {
                    config.Rider.ProSpeedCleared = (byte)index;
                }
            }
            else
            {
                if (index > config.Rider.ProVersusCleared)
                {
                    config.Rider.ProVersusCleared = (byte)index;
                }
            }
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

            byte total = LicenseTotalSteps;
            byte level = config.Rider.LicenseLevel > LicenseMaxLevel ? LicenseMaxLevel : config.Rider.LicenseLevel;
            byte step = config.Rider.LicenseStep > total ? total : config.Rider.LicenseStep;

            TryParseStageReport(payload, offset, total, out uint missionId, out byte reportedStep, out int elapsedMs);
            Log.Info($"[RiderSchool] {nickname} 通关上报 mission={missionId} step={reportedStep} 用时={elapsedMs}ms "
                     + $"payload={ToHex(payload, offset)}（当前 等级{level} 累计{step}/{total}）");

            byte newStep;
            if (reportedStep >= 1 && reportedStep <= total)
            {
                // 上报的是累计关卡序号：只前进不后退
                newStep = reportedStep > step ? reportedStep : step;
            }
            else
            {
                // 解析结果不可信：按“通过一关”推进
                newStep = (byte)Math.Min(step + 1, total);
            }

            if (newStep != step)
            {
                UpdateProTrackProgress(config, newStep, reportedStep);
                // 只累计关卡数；等级等玩家点“获取驾照”（PqUpdateRiderSchoolLevelPacket）时才前进
                config.Rider.LicenseStep = newStep;
                ProfileService.Save(nickname, config);
                Log.Info($"[RiderSchool] {nickname} 驾照关卡进度更新为 累计{newStep}/{total}（等级仍为 {level}，待领取驾照）");
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
        /// PqUpdateRiderSchoolLevelPacket：整级通关后的晋级上报（[u8 新等级]）。
        /// 记录到玩家档案并返回客户端要求的成功应答。
        /// </summary>
        public static byte RecordPromotion(SessionGroup session, byte reportedValue)
        {
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
            byte step = config.Rider.LicenseStep > total ? total : config.Rider.LicenseStep;
            byte level = config.Rider.LicenseLevel > LicenseMaxLevel ? LicenseMaxLevel : config.Rider.LicenseLevel;
            byte eligible = GetLevelFromSteps(step);
            Log.Info($"[RiderSchool] {nickname} 获取驾照上报 value={reportedValue}（当前 等级{level} 累计{step}/{total} 可晋级到{eligible}）");

            // 每次点击最多前进一档，且不能超过“已通关卡允许的等级”：
            // 这样重复点击不会连加，没拿到足够的关卡也拿不到驾照。
            if (level < eligible)
            {
                level = (byte)(level + 1);
                config.Rider.LicenseLevel = level;
                ProfileService.Save(nickname, config);
                Log.Info($"[RiderSchool] {nickname} 获得驾照 等级{level}（累计 {step}/{total}，关卡进度保留）");
            }
            else
            {
                Log.Info($"[RiderSchool] {nickname} 暂不可晋级（等级{level}，可晋级到{eligible}），保持原等级");
            }
            return level;
        }

        /// <summary>
        /// 领取 PRO 徽章。客户端只会在达成 3 条 proCategory 赛道纪录后才发这个请求，
        /// 服务端不做二次拦截（否则会出现“点了没保存”），收到就记录。
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
            // 徽章是 PRO 挑战的前置：3 条赛道成绩全部达标才能获得，拿到后才会开放 PRO 计时赛/对决
            bool prerequisitesMet = CheckProEmblemPrerequisites(nickname, out string detail);
            Log.Info($"[RiderSchool] {nickname} PRO 徽章前置条件: {detail}");
            if (!prerequisitesMet)
            {
                Log.Info($"[RiderSchool] {nickname} 拒绝领取 PRO 徽章：3 条成绩未全部达标");
                return;
            }
            if (!config.Rider.ProEmblemEarned)
            {
                config.Rider.ProEmblemEarned = true;
                config.Rider.LicenseLevel = 6; // PRO 驾照通关
                ProfileService.Save(nickname, config);
                Log.Info($"[RiderSchool] {nickname} PRO 徽章已记录（成绩全部达标，已开放 PRO 挑战）");
            }
        }

        /// <summary>PRO 当前进度所在那一对的计时赛/对决关卡（没有更多对时返回 0）。</summary>
        /// <summary>PRO 徽章应答里回当前这一对关卡编号。</summary>
        public static byte GetNextProPairStep(SessionGroup session, bool speedTrack)
        {
            GetCurrentProPair(session, out byte speedStage, out byte versusStage);
            return speedTrack ? speedStage : versusStage;
        }

        /// <summary>PRO 计时赛（奇数关 31/33/35/37/39/41）已通关数量。</summary>
        public static byte GetProSpeedCleared(SessionGroup session)
        {
            return GetProCleared(session, true);
        }

        /// <summary>PRO 对决（偶数关 32/34/36/38/40/42）已通关数量。</summary>
        public static byte GetProVersusCleared(SessionGroup session)
        {
            return GetProCleared(session, false);
        }

        private static byte GetProCleared(SessionGroup session, bool speedTrack)
        {
            string nickname = session?.Client?.Nickname;
            var config = string.IsNullOrEmpty(nickname) ? null : ProfileService.GetProfileConfig(nickname);
            if (config?.Rider == null)
            {
                return 0;
            }

            byte speed = config.Rider.ProSpeedCleared;
            byte versus = config.Rider.ProVersusCleared;
            if (speed == 0 && versus == 0)
            {
                // 旧存档没有分轨计数：按“成对通关”反推（先计时赛后对决）
                int cleared = config.Rider.LicenseStep > GetStepsBeforeLevel(LicenseMaxLevel)
                    ? config.Rider.LicenseStep - GetStepsBeforeLevel(LicenseMaxLevel)
                    : 0;
                speed = (byte)Math.Min(ProPairCount, (cleared + 1) / 2);
                versus = (byte)Math.Min(ProPairCount, cleared / 2);
            }

            byte value = speedTrack ? speed : versus;
            return value > ProPairCount ? (byte)ProPairCount : value;
        }

        /// <summary>
        /// PRO 面板：客户端把第 2/4 字节当成**已经通过的关数**（0~6），
        /// 计时赛关卡 = 31 + 2*已通过数、对决关卡 = 32 + 2*已通过数；
        /// 这里发绝对关卡编号（31/32）会让整块 PRO 变成灰色不可点。
        /// </summary>
        /// <summary>
        /// PRO 关卡按月轮换：客户端只认“当前周期”的那一对（计时赛 / 对决用同一个下标）。
        /// 与旧版一致：周期 = 从 1900 年起的奇数月计数（单数月 1 号轮换，正好对应面板上
        /// “单数月的每月1号任务会进行重置，可更新驾照”这句提示）。
        /// </summary>
        public static int GetProPeriodIndex()
        {
            if (evenProStep.Count == 0)
            {
                return 0;
            }
            int period = RouterListener.DataTime();
            int index = (period - 1) % evenProStep.Count;
            return index < 0 ? 0 : index;
        }

        public static void GetProPeriodPair(out byte speedStage, out byte versusStage)
        {
            int index = GetProPeriodIndex();
            speedStage = index < evenProStep.Count ? evenProStep[index] : (byte)0;
            versusStage = index < oddProStep.Count ? oddProStep[index] : (byte)0;
        }

        /// <summary>PRO 徽章成绩取的是标准模式（速度档 7）+ 个人赛（0）的成绩。</summary>
        public const byte ProRecordSpeedType = 7;
        public const byte ProRecordGameType = 0;

        /// <summary>
        /// PRO 徽章第 index 条任务的玩家最好成绩（毫秒）：
        /// 按赛道 hash 去 TrackRank 里读「标准模式 / 个人赛」这一份成绩，没有则返回 0。
        /// </summary>
        public static int GetProEmblemRecord(string nickname, int index)
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
            return (int)TrackRankData.GetBestTime(hash, ProRecordSpeedType, ProRecordGameType, nickname);
        }

        /// <summary>
        /// PRO 徽章的前置条件：3 条赛道成绩都要有记录且不慢于时限（record > 0 && record <= limit）。
        /// </summary>
        public static bool CheckProEmblemPrerequisites(string nickname, out string detail)
        {
            var parts = new List<string>();
            bool ok = ProMissions.Count > 0;
            for (int i = 0; i < ProMissions.Count; i++)
            {
                ProEmblemMission mission = ProMissions[i];
                int record = GetProEmblemRecord(nickname, i);
                int limit = mission?.TimeLimit ?? 0;
                bool passed = record > 0 && (limit <= 0 || record <= limit);
                ok &= passed;
                parts.Add($"{mission?.Track}={record}/{(limit > 0 ? limit.ToString() : "-")}{(passed ? "✓" : "✗")}");
            }
            detail = parts.Count == 0 ? "未读取到 proCategory 配置" : string.Join(" ", parts);
            return ok;
        }

        /// <summary>
        /// 当前应该在 PRO 面板上展示的那一对关卡（绝对编号，如 31/32）。
        /// 未拿到 PRO 驾照时按“已通关整对”推进；已拿到 PRO 驾照时沿用旧版按月轮换。
        /// </summary>
        public static void GetCurrentProPair(SessionGroup session, out byte speedStage, out byte versusStage)
        {
            GetProgress(session, out byte level, out byte step);
            bool ownsPro = level >= LicenseMaxLevel && step >= LicenseTotalSteps;
            if (ownsPro)
            {
                GetProPeriodPair(out speedStage, out versusStage);
                return;
            }
            byte speedCleared = GetProSpeedCleared(session);
            byte versusCleared = GetProVersusCleared(session);
            byte pairCleared = speedCleared < versusCleared ? speedCleared : versusCleared;
            // 第 2/4 字节是**绝对关卡编号**（31/32、33/34 …）：发 1 会被客户端当成新手第 1 关
            speedStage = (byte)Math.Min(31 + pairCleared * 2, 41);
            versusStage = (byte)Math.Min(32 + pairCleared * 2, 42);
        }

        /// <summary>
        /// PRO 面板（P5136 客户端实测结构，两套编码）：
        ///   未拿到 PRO 驾照：u8 徽章、u8 计时赛关卡、u8 当前驾照等级、u8 最后通关关卡、i32×3 纪录
        ///   已拿到 PRO 驾照：u8 1、u8 计时赛关卡、u8 6、u8 对决关卡（旧版硬编码的那一套）
        /// 关卡来自 PRO 的成对关卡表 [31,32] [33,34] … [41,42]，未拿 PRO 时按已通关对数推进。
        /// </summary>
        public static void PrRiderSchoolPro(SessionGroup Parent)
        {
            GetProgress(Parent, out byte level, out byte step);
            byte speedCleared = GetProSpeedCleared(Parent);
            byte versusCleared = GetProVersusCleared(Parent);
            bool emblemEarned = ProfileService.GetProfileConfig(Parent.Client.Nickname)?.Rider?.ProEmblemEarned == true;
            bool ownsPro = level >= LicenseMaxLevel && step >= LicenseTotalSteps;

            // 客户端只认“当期轮换”的那一对关卡，非当期的卡片一律置灰：
            //   拿到徽章（或已拥有 PRO 驾照）后 → 发当期那一对（本月 = 41/42）
            //   还没拿到徽章（资格审核阶段）→ 按已通关对数推进，仅用于展示
            byte speedStage;
            byte versusStage;
            if (emblemEarned || ownsPro)
            {
                GetProPeriodPair(out speedStage, out versusStage);
            }
            else
            {
                byte pairCleared = speedCleared < versusCleared ? speedCleared : versusCleared;
                speedStage = (byte)Math.Min(31 + pairCleared * 2, 41);
                versusStage = (byte)Math.Min(32 + pairCleared * 2, 42);
            }

            var records = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                records.Add(GetProEmblemRecord(Parent.Client.Nickname, i));
            }

            using (OutPacket oPacket = new OutPacket("PrRiderSchoolProPacket"))
            {
                // u8 勋章是否已获得、u8 计时赛关卡、u8 驾照等级(PRO=6)、u8 对决关卡、i32×3 前置成绩
                oPacket.WriteByte(emblemEarned ? (byte)1 : (byte)0);
                oPacket.WriteByte(speedStage);
                oPacket.WriteByte(LicenseMaxLevel);
                oPacket.WriteByte(versusStage);
                for (int i = 0; i < 3; i++)
                {
                    oPacket.WriteInt(records[i]);
                }
                Log.Info($"[RiderSchool] {Parent.Client.Nickname} PRO 包字节={BitConverter.ToString(oPacket.ToArray()).Replace('-', ' ')}");
                Parent.Client.Send(oPacket);
            }
            Log.Info($"[RiderSchool] {Parent.Client.Nickname} 下发 PRO 数据: 勋章{emblemEarned} 关卡 {speedStage}/{versusStage} "
                     + $"等级{LicenseMaxLevel}(存档 {level}) 最后通关 {step}，"
                     + $"已通关 计时赛{speedCleared}/6 对决{versusCleared}/6，成绩={string.Join(",", records)}");
        }
    }
}
