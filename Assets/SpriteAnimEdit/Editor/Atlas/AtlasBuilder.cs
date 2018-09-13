using System;
using System.Collections.Generic;

namespace SpriteAnimEdit.Editor.Atlas
{
	public class Entry
	{
		public int index;
		public int x, y;
		public int w, h;
		public bool flipped;
	}

	public class Data
	{
		public int width, height;
		public float occupancy;
		public Entry[] entries;
		
		public Entry FindEntryWithIndex(int index)
		{
			return System.Array.Find(entries, (e) => index == e.index);
		}
	}
	
	public class Builder
	{
		int maxAllowedAtlasCount = 0;
		int atlasWidth = 0;
		int atlasHeight = 0;
		bool forceSquare = false;
		bool allowOptimizeSize = true;
		int alignShift = 0;
		bool allowRotation = true;
		
		List<RectSize> sourceRects = new List<RectSize>();

		List<Data> atlases = new List<Data>();
		List<int> remainingRectIndices = new List<int>();
		
		bool oversizeTextures = false;

		public Builder(int atlasWidth, int atlasHeight, int maxAllowedAtlasCount, bool allowOptimizeSize, bool forceSquare, bool allowRotation)
		{
			this.atlasWidth = atlasWidth;
			this.atlasHeight = atlasHeight;
			this.maxAllowedAtlasCount = maxAllowedAtlasCount;
			this.forceSquare = forceSquare;
			this.allowOptimizeSize = allowOptimizeSize;
			this.allowRotation = allowRotation;
		}
		
		public void AddRect(int width, int height)
		{
			RectSize rs = new RectSize();
			rs.width = width;
			rs.height = height;
			sourceRects.Add(rs);
		}

		MaxRectsBinPack FindBestBinPacker(int width, int height, ref List<RectSize> currRects, ref bool allUsed)
		{
			List<MaxRectsBinPack> binPackers = new List<MaxRectsBinPack>();
			List<List<RectSize>> binPackerRects = new List<List<RectSize>>();
			List<bool> binPackerAllUsed = new List<bool>();

			//MaxRectsBinPack.FreeRectChoiceHeuristic[] heuristics = { MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit,
			//                                                         MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestLongSideFit,
			//                                                         MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit,
			//                                                         MaxRectsBinPack.FreeRectChoiceHeuristic.RectBottomLeftRule,
			//                                                         MaxRectsBinPack.FreeRectChoiceHeuristic.RectContactPointRule };

			MaxRectsBinPack.FreeRectChoiceHeuristic[] heuristics = { MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit,
			                                                         MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestLongSideFit,
			                                                         MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit,
			                                                         MaxRectsBinPack.FreeRectChoiceHeuristic.RectBottomLeftRule,
			                                                          };

			foreach (var heuristic in heuristics)
			{
				MaxRectsBinPack binPacker = new MaxRectsBinPack(width, height, allowRotation);
				List<RectSize> activeRects = new List<RectSize>(currRects);
				bool activeAllUsed = binPacker.Insert(activeRects, heuristic);

				binPackers.Add(binPacker);
				binPackerRects.Add(activeRects);
				binPackerAllUsed.Add(activeAllUsed);
			}

			int leastWastedPixels = Int32.MaxValue;
			int leastWastedIndex = -1;
			for (int i = 0; i < binPackers.Count; ++i)
			{
				int wastedPixels = binPackers[i].WastedBinArea();
				if (wastedPixels < leastWastedPixels)
				{
					leastWastedPixels = wastedPixels;
					leastWastedIndex = i;
					oversizeTextures = true;
				}
			}

			currRects = binPackerRects[leastWastedIndex];
			allUsed = binPackerAllUsed[leastWastedIndex];
			return binPackers[leastWastedIndex];
		}
		
        //��ʼ��ͼ��
		public int Build()
		{
			atlases = new List<Data>();
			remainingRectIndices = new List<int>();
			bool[] usedRect = new bool[sourceRects.Count];

			int atlasWidth = this.atlasWidth >> alignShift;
			int atlasHeight = this.atlasHeight >> alignShift;

            //�����Լ�飬����ʹ�ñ�ʵ�����ͼ����С�����������
            int align = (1 << alignShift) - 1;
			int minSize = Math.Min(atlasWidth, atlasHeight);
			int maxSize = Math.Max(atlasWidth, atlasHeight);
			foreach (RectSize rs in sourceRects)
			{
				int maxDim = (Math.Max(rs.width, rs.height) + align) >> alignShift;
				int minDim = (Math.Min(rs.width, rs.height) + align) >> alignShift;
				
				//��Ҫ��������ߴ�
				if (maxDim > maxSize || (maxDim <= maxSize && minDim > minSize))
				{
					remainingRectIndices = new List<int>();
					for (int i = 0; i < sourceRects.Count; ++i)
						remainingRectIndices.Add(i);
					return remainingRectIndices.Count;
				}
			}
			
			//����ʱ�����ƣ�����
			List<RectSize> rects = new List<RectSize>();
			foreach (RectSize rs in sourceRects)
			{
				RectSize t = new RectSize();
				t.width = (rs.width + align) >> alignShift;
				t.height = (rs.height + align) >> alignShift;
				rects.Add(t);
			}

			bool allUsed = false;
			while (allUsed == false && atlases.Count < maxAllowedAtlasCount)
			{
				int numPasses = 1;
				int thisCellW = atlasWidth, thisCellH = atlasHeight;
				bool reverted = false;

				while (numPasses > 0)
				{
					//����һ�ݣ��ڱ�Ҫʱ��������
					List<RectSize> currRects = new List<RectSize>(rects);

//					MaxRectsBinPack binPacker = new MaxRectsBinPack(thisCellW, thisCellH);
//					allUsed = binPacker.Insert(currRects, MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit);
					MaxRectsBinPack binPacker = FindBestBinPacker(thisCellW, thisCellH, ref currRects, ref allUsed);
					float occupancy = binPacker.Occupancy();

                    //�����ڵ�һ�α���֮������ʹ������ռ��> 0.5f����һ�룬�Ա���PO2���������õ�ɡ�����
                    bool firstPassFull = numPasses == 1 && occupancy > 0.5f;

                    // ��ռ����< 0.5ʱ���ָ�ͼ����С������
                    // ��ʱ����С�ָ�����ǰ��ֵ��ѭ��Ӧ�ý������ֵ��Ϊ����ֵ
                    if ( firstPassFull ||
						(numPasses > 1 && occupancy > 0.5f && allUsed) ||
						reverted || !allowOptimizeSize)
					{
						List<Entry> atlasEntries = new List<Entry>();
						
						foreach (var t in binPacker.GetMapped())
						{
							int matchedWidth = 0;
							int matchedHeight = 0;

							int matchedId = -1;
							bool flipped = false;
							for (int i = 0; i < sourceRects.Count; ++i)
							{
								int width = (sourceRects[i].width + align) >> alignShift;
								int height = (sourceRects[i].height + align) >> alignShift;
								if (!usedRect[i] && width == t.width && height == t.height)
								{
									matchedId = i;
									matchedWidth = sourceRects[i].width;
									matchedHeight = sourceRects[i].height;
									break;
								}
							}

                            //��û��ƥ����κζ�������ʼѰ����ͬ�ľ���
                            if (matchedId == -1)
							{
								for (int i = 0; i < sourceRects.Count; ++i)
								{
									int width = (sourceRects[i].width + align) >> alignShift;
									int height = (sourceRects[i].height + align) >> alignShift;
									if (!usedRect[i] && width == t.height && height == t.width)
									{
										matchedId = i;
										flipped = true;
										matchedWidth = sourceRects[i].height;
										matchedHeight = sourceRects[i].width;
										break;
									}
								}
							}
							
							usedRect[matchedId] = true;
							Entry newEntry = new Entry();
							newEntry.flipped = flipped;
							newEntry.x = t.x << alignShift;
							newEntry.y = t.y << alignShift;
							newEntry.w = matchedWidth;
							newEntry.h = matchedHeight;
							newEntry.index = matchedId;
							atlasEntries.Add(newEntry);
						}

						Data currAtlas = new Data();
						currAtlas.width = thisCellW << alignShift;
						currAtlas.height = thisCellH << alignShift;
						currAtlas.occupancy = binPacker.Occupancy();
						currAtlas.entries = atlasEntries.ToArray();
						
						atlases.Add(currAtlas);

						rects = currRects;
						break; // done
					}
					else
					{
						if (!allUsed) 
						{
							if (forceSquare)
							{
								thisCellW *= 2;
								thisCellH *= 2;
							}
							else
							{
                                //ֻ���ڵ�һ����С�ߴ����ܳ�����һ���ߴ� 
                                if (thisCellW < atlasWidth || thisCellH < atlasHeight)
								{
                                    //��ͼ��С�������������ʺϣ����Իָ���ǰ�ĸ��ģ����ٴα������ݣ���ʹ��Щ�˷�
                                    if (thisCellW < thisCellH) thisCellW *= 2;
									else thisCellH *= 2;
								}
							}

							reverted = true;
						}
						else
						{
							if (forceSquare)
							{
								thisCellH /= 2;
								thisCellW /= 2;
							}
							else
							{
                                //����һ�������δ��ʹ�ã�������һ���ߴ���С 
                                if (thisCellW < thisCellH) thisCellH /= 2;
								else thisCellW /= 2;
							}
						}

						numPasses++;
					}
				}
			}
		
			remainingRectIndices = new List<int>();
			for (int i = 0; i < usedRect.Length; ++i)
			{
				if (!usedRect[i])
				{
					remainingRectIndices.Add(i);
				}
			}
				
			return remainingRectIndices.Count;
		}

		public Data[] GetAtlasData()
		{
			return atlases.ToArray();
		}

		public int[] GetRemainingRectIndices()
		{
			return remainingRectIndices.ToArray();
		}
		
		public bool HasOversizeTextures()
		{
			return oversizeTextures;
		}
	}
}

