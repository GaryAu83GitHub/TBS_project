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
    }
}
