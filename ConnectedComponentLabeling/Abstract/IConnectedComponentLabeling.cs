using DotSpatial.Data;
using System.Collections.Generic;
using System.Drawing;

namespace ConnectedComponentLabeling
{
    public interface IConnectedComponentLabeling
    {
        Dictionary<int, HashSet<Point>> Process(IRaster input);
    }
}