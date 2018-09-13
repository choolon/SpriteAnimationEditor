using System.Collections.Generic;

namespace SpriteAnimEdit.Editor.Atlas
{
	class RectSize
	{
		public int width = 0;
		public int height = 0;
	};

	class Rect
	{
		public int x = 0;
		public int y = 0;
		public int width = 0;
		public int height = 0;

		public static bool IsContainedIn(Rect a, Rect b)
		{
			return (a.x >= b.x) && (a.y >= b.y)
				&& (a.x + a.width <= b.x + b.width)
				&& (a.y + a.height <= b.y + b.height);
		}

		public Rect Copy()
		{
			Rect r = new Rect();
			r.x = x;
			r.y = y;
			r.width = width;
			r.height = height;
			return r;
		}
	};

	class DisjointRectCollection
	{
		public List<Rect> rects = new List<Rect>();

		public bool Add(Rect r)
		{
			if (r.width == 0 || r.height == 0)
				return true;

			if (!Disjoint(r))
				return false;

			rects.Add(r);

			return true;
		}

		public void Clear()
		{
			rects.Clear();
		}

		bool Disjoint(Rect r)
		{
			if (r.width == 0 || r.height == 0)
				return true;

			for (int i = 0; i < rects.Count; ++i)
				if (!IsDisjoint(rects[i], r))
					return false;
			return true;
		}

		static bool IsDisjoint(Rect a, Rect b)
		{
			if ((a.x + a.width <= b.x) ||
				(b.x + b.width <= a.x) ||
				(a.y + a.height <= b.y) ||
				(b.y + b.height <= a.y))
				return true;
			return false;
		}
	};

}