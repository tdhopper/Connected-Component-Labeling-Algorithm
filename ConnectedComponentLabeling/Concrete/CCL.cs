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
        private int _width;
        private int _height;


        #endregion

        #region IConnectedComponentLabeling

        public Dictionary<int, HashSet<Point>> Process(IRaster input)
        {
            _input = input;
            
            _width = input.NumColumns;
            _height = input.NumRows;
            _board = new int[_width, _height];

            Dictionary<int, HashSet<Point>> patterns = Find(_width, _height);
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

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    Point currentPoint = new Point(j, i);

                    if (!CheckIsForeGround(currentPoint))
                    {
                        continue;
                    }

					HashSet<int> neighboringLabels = GetNeighboringLabels(currentPoint);		
                    int currentLabel;

                    if (!neighboringLabels.Any())
                    {
                        currentLabel = labelCount;
                        allLabels.Add(currentLabel, new Label(currentLabel));
                        labelCount++;
                    }
                    else
                    {
                        currentLabel = neighboringLabels.Min(n => allLabels[n].GetRoot().Name);
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

                    _board[j, i] = currentLabel;
                }
            }


            Dictionary<int, HashSet<Point>> patterns = AggregatePatterns(allLabels);

            return patterns;
        }

        private HashSet<int> GetNeighboringLabels(Point pix)
        {
            var neighboringLabels = new HashSet<int>();

            for (int i = pix.Y - 1; i <= pix.Y + 2 && i < _height - 1; i++)
            {
                for (int j = pix.X - 1; j <= pix.X + 2 && j < _width - 1; j++)
                {
                    if (i > -1 && j > -1 && _board[j, i] != 0)
                    {
                        neighboringLabels.Add(_board[j, i]);
                    }
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


        private Dictionary<int, HashSet<Point>> AggregatePatterns(Dictionary<int, Label> allLabels)
        {
            var patterns = new Dictionary<int, HashSet<Point>>();

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    int patternNumber = _board[j, i];

                    if (patternNumber != 0)
                    {
                        patternNumber = allLabels[patternNumber].GetRoot().Name;

                        if (!patterns.ContainsKey(patternNumber))
                        {
                            patterns[patternNumber] = new HashSet<Point>();
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
