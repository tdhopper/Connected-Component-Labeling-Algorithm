using System.Collections.Generic;
using System.Drawing;
using System.ComponentModel.Composition;
using System.Linq;
using DotSpatial.Data;

namespace ConnectedComponentLabeling
{
    [Export(typeof(IConnectedComponentLabeling))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CCL : IConnectedComponentLabeling
    {

        public CCL()
        {
        }

        #region Member Variables

        private int[,] _board;
        private IRaster _input;
        private int _startX;
        private int _startY;
        private int _noDataValue;

        #endregion

        #region IConnectedComponentLabeling

        public Dictionary<int, HashSet<Point>> Process(IRaster input)
        {
            _input = input;
            
            int width = input.NumColumns;
            int height = input.NumRows;
            _board = new int[width, height];

            Dictionary<int, HashSet<Point>> patterns = Find(width, height);
            return patterns;
        }

        #endregion

        #region Protected Methods

        protected virtual bool CheckIsForeGround(Point currentPoint)
        {
            try
            {
                return _input.Value[currentPoint.Y, currentPoint.X].ToString() != _input.NoDataValue.ToString();
            }
            catch(System.Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            return true;
        }


        public static int CountConnectedComponents(IRaster raster)
        // Count the number of connected components in a raster.
        {
            return (new CCL()).Process(raster).Count;
        }

        #endregion

        #region Private Methods

        private Dictionary<int, HashSet<Point>> Find(int width, int height)
        {
            int labelCount = 1;
            var allLabels = new Dictionary<int, Label>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Point currentPoint = new Point(x, y);

                    if (CheckIsForeGround(currentPoint))
                    {
                        HashSet<int> neighboringLabels = GetNeighboringLabels(currentPoint, width, height);
                        int currentLabel;

                        if (neighboringLabels.Count == 0)
                        {
                            currentLabel = labelCount;
                            allLabels.Add(currentLabel, new Label(currentLabel));
                            labelCount++;
                        }
                        else
                        {
                            currentLabel = neighboringLabels.Min();
                            var root = allLabels[currentLabel].GetRoot();

                            foreach (var item in neighboringLabels)
                            {
                                var root2 = allLabels[item].GetRoot();

                                if (root.Name != root2.Name)
                                {
                                    allLabels[item].Join(allLabels[currentLabel]);
                                }
                            }
                        }

                        _board[x, y] = currentLabel;
                    }
                }
            }


            Dictionary<int, HashSet<Point>> patterns = AggregatePatterns(allLabels, width, height);

            return patterns;
        }

        private HashSet<int> GetNeighboringLabels(Point pix, int width, int height)
        {
            var neighboringLabels = new HashSet<int>();
            int x = pix.Y;
            int y = pix.X;

            if (x > 0)//North
            {
                CheckCorner(neighboringLabels, x - 1, y);

                if (y > 0)//North West
                {
                    CheckCorner(neighboringLabels, x - 1, y - 1);
                }

                if (y < width - 1)//North East
                {
                    CheckCorner(neighboringLabels, x - 1, y + 1);
                }
            }
            if (y > 0)//West
            {
                CheckCorner(neighboringLabels, x, y - 1);

                if (x < height - 1)//South West
                {
                    CheckCorner(neighboringLabels, x + 1, y - 1);
                }
            }
            if (y < width - 1)//East
            {
                CheckCorner(neighboringLabels, x, y + 1);

                if (x < height - 1)//South East
                {
                    CheckCorner(neighboringLabels, x + 1, y + 1);
                }
            }

            return neighboringLabels;
        }

        private void CheckCorner(HashSet<int> neighboringLabels, int i, int j)
        {
            if (_board[j, i] != 0 && !neighboringLabels.Contains(_board[j, i]))
            {
                neighboringLabels.Add(_board[j, i]);
            }
        }


        //private int GetDimension(HashSet<Point> shape, out int dimensionShift, bool isWidth)
        //{
        //    int result = dimensionShift = CheckDimensionType(shape[0], isWidth);

        //    for (int i = 1; i < shape.Count; i++)
        //    {
        //        int dimension = CheckDimensionType(shape[i], isWidth);

        //        if (result < dimension)
        //        {
        //            result = dimension;
        //        }

        //        if (dimensionShift > dimension)
        //        {
        //            dimensionShift = dimension;
        //        }
        //    }

        //    return (result + 1) - dimensionShift;
        //}

        private int CheckDimensionType(Pixel shape, bool isWidth)
        {
            return isWidth ? shape.Position.X : shape.Position.Y;
        }

        private Dictionary<int, HashSet<Point>> AggregatePatterns(Dictionary<int, Label> allLabels, int width, int height)
        {
            var patterns = new Dictionary<int, HashSet<Point>>();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int patternNumber = _board[j, i];

                    if (patternNumber != 0)
                    {
                        patternNumber = allLabels[patternNumber].GetRoot().Name;

                        if (!patterns.ContainsKey(patternNumber))
                        {
                            patterns.Add(patternNumber, new HashSet<Point>());
                        }

                        patterns[patternNumber].Add(new Point(j, i));
                    }
                }
            }

            return patterns;
        }

        #endregion
    }
}
