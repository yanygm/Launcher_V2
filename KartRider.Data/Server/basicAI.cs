using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Profile;

namespace KartRider
{
    // 配件类（气球/头带/护目镜）
    public class AIAccessory
    {
        public short Id { get; set; }
        public int Speed { get; set; }
        public int Item { get; set; }
    }

    // 角色类
    public class AICharacter
    {
        public short Id { get; set; }              // 角色ID
        public List<string> Rids { get; set; }     // rid名称列表（按XML顺序）
        public List<AIAccessory> Balloons { get; set; }
        public List<AIAccessory> Headbands { get; set; }
        public List<AIAccessory> Goggles { get; set; }

        public AICharacter()
        {
            Rids = new List<string>();
            Balloons = new List<AIAccessory>();
            Headbands = new List<AIAccessory>();
            Goggles = new List<AIAccessory>();
        }
    }

    // 卡丁车类
    public class AIKart
    {
        public short Id { get; set; }              // 卡丁车ID（short）
        public int Speed { get; set; }
        public int Item { get; set; }
    }

    public class DictionaryRandomSelector
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// 从角色Dictionary中随机获取不重复的ID（short类型）
        /// </summary>
        /// <param name="characterDict">角色Dictionary</param>
        /// <param name="count">需要的数量</param>
        /// <returns>不重复的角色ID列表（short）</returns>
        public List<short> GetRandomCharacterIds(Dictionary<short, AICharacter> characterDict, int count)
        {
            if (characterDict == null || characterDict.Count == 0 || count <= 0)
                return new List<short>();

            count = Math.Min(count, characterDict.Count);
            // 从键集合中随机选择不重复的ID
            return characterDict.Keys.OrderBy(_ => _random.Next()).Take(count).ToList();
        }

        /// <summary>
        /// 从指定角色中随机获取rid（按XML顺序索引）
        /// </summary>
        public short? GetRandomRidIndex(AICharacter character)
        {
            if (character?.Rids == null || character.Rids.Count == 0)
                return null;
            return (short)_random.Next(character.Rids.Count);
        }

        /// <summary>
        /// 从指定角色中随机获取配件ID（气球/头带/护目镜）
        /// </summary>
        public short? GetRandomAccessoryId(List<AIAccessory> accessories)
        {
            if (accessories == null || accessories.Count == 0)
                return null;
            return accessories[_random.Next(accessories.Count)].Id;
        }

        /// <summary>
        /// 从卡丁车Dictionary中随机获取不重复的ID（筛选speed≠0或item≠0）
        /// </summary>
        /// <param name="kartDict">卡丁车Dictionary</param>
        /// <param name="count">需要的数量</param>
        /// <returns>不重复的卡丁车ID列表（short）</returns>
        public List<short> GetRandomKartIds(Dictionary<short, AIKart> kartDict, int count,
            bool requireSpeed = false, bool requireItem = false)
        {
            if (kartDict == null || kartDict.Count == 0 || count <= 0)
                return new List<short>();

            // 根据参数筛选符合条件的卡丁车ID
            IEnumerable<short> validIds = kartDict.Values
                .Where(k =>
                    (!requireSpeed || k.Speed != 0) &&  // 如果要求Speed≠0，则筛选；否则不限制
                    (!requireItem || k.Item != 0)      // 如果要求Item≠0，则筛选；否则不限制
                )
                .Select(k => k.Id);

            // 转换为列表
            var validIdList = validIds.ToList();

            if (validIdList.Count == 0)
                return new List<short>();

            // 限制数量不超过有效ID总数
            count = Math.Min(count, validIdList.Count);

            // 随机排序并取前count个
            return validIdList.OrderBy(_ => _random.Next()).Take(count).ToList();
        }
    }

    public class AI
    {
        public static List<float> GetAISpec(byte GameType)
        {
            Random random = new Random();
            var e = 1000;
            var f = 1500;
            if (ProfileService.SettingConfig.AiSpeedType == "简单")
            {
                var b = (float)random.Next(2300, 2600);
                var c = (float)random.Next(2900, 3000);
                var d = new[] { 1.4f, 1.5f, 1.6f }[random.Next(3)];
                if (GameType == 0)
                {
                    var a = new[] { 0.5f, 0.6f, 0.7f }[random.Next(3)];
                    return new List<float>() { a, b, c, d, e, f };
                }
                else if (GameType == 1)
                {
                    var a = new[] { 0.4f, 0.5f, 0.6f }[random.Next(3)];
                    return new List<float>() { a, b, c, d, e, f };
                }
            }
            else if (ProfileService.SettingConfig.AiSpeedType == "困难")
            {
                var b = (float)random.Next(2500, 2800);
                var c = (float)random.Next(3100, 3200);
                var d = new[] { 1.6f, 1.7f, 1.8f }[random.Next(3)];
                if (GameType == 0)
                {
                    var a = new[] { 0.7f, 0.8f, 0.9f }[random.Next(3)];
                    return new List<float>() { a, b, c, d, e, f };
                }
                else if (GameType == 1)
                {
                    var a = new[] { 0.5f, 0.6f, 0.7f }[random.Next(3)];
                    return new List<float>() { a, b, c, d, e, f };
                }
            }
            else if (ProfileService.SettingConfig.AiSpeedType == "地狱")
            {
                var b = (float)random.Next(2700, 3000);
                var c = (float)random.Next(3300, 3500);
                var d = new[] { 1.8f, 1.9f, 2.0f }[random.Next(3)];
                if (GameType == 0)
                {
                    var a = new[] { 0.9f, 1.0f, 1.1f }[random.Next(3)];
                    return new List<float>() { a, b, c, d, e, f };
                }
                else if (GameType == 1)
                {
                    var a = new[] { 0.6f, 0.7f, 0.8f }[random.Next(3)];
                    return new List<float>() { a, b, c, d, e, f };
                }
            }
            Console.WriteLine(ProfileService.SettingConfig.AiSpeedType);
            return null;
        }
    }
}
