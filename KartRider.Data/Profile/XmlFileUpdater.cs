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
        /// <param name="resourceName">资源中XML文件的名称</param>
        public void UpdateLocalXmlWithResource(string localFilePath, string resourceName)
        {
            try
            {
                // 加载本地XML文件
                XDocument localXml = XDocument.Load(localFilePath);
                
                // 加载资源中的XML文件
                XDocument resourceXml = XDocument.Parse(resourceName);
                
                if (localXml == null || resourceXml == null || 
                    localXml.Root == null || resourceXml.Root == null)
                {
                    Console.WriteLine("无法加载ModelMax.xml文件或ModelMax.xml结构不完整");
                    return;
                }
                
                // 比较并合并XML内容
                bool isUpdated = MergeXml(localXml.Root, resourceXml.Root);
                
                // 如果有更新，保存本地文件
                if (isUpdated)
                {
                    localXml.Save(localFilePath);
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
            }
        }

        /// <summary>
        /// 将资源XML中的内容合并到本地XML中
        /// </summary>
        /// <returns>如果有内容被添加则返回true，否则返回false</returns>
        private bool MergeXml(XElement localRoot, XElement resourceRoot)
        {
            bool isUpdated = false;
            
            // 比较并添加子元素
            foreach (var resourceElement in resourceRoot.Elements())
            {
                // 这里使用元素名和关键属性作为唯一标识，可根据实际情况修改
                bool exists = CheckElementExists(localRoot, resourceElement);
                
                if (!exists)
                {
                    // 复制资源元素到本地XML
                    localRoot.Add(new XElement(resourceElement));
                    isUpdated = true;
                    Console.WriteLine($"已添加车辆: {resourceElement.Name}");
                }
                else
                {
                    // 如果元素存在，递归检查其子元素
                    var localElement = FindCorrespondingElement(localRoot, resourceElement);
                    if (localElement != null)
                    {
                        bool childUpdated = MergeXml(localElement, resourceElement);
                        isUpdated = isUpdated || childUpdated;
                    }
                }
            }
            
            return isUpdated;
        }

        /// <summary>
        /// 检查本地XML中是否存在与资源XML中相同的元素
        /// </summary>
        private bool CheckElementExists(XElement parent, XElement elementToCheck)
        {
            return FindCorrespondingElement(parent, elementToCheck) != null;
        }

        /// <summary>
        /// 查找本地XML中与资源XML中相对应的元素
        /// 这里的匹配逻辑可以根据实际XML结构进行修改
        /// </summary>
        private XElement FindCorrespondingElement(XElement parent, XElement elementToFind)
        {
            // 简单匹配：元素名相同，且具有相同的ID属性（如果存在）
            var candidates = parent.Elements(elementToFind.Name);
            
            // 如果有ID属性，使用ID进行匹配
            if (elementToFind.Attribute("id") != null)
            {
                string idValue = elementToFind.Attribute("id").Value;
                return candidates.FirstOrDefault(e => e.Attribute("id")?.Value == idValue);
            }
            
            // 如果没有ID属性，仅通过元素名匹配（可能不够精确，根据实际情况调整）
            return candidates.FirstOrDefault();
        }
    }
}
