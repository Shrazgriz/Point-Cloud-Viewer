using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnyCAD.Platform;

namespace WpfCloud
{
    /// <summary>
    /// 用于处理SceneNode 与WPF之间绑定
    /// </summary>
    public class NodeInfo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public NodeInfo()
        {
            ID = 0;
            Name = "";
        }
        public NodeInfo(SceneNode Node)
        {
            ID = Node.GetId().AsInt();
            Name = Node.ToString();
        }
    }
}
