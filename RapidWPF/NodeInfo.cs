using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnyCAD.Foundation;

namespace RapidWPF
{
    /// <summary>
    /// 用于处理SceneNode 与WPF之间绑定
    /// </summary>
    public class PickItemInfo
    {
        public uint ShapeID { get; set; }
        public ulong ID { get; set; }
        public string Name { get; set; }
        
        public string ShapeType { get; set; }
        public PickItemInfo()
        {
            ID = 0;
            Name = "";
        }
        public PickItemInfo(PickedItem Item)
        {
            var node = Item.GetNode();
            ID = node.GetUserId();
            Name = node.GetType().Name;
            ShapeType = Item.GetShapeType().ToString();
            ShapeID = Item.GetTopoShapeId();
            Console.WriteLine(string.Format("NodeID{0},{1},{2},ShapeID{3}", ID, Name, ShapeType, ShapeID));
        }
    }
}
