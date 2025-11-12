namespace Launcher.App.Server
{
    // 配件类（气球/头带/护目镜）
    public class AIAccessory
    {
        public short Id { get; set; }      // ID改为short
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
}