using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Backends.HexGrid.Tools
{
    public enum HexDirection { NE, E, SE, SW, W, NW }

    public static class HexDirectionExtensions
    {
        public static HexDirection Opposite(this HexDirection aDir)
        {
            return (int)aDir < 3 ? (aDir + 3 ) : (aDir - 3);
        }

        public static HexDirection Previous(this HexDirection aDir)
        {
            return aDir == HexDirection.NE ? HexDirection.NW : (aDir - 1);
        }

        public static HexDirection Previous2(this HexDirection aDir)
        {
            aDir -= 2;
            return aDir >= HexDirection.NE ? aDir : (aDir + 6);
        }

        public static HexDirection Next(this HexDirection aDir)
        {
            return aDir == HexDirection.NW ? HexDirection.NE : (aDir + 1);
        }

        public static HexDirection Next2(this HexDirection aDir)
        {
            aDir += 2;
            return aDir <= HexDirection.NW ? aDir : (aDir - 6);
        }
    }
}
