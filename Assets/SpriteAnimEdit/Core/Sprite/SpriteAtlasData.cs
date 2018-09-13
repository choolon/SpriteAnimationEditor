using UnityEngine;

namespace SpriteAnimEdit.Core.Sprite
{
    public class SpriteAtlasData : ScriptableObject
    {
        [System.Serializable]
        public class SpriteData
        {
            public string name = "";        //原始图名
            public string imagePath = "";   //图片路径
            public Rect bound;              //裁剪透明后的区域（左下角为0,0）
            public int regionX, regionY;    //在图集上的位置（左下角为0,0）
            public int regionH, regionW;    //在图集上的位置（左下角为0,0）
            public int anchorX, anchorY;    //锚点在图集上的位置（左下角为0,0）
            public int padding = 0;         //图片间隔
            public int sourceWidth = 512;   //原始图大小
            public int sourceHeight = 512;  //原始图大小
            public int atlasIndex = 0;      //所在大图索引

            public void CopyFrom(SpriteData src)
            {
                name = src.name;
				imagePath = src.imagePath;
                bound = new Rect(src.bound.x, src.bound.y, src.bound.width, bound.height);
                regionX = src.regionX;
                regionY = src.regionY;
                regionW = src.regionW;
                regionH = src.regionH;
                padding = src.padding;
                sourceWidth = src.sourceWidth;
                sourceWidth = src.sourceHeight;
                atlasIndex = src.atlasIndex;
            }
        }

		/// <summary>
		/// 1、修改SpriteData的bound.width和bound.height的意义为宽和高（原来为最大x和最大y）
		/// 2、SpriteData中增加锚点在图集上的位置，方便计算网格顶点坐标等
		/// </summary>
		public const int CURRENT_VERSION = 1;
        public int version = 0;
        public Texture2D[] atlasTextures;
        public Material[] atlasMaterials;
        public SpriteData[] spriteDataList;

        //通过Sprite名获取其信息
        public SpriteAtlasData.SpriteData GetSpriteData(string spriteName)
        {
            if (!string.IsNullOrEmpty(spriteName))
            {
                for (int i = 0; i < spriteDataList.Length; i++)
                {
                    SpriteData spriteData = spriteDataList[i];
                    if (spriteData.name.Equals(spriteName))
                    {
                        return spriteData;
                    }
                }
            }
            return null;
        }
    }

   
}
