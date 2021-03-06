﻿
//BSD, 2014-2017, WinterDev
//MattersHackers
//AGG 2.4

using PixelFarm.Drawing;
using PixelFarm.Agg.VertexSource;
using PixelFarm.VectorMath;
namespace PixelFarm.Agg
{
    public class SpriteShape
    {
        PathWriter path = new PathWriter();
        Color[] colors = new Color[100];
        int[] pathIndexList = new int[100];
        int numPaths = 0;
        RectD boundingRect;
        Vector2 center;
        public SpriteShape()
        {
        }

        public PathWriter Path
        {
            get
            {
                return path;
            }
        }

        public int NumPaths
        {
            get
            {
                return numPaths;
            }
        }

        public RectD Bounds
        {
            get
            {
                return boundingRect;
            }
        }

        public Color[] Colors
        {
            get
            {
                return colors;
            }
        }

        public int[] PathIndexList
        {
            get
            {
                return pathIndexList;
            }
        }

        public Vector2 Center
        {
            get
            {
                return center;
            }
        }

        public void ParseLion()
        {
            numPaths = PixelFarm.Agg.LionDataStore.LoadLionData(path, colors, pathIndexList);
            PixelFarm.Agg.BoundingRect.GetBoundingRect(path.Vxs, pathIndexList, numPaths, out boundingRect);
            center.x = (boundingRect.Right - boundingRect.Left) / 2.0;
            center.y = (boundingRect.Top - boundingRect.Bottom) / 2.0;
        }
        public static void UnsafeDirectSetData(SpriteShape lion,
            int numPaths,
            PathWriter pathStore,
            Color[] colors,
            int[] pathIndice)
        {
            lion.path = pathStore;
            lion.colors = colors;
            lion.pathIndexList = pathIndice;
            lion.numPaths = numPaths;
            lion.UpdateBoundingRect();
        }
        void UpdateBoundingRect()
        {
            PixelFarm.Agg.BoundingRect.GetBoundingRect(path.Vxs, pathIndexList, numPaths, out boundingRect);
            center.x = (boundingRect.Right - boundingRect.Left) / 2.0;
            center.y = (boundingRect.Top - boundingRect.Bottom) / 2.0;
        }
    }
}