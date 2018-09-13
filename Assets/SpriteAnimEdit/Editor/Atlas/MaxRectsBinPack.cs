using System;
using System.Collections.Generic;

namespace SpriteAnimEdit.Editor.Atlas
{

    /*MaxRectsBinPackʵ����MAXRECTS���ݽṹ��ʹ�����ֽṹ�Ĳ�ͬ��װ���㷨*/
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

        //��Ҫ���µ���ʱ����ʼ��һ�����x�߶ȵ�λ�Ŀ�����
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

        //ָ���ںδ������¾���ʱ����ʹ�õĲ�ͬ��������
        public enum FreeRectChoiceHeuristic
		{
			RectBestShortSideFit,   //���ζ̱�
			RectBestLongSideFit,    //���γ���
			RectBestAreaFit,        //��С��������
			RectBottomLeftRule,     //����ĳ��֮��
			RectContactPointRule    //�����ƽ���һ��
		};

        // ����
        // rects��ָ����������������������
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


		//��rect�в���һ������
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
        
        //����ʹ�õ����
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

        // ������ø������ε�λ�õ÷�
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
					score1 = -score1; //��ת�õ���С��ֵ�����Ӵ���÷�Խ��Խ��
                    break;
				case FreeRectChoiceHeuristic.RectBestLongSideFit: newNode = FindPositionForNewNodeBestLongSideFit(width, height, ref score2, ref score1); break;
				case FreeRectChoiceHeuristic.RectBestAreaFit: newNode = FindPositionForNewNodeBestAreaFit(width, height, ref score1, ref score2); break;
			}

            //�޷�ƥ�䵱ǰ���� 
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

        //��������λ�á�
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
                //���Ž��������ڴ�ֱ(�Ƿ�ת)����
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
                //���Ž��������ڴ�ֱ(�Ƿ�ת)���� 
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

		//����ɹ����ָ�
		bool SplitFreeNode(Rect freeNode, Rect usedNode)
		{
			//���Ծ����Ƿ��ཻ 
			if (usedNode.x >= freeNode.x + freeNode.width || usedNode.x + usedNode.width <= freeNode.x ||
				usedNode.y >= freeNode.y + freeNode.height || usedNode.y + usedNode.height <= freeNode.y)
				return false;

			if (usedNode.x < freeNode.x + freeNode.width && usedNode.x + usedNode.width > freeNode.x)
			{
                //�½ڵ�λ����ʹ�ýڵ�Ķ��� 
                if (usedNode.y > freeNode.y && usedNode.y < freeNode.y + freeNode.height)
				{
					Rect newNode = freeNode.Copy();
					newNode.height = usedNode.y - newNode.y;
					freeRectangles.Add(newNode);
				}

				//�ײ�
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
				//���
				if (usedNode.x > freeNode.x && usedNode.x < freeNode.x + freeNode.width)
				{
					Rect newNode = freeNode.Copy();
					newNode.width = usedNode.x - newNode.x;
					freeRectangles.Add(newNode);
				}

				//�Ҳ�
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

        //�������ɾ����б�ɾ������������
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

        //����������i1��i2�ǲ��ཻ�ģ����������ص��ĳ��ȣ��򷵻�0
        int CommonIntervalLength(int i1start, int i1end, int i2start, int i2end)
		{
			if (i1end < i2start || i2end < i1start)
				return 0;
			return Math.Min(i1end, i2end) - Math.Max(i1start, i2start);
		}
	}

}
