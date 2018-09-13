
using System.Collections.Generic;
using SpriteAnimEdit.Core.Sprite;
using UnityEditor;
using UnityEngine;

namespace SpriteAnimEdit.Editor.Sprite
{
    public class SpriteAtlasProxy
    {
        public SpriteAtlasData obj;
        public List<SpriteAtlasData.SpriteData> spriteDataList = new List<SpriteAtlasData.SpriteData>();
        public Texture2D[] atlasTextures;
        public Material[] atlasMaterials;
       

        public SpriteAtlasProxy(SpriteAtlasData obj)
		{
			this.obj = obj;
            CopyFromSource();
		}

        public void CopyFromSource()
        {
            if (obj.spriteDataList == null)
            {
                obj.spriteDataList = new SpriteAtlasData.SpriteData[0];
            }
            spriteDataList = new List<SpriteAtlasData.SpriteData>(obj.spriteDataList.Length);
            
            foreach (var v in obj.spriteDataList)
            {
                if (v == null)
                    spriteDataList.Add(null);
                else
                {
                    var t = new SpriteAtlasData.SpriteData();
                    t.CopyFrom(v);
                    spriteDataList.Add(t);
                }
            }
        }


      

        public void CopyToTarget(SpriteAtlasData target)
        {
            target.version = SpriteAtlasData.CURRENT_VERSION;

            spriteDataList.Sort((a, b) => a.name.CompareTo(b.name));
            target.spriteDataList = spriteDataList.ToArray();
          
            target.atlasTextures = atlasTextures;
            target.atlasMaterials = atlasMaterials;
        }

        public int FindSpriteBySource(string name)
        {
            for (int i = 0; i < spriteDataList.Count; ++i)
            {
                if (spriteDataList[i].name == name)
                {
                    return i;
                }
            }
            return -1;
        }

       

        public int FindOrCreateEmptySpriteSlot()
        {
            for (int index = 0; index < spriteDataList.Count; ++index)
            {
                if (spriteDataList[index].name.Length == 0)
                    return index;
            }
            spriteDataList.Add(new SpriteAtlasData.SpriteData());
            return spriteDataList.Count - 1;
        }

        public bool Empty { get { return spriteDataList.Count == 0; } }

        public bool dice = false;

        public bool forceTextureSize = false;
        public int forcedTextureWidth = 1024;
        public int forcedTextureHeight = 1024;

        public bool forceSquareAtlas = true;
        public int maxTextureSize = 1024;

        public bool allowMultipleAtlases = false;
        public bool disableRotation = true;
        public bool disableTrimming = false;
		public bool outputDebugGraphic = false;

		public Vector2 anchorPointInEditor = new Vector2(0, 300);		// 锚点在编辑器（图片View）中的位置（相对于View的下部中间）
		public Vector2 anchorPointInImage = new Vector2(0, 254);		// 锚点在图片上的位置（相对于图片的下部中间）

        public int atlasWidth, atlasHeight;
        public float atlasWastage;

        public int padding = 2;
    }
}
