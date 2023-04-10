using System.Collections.Generic;

namespace Assets.Scripts.Backends.Tools
{
    public class HexCellPriorityQueue
    {
        List<HexCell> list = new List<HexCell>();

        public int Count { get { return count; } }

        private int count = 0;

        public void Enqueue(HexCell cell)
        {
            count += 1;
        }

        public HexCell Dequeue()
        {
            count -= 1;
            return null;
        }

        public void Change(HexCell cell, int oldPriority)
        { }

        public void Clear()
        {
            list.Clear();
            count = 0;
        }
    }
}
