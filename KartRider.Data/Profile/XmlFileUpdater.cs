using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace XmlFileUpdater
{
    class XmlUpdater
    {
        /// <summary>
        /// 比较本地XML文件和资源中的XML文件，将资源中独有的内容添加到本地文件
        /// </summary>
        /// <param name="localFilePath">本地XML文件路径</param>
        /// <param name="resourceName">资源中XML文件的名称（需包含命名空间前缀）</param>
        public void UpdateLocalXmlWithResource(string localFilePath, string resourceName)
        {
            try
            {
                // 验证本地文件是否存在
                if (!File.Exists(localFilePath))
                {
                    Console.WriteLine($"本地文件不存在: {localFilePath}");
                    return;
                }

                // 加载本地XML文件
                XDocument localXml = XDocument.Load(localFilePath);

                // 从程序集资源加载XML（修复资源加载逻辑）
                XDocument resourceXml = XDocument.Parse(resourceName);
                if (resourceXml == null)
                {
                    Console.WriteLine($"无法加载资源文件: {resourceName}");
                    return;
                }

                // 验证XML根节点
                if (localXml.Root == null || resourceXml.Root == null)
                {
                    Console.WriteLine("XML文件结构不完整，缺少根节点");
                    return;
                }

                // 比较并合并XML内容（使用ID属性作为唯一标识，适配Kart节点）
                bool isUpdated = MergeXml(localXml.Root, resourceXml.Root, "ID");

                // 如果有更新，保存本地文件
                if (isUpdated)
                {
                    // 保存时保留缩进格式
                    localXml.Save(localFilePath, SaveOptions.None);
                    Console.WriteLine("ModelMax.xml文件已更新");
                }
                else
                {
                    Console.WriteLine("ModelMax.xml文件已是最新，无需更新");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理ModelMax.xml文件时出错: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 将资源XML中的内容合并到本地XML中
        /// </summary>
        /// <param name="uniqueAttribute">用于判断元素唯一性的属性名（如"ID"）</param>
        /// <returns>如果有内容被添加则返回true，否则返回false</returns>
        private bool MergeXml(XElement localRoot, XElement resourceRoot, string uniqueAttribute)
        {
            bool isUpdated = false;

            // 比较并添加子元素
            foreach (var resourceElement in resourceRoot.Elements())
            {
                // 检查本地是否已存在相同元素（使用唯一属性判断）
                bool exists = CheckElementExists(localRoot, resourceElement, uniqueAttribute);

                if (!exists)
                {
                    // 复制资源元素到本地XML（包含所有属性和子节点）
                    localRoot.Add(new XElement(resourceElement));
                    isUpdated = true;
                    string elementInfo = GetElementIdentifier(resourceElement, uniqueAttribute);
                    Console.WriteLine($"已添加元素: {elementInfo}");
                }
                else
                {
                    // 如果元素存在，递归检查其子元素
                    var localElement = FindCorrespondingElement(localRoot, resourceElement, uniqueAttribute);
                    if (localElement != null)
                    {
                        bool childUpdated = MergeXml(localElement, resourceElement, uniqueAttribute);
                        isUpdated = isUpdated || childUpdated;
                    }
                }
            }

            return isUpdated;
        }

        /// <summary>
        /// 检查本地XML中是否存在与资源XML中相同的元素
        /// </summary>
        private bool CheckElementExists(XElement parent, XElement elementToCheck, string uniqueAttribute)
        {
            return FindCorrespondingElement(parent, elementToCheck, uniqueAttribute) != null;
        }

        /// <summary>
        /// 查找本地XML中与资源XML中相对应的元素
        /// </summary>
        private XElement FindCorrespondingElement(XElement parent, XElement elementToFind, string uniqueAttribute)
        {
            // 1. 先通过元素名过滤候选元素
            var candidates = parent.Elements(elementToFind.Name);
            if (!candidates.Any())
                return null;

            // 2. 使用唯一属性（如ID）精确匹配（适配Kart节点的ID属性）
            XAttribute uniqueAttr = elementToFind.Attribute(uniqueAttribute);
            if (uniqueAttr != null)
            {
                string targetValue = uniqueAttr.Value;
                return candidates.FirstOrDefault(e =>
                    e.Attribute(uniqueAttribute)?.Value.Equals(targetValue, StringComparison.OrdinalIgnoreCase) == true);
            }

            // 3. 没有唯一属性时，使用所有属性组合匹配
            foreach (var candidate in candidates)
            {
                if (AreAttributesEqual(candidate, elementToFind))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// 比较两个元素的所有属性是否相等
        /// </summary>
        private bool AreAttributesEqual(XElement a, XElement b)
        {
            // 属性数量不同直接返回false
            if (a.Attributes().Count() != b.Attributes().Count())
                return false;

            // 比较每个属性的名称和值
            foreach (var attr in a.Attributes())
            {
                var correspondingAttr = b.Attribute(attr.Name);
                if (correspondingAttr == null || !correspondingAttr.Value.Equals(attr.Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取元素的标识信息（用于日志输出）
        /// </summary>
        private string GetElementIdentifier(XElement element, string uniqueAttribute)
        {
            var idAttr = element.Attribute(uniqueAttribute);
            if (idAttr != null)
            {
                return $"{element.Name} (ID: {idAttr.Value})";
            }
            return element.Name.ToString();
        }
    }
}
