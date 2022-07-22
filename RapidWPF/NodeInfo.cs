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
        private ulong userID;

        public ulong UserID
        {
            get => userID; set
            {
                userID = value;
                IDSTR = string.Format("ID{0}", value);
            }
        }
        public uint ShapeID { get; set; }
        public ulong NodeID { get; set; }
        public string Name { get; set; }

        public string IDSTR { get; set; }
        public string ShapeType { get; set; }
        public PickItemInfo()
        {
            IDSTR = "";
            ShapeType = "";
        }
        public PickItemInfo(PickedItem Item)
        {
            var node = Item.GetNode();
            NodeID = Item.GetNodeId();
            UserID = node.GetUserId();
            Name = node.GetType().Name;
            ShapeType = Item.GetShapeType().ToString();
            ShapeID = Item.GetTopoShapeId();
            Console.WriteLine(string.Format("NodeID{0},{1},{2},ShapeID{3}", NodeID, Name, ShapeType, ShapeID));
        }
    }
}
