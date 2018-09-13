using System;
using System.Collections.Generic;

namespace SpriteAnimEdit.Editor.Atlas
{

    /*MaxRectsBinPack实现了MAXRECTS数据结构和使用这种结构的不同的装箱算法*/
    class MaxRectsBinPack
	{
		bool allowRotation = false;
        
		public MaxRectsBinPack()
		{
		}
        
		public MaxRectsBinPack(int width, int height, bool allowRotation)
		{
			Init(width, height, allowRotation);
		}

        //需要重新调用时，初始化一个宽度x高度单位的空容器
        private void Init(int width, int height, bool allowRotation)
		{
			binWidth = width;
			binHeight = height;
			this.allowRotation = allowRotation;

			Rect n = new Rect();
			n.x = 0;
			n.y = 0;
			n.width = width;
			n.height = height;

			usedRectangles.Clear();

			freeRectangles.Clear();
			freeRectangles.Add(n);
		}

        //指定在何处放置新矩形时可以使用的不同触发规则。
        public enum FreeRectChoiceHeuristic
		{
			RectBestShortSideFit,   //矩形短边
			RectBestLongSideFit,    //矩形长边
			RectBestAreaFit,        //最小矩形区域
			RectBottomLeftRule,     //插入某两之间
			RectContactPointRule    //与相似紧挨一起
		};

        // 插入
        // rects：指定插入表，会操作这个表的引用
        public bool Insert(List<RectSize> rects, FreeRectChoiceHeuristic method)
		{
			int numRects = rects.Count;
			while (rects.Count > 0)
			{
				int bestScore1 = Int32.MaxValue;
				int bestScore2 = Int32.MaxValue;
				int bestRectIndex = -1;
				Rect bestNode = null;

				for (int i = 0; i < rects.Count; ++i)
				{
					int score1 = 0;
					int score2 = 0;
					Rect newNode = ScoreRect(rects[i].width, rects[i].height, method, ref score1, ref score2);

					if (score1 < bestScore1 || (score1 == bestScore1 && score2 < bestScore2))
					{
						bestScore1 = score1;
						bestScore2 = score2;
						bestNode = newNode;
						bestRectIndex = i;
					}
				}

				if (bestRectIndex == -1)
					return usedRectangles.Count == numRects;

				PlaceRect(bestNode);
				rects.RemoveAt(bestRectIndex);
			}

			return usedRectangles.Count == numRects;
		}

		public List<Rect> GetMapped()
		{
			return usedRectangles;
		}


		//在rect中插入一个矩形
		public Rect Insert(int width, int height, FreeRectChoiceHeuristic method)
		{
			Rect newNode = new Rect();
			int score1 = 0;
			int score2 = 0;
			switch (method)
			{
				case FreeRectChoiceHeuristic.RectBestShortSideFit: newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2); break;
				case FreeRectChoiceHeuristic.RectBottomLeftRule: newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2); break;
				case FreeRectChoiceHeuristic.RectContactPointRule: newNode = FindPositionForNewNodeContactPoint(width, height, ref score1); break;
				case FreeRectChoiceHeuristic.RectBestLongSideFit: newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1); break;
				case FreeRectChoiceHeuristic.RectBestAreaFit: newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2); break;
			}

			if (newNode.height == 0)
				return newNode;

			int numRectanglesToProcess = freeRectangles.Count;
			for (int i = 0; i < numRectanglesToProcess; ++i)
			{
				if (SplitFreeNode(freeRectangles[i], newNode))
				{
					freeRectangles.RemoveAt(i);
					--i;
					--numRectanglesToProcess;
				}
			}

			PruneFreeList();

			usedRectangles.Add(newNode);
			return newNode;
		}
        
        //计算使用的面积
		public float Occupancy()
		{
			long usedSurfaceArea = 0;
			for (int i = 0; i < usedRectangles.Count; ++i)
				usedSurfaceArea += usedRectangles[i].width * usedRectangles[i].height;

			return (float)usedSurfaceArea / (float)(binWidth * binHeight);
		}

		public int WastedBinArea()
		{
			long usedSurfaceArea = 0;
			for (int i = 0; i < usedRectangles.Count; ++i)
				usedSurfaceArea += usedRectangles[i].width * usedRectangles[i].height;

			return (int)((long)(binWidth * binHeight) - usedSurfaceArea);
		}


		int binWidth = 0;
		int binHeight = 0;

		List<Rect> usedRectangles = new List<Rect>();
		List<Rect> freeRectangles = new List<Rect>();

        // 计算放置给定矩形的位置得分
        Rect ScoreRect(int width, int height, FreeRectChoiceHeuristic method, ref int score1, ref int score2)
		{
			Rect newNode = null;
			score1 = Int32.MaxValue;
			score2 = Int32.MaxValue;
			switch (method)
			{
				case FreeRectChoiceHeuristic.RectBestShortSideFit: newNode = FindPositionForNewNodeBestShortSideFit(width, height, ref score1, ref score2); break;
				case FreeRectChoiceHeuristic.RectBottomLeftRule: newNode = FindPositionForNewNodeBottomLeft(width, height, ref score1, ref score2); break;
				case FreeRectChoiceHeuristic.RectContactPointRule: newNode = FindPositionForNewNodeContactPoint(width, height, ref score1);
					score1 = -score1; //反转得到最小的值，但接触点得分越大越好
                    break;
				case FreeRectChoiceHeuristic.RectBestLongSideFit: newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1); break;
				case FreeRectChoiceHeuristic.RectBestAreaFit: newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2); break;
			}

            //无法匹配当前矩形 
            if (newNode.height == 0)
			{
				score1 = Int32.MaxValue;
				score2 = Int32.MaxValue;
			}

			return newNode;
		}

		/// Places the given rectangle into the bin.
		void PlaceRect(Rect node)
		{
			int numRectanglesToProcess = freeRectangles.Count;
			for (int i = 0; i < numRectanglesToProcess; ++i)
			{
				if (SplitFreeNode(freeRectangles[i], node))
				{
					freeRectangles.RemoveAt(i);
					--i;
					--numRectanglesToProcess;
				}
			}

			PruneFreeList();

			usedRectangles.Add(node);
		}

        //计算变体的位置。
        int ContactPointScoreNode(int x, int y, int width, int height)
		{
			int score = 0;

			if (x == 0 || x + width == binWidth)
				score += height;
			if (y == 0 || y + height == binHeight)
				score += width;

			for (int i = 0; i < usedRectangles.Count; ++i)
			{
				if (usedRectangles[i].x == x + width || usedRectangles[i].x + usedRectangles[i].width == x)
					score += CommonIntervalLength(usedRectangles[i].y, usedRectangles[i].y + usedRectangles[i].height, y, y + height);
				if (usedRectangles[i].y == y + height || usedRectangles[i].y + usedRectangles[i].height == y)
					score += CommonIntervalLength(usedRectangles[i].x, usedRectangles[i].x + usedRectangles[i].width, x, x + width);
			}
			return score;
		}

		Rect FindPositionForNewNodeBottomLeft(int width, int height, ref int bestY, ref int bestX)
		{
			Rect bestNode = new Rect();

			bestY = Int32.MaxValue;

			for (int i = 0; i < freeRectangles.Count; ++i)
			{
                //试着将矩形置于垂直(非翻转)方向。
                if (freeRectangles[i].width >= width && freeRectangles[i].height >= height)
				{
					int topSideY = freeRectangles[i].y + height;
					if (topSideY < bestY || (topSideY == bestY && freeRectangles[i].x < bestX))
					{
						bestNode.x = freeRectangles[i].x;
						bestNode.y = freeRectangles[i].y;
						bestNode.width = width;
						bestNode.height = height;
						bestY = topSideY;
						bestX = freeRectangles[i].x;
					}
				}
				if (allowRotation && freeRectangles[i].width >= height && freeRectangles[i].height >= width)
				{
					int topSideY = freeRectangles[i].y + width;
					if (topSideY < bestY || (topSideY == bestY && freeRectangles[i].x < bestX))
					{
						bestNode.x = freeRectangles[i].x;
						bestNode.y = freeRectangles[i].y;
						bestNode.width = height;
						bestNode.height = width;
						bestY = topSideY;
						bestX = freeRectangles[i].x;
					}
				}
			}
			return bestNode;
		}

		Rect FindPositionForNewNodeBestShortSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
		{
			Rect bestNode = new Rect();

			bestShortSideFit = Int32.MaxValue;

			for (int i = 0; i < freeRectangles.Count; ++i)
			{
                //试着将矩形置于垂直(非翻转)方向。 
                if (freeRectangles[i].width >= width && freeRectangles[i].height >= height)
				{
					int leftoverHoriz = Math.Abs(freeRectangles[i].width - width);
					int leftoverVert = Math.Abs(freeRectangles[i].height - height);
					int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
					int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

					if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
					{
						bestNode.x = freeRectangles[i].x;
						bestNode.y = freeRectangles[i].y;
						bestNode.width = width;
						bestNode.height = height;
						bestShortSideFit = shortSideFit;
						bestLongSideFit = longSideFit;
					}
				}

				if (allowRotation && freeRectangles[i].width >= height && freeRectangles[i].height >= width)
				{
					int flippedLeftoverHoriz = Math.Abs(freeRectangles[i].width - height);
					int flippedLeftoverVert = Math.Abs(freeRectangles[i].height - width);
					int flippedShortSideFit = Math.Min(flippedLeftoverHoriz, flippedLeftoverVert);
					int flippedLongSideFit = Math.Max(flippedLeftoverHoriz, flippedLeftoverVert);

					if (flippedShortSideFit < bestShortSideFit || (flippedShortSideFit == bestShortSideFit && flippedLongSideFit < bestLongSideFit))
					{
						bestNode.x = freeRectangles[i].x;
						bestNode.y = freeRectangles[i].y;
						bestNode.width = height;
						bestNode.height = width;
						bestShortSideFit = flippedShortSideFit;
						bestLongSideFit = flippedLongSideFit;
					}
				}
			}
			return bestNode;
		}

		Rect FindPositionForNewNodeBestLongSideFit(int width, int height, ref int bestShortSideFit, ref int bestLongSideFit)
		{
			Rect bestNode = new Rect();
			bestLongSideFit = Int32.MaxValue;

			for (int i = 0; i < freeRectangles.Count; ++i)
			{
				if (freeRectangles[i].width >= width && freeRectangles[i].height >= height)
				{
					int leftoverHoriz = Math.Abs(freeRectangles[i].width - width);
					int leftoverVert = Math.Abs(freeRectangles[i].height - height);
					int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
					int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

					if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
					{
						bestNode.x = freeRectangles[i].x;
						bestNode.y = freeRectangles[i].y;
						bestNode.width = width;
						bestNode.height = height;
						bestShortSideFit = shortSideFit;
						bestLongSideFit = longSideFit;
					}
				}

				if (allowRotation && freeRectangles[i].width >= height && freeRectangles[i].height >= width)
				{
					int leftoverHoriz = Math.Abs(freeRectangles[i].width - height);
					int leftoverVert = Math.Abs(freeRectangles[i].height - width);
					int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
					int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

					if (longSideFit < bestLongSideFit || (longSideFit == bestLongSideFit && shortSideFit < bestShortSideFit))
					{
						bestNode.x = freeRectangles[i].x;
						bestNode.y = freeRectangles[i].y;
						bestNode.width = height;
						bestNode.height = width;
						bestShortSideFit = shortSideFit;
						bestLongSideFit = longSideFit;
					}
				}
			}
			return bestNode;
		}

		Rect FindPositionForNewNodeBestAreaFit(int width, int height, ref int bestAreaFit, ref int bestShortSideFit)
		{
			Rect bestNode = new Rect();

			bestAreaFit = Int32.MaxValue;

			for (int i = 0; i < freeRectangles.Count; ++i)
			{
				int areaFit = freeRectangles[i].width * freeRectangles[i].height - width * height;
                
				if (freeRectangles[i].width >= width && freeRectangles[i].height >= height)
				{
					int leftoverHoriz = Math.Abs(freeRectangles[i].width - width);
					int leftoverVert = Math.Abs(freeRectangles[i].height - height);
					int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

					if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
					{
						bestNode.x = freeRectangles[i].x;
						bestNode.y = freeRectangles[i].y;
						bestNode.width = width;
						bestNode.height = height;
						bestShortSideFit = shortSideFit;
						bestAreaFit = areaFit;
					}
				}

				if (allowRotation && freeRectangles[i].width >= height && freeRectangles[i].height >= width)
				{
					int leftoverHoriz = Math.Abs(freeRectangles[i].width - height);
					int leftoverVert = Math.Abs(freeRectangles[i].height - width);
					int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);

					if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shortSideFit < bestShortSideFit))
					{
						bestNode.x = freeRectangles[i].x;
						bestNode.y = freeRectangles[i].y;
						bestNode.width = height;
						bestNode.height = width;
						bestShortSideFit = shortSideFit;
						bestAreaFit = areaFit;
					}
				}
			}
			return bestNode;
		}

		Rect FindPositionForNewNodeContactPoint(int width, int height, ref int bestContactScore)
		{
			Rect bestNode = new Rect();
			bestContactScore = -1;

			for (int i = 0; i < freeRectangles.Count; ++i)
			{
				if (freeRectangles[i].width >= width && freeRectangles[i].height >= height)
				{
					int score = ContactPointScoreNode(freeRectangles[i].x, freeRectangles[i].y, width, height);
					if (score > bestContactScore)
					{
						bestNode.x = freeRectangles[i].x;
						bestNode.y = freeRectangles[i].y;
						bestNode.width = width;
						bestNode.height = height;
						bestContactScore = score;
					}
				}
				if (allowRotation && freeRectangles[i].width >= height && freeRectangles[i].height >= width)
				{
					int score = ContactPointScoreNode(freeRectangles[i].x, freeRectangles[i].y, width, height);
					if (score > bestContactScore)
					{
						bestNode.x = freeRectangles[i].x;
						bestNode.y = freeRectangles[i].y;
						bestNode.width = height;
						bestNode.height = width;
						bestContactScore = score;
					}
				}
			}
			return bestNode;
		}

		//如果成功被分割
		bool SplitFreeNode(Rect freeNode, Rect usedNode)
		{
			//测试矩阵是否相交 
			if (usedNode.x >= freeNode.x + freeNode.width || usedNode.x + usedNode.width <= freeNode.x ||
				usedNode.y >= freeNode.y + freeNode.height || usedNode.y + usedNode.height <= freeNode.y)
				return false;

			if (usedNode.x < freeNode.x + freeNode.width && usedNode.x + usedNode.width > freeNode.x)
			{
                //新节点位于所使用节点的顶部 
                if (usedNode.y > freeNode.y && usedNode.y < freeNode.y + freeNode.height)
				{
					Rect newNode = freeNode.Copy();
					newNode.height = usedNode.y - newNode.y;
					freeRectangles.Add(newNode);
				}

				//底部
				if (usedNode.y + usedNode.height < freeNode.y + freeNode.height)
				{
					Rect newNode = freeNode.Copy();
					newNode.y = usedNode.y + usedNode.height;
					newNode.height = freeNode.y + freeNode.height - (usedNode.y + usedNode.height);
					freeRectangles.Add(newNode);
				}
			}

			if (usedNode.y < freeNode.y + freeNode.height && usedNode.y + usedNode.height > freeNode.y)
			{
				//左侧
				if (usedNode.x > freeNode.x && usedNode.x < freeNode.x + freeNode.width)
				{
					Rect newNode = freeNode.Copy();
					newNode.width = usedNode.x - newNode.x;
					freeRectangles.Add(newNode);
				}

				//右侧
				if (usedNode.x + usedNode.width < freeNode.x + freeNode.width)
				{
					Rect newNode = freeNode.Copy();
					newNode.x = usedNode.x + usedNode.width;
					newNode.width = freeNode.x + freeNode.width - (usedNode.x + usedNode.width);
					freeRectangles.Add(newNode);
				}
			}

			return true;
		}

        //遍历自由矩形列表并删除所有冗余项
        void PruneFreeList()
		{
			for (int i = 0; i < freeRectangles.Count; ++i)
			{
				for (int j = i + 1; j < freeRectangles.Count; ++j)
				{
					if (Rect.IsContainedIn(freeRectangles[i], freeRectangles[j]))
					{
						freeRectangles.RemoveAt(i);
						--i;
						break;
					}
					if (Rect.IsContainedIn(freeRectangles[j], freeRectangles[i]))
					{
						freeRectangles.RemoveAt(j);
						--j;
					}
				}
			}
		}

        //如果两个间隔i1和i2是不相交的，而是它们重叠的长度，则返回0
        int CommonIntervalLength(int i1start, int i1end, int i2start, int i2end)
		{
			if (i1end < i2start || i2end < i1start)
				return 0;
			return Math.Min(i1end, i2end) - Math.Max(i1start, i2start);
		}
	}

}
