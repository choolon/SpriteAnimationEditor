using UnityEditor;
using UnityEngine;

namespace SpriteAnimEdit.Editor.Sprite
{
    public class SpriteAtlasEditorTextureView
    {
		private SpriteAtlasEditorPopup host = null;
		private SpriteAtlasProxy _spriteAtlasProxy { get { return host.SpriteAtlasProxy; } }
        private float editorDisplayScale = 1f;
        private Vector2 textureScrollPos = new Vector2(0.0f, 0.0f);
        private int textureBorderPixels = 0;
        public Texture2D CurTexture;

		public SpriteAtlasEditorTextureView(SpriteAtlasEditorPopup host)
        {
			this.host = host;
        }

        //大图绘制
        public void Draw()
        {
            if (editorDisplayScale <= 1.0f) editorDisplayScale = 1.0f;
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Rect rect = GUILayoutUtility.GetRect(128.0f, 128.0f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            //在给定矩形内裁剪或平铺图像
            TextureGrid.Draw(rect);

            if (CurTexture != null)
            {
                // middle mouse drag and scroll zoom
                if (rect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDrag && Event.current.button == 2)
                    {
                        textureScrollPos -= Event.current.delta * editorDisplayScale;
                        Event.current.Use();
                        HandleUtility.Repaint();
                    }
                    if (Event.current.type == EventType.ScrollWheel)
                    {
                        editorDisplayScale -= Event.current.delta.y * 0.03f;
                        Event.current.Use();
                        HandleUtility.Repaint();
                    }
                }

                bool alphaBlend = true;
				Rect scrollViewRect = new Rect(0, 0, textureBorderPixels * 2 + rect.width * editorDisplayScale, textureBorderPixels * 2 + rect.height * editorDisplayScale);
				Vector2 anchorPointInView = new Vector2(scrollViewRect.center.x + _spriteAtlasProxy.anchorPointInEditor.x, scrollViewRect.yMax - _spriteAtlasProxy.anchorPointInEditor.y); 
				textureScrollPos = GUI.BeginScrollView(rect, textureScrollPos, scrollViewRect);

                Rect textureRect = new Rect(0, 0, CurTexture.width * editorDisplayScale, CurTexture.height * editorDisplayScale);
				textureRect.x = anchorPointInView.x - (textureRect.width * 0.5f + _spriteAtlasProxy.anchorPointInImage.x);
				textureRect.y = anchorPointInView.y - (textureRect.height - _spriteAtlasProxy.anchorPointInImage.y);

                CurTexture.filterMode = FilterMode.Point;
                GUI.DrawTexture(textureRect, CurTexture, ScaleMode.ScaleAndCrop, alphaBlend);
                GameEditorUtility.DrawOutline(textureRect, Color.green);
				GameEditorUtility.DrawOutline(scrollViewRect, Color.blue);

				// 编辑器中的锚点
				Handles.color = new Color(1, 1, 0, 0.5f);
				Handles.DrawLine(new Vector2(scrollViewRect.x + 10, anchorPointInView.y), new Vector2(scrollViewRect.x + scrollViewRect.width - 10, anchorPointInView.y));
				Handles.DrawLine(new Vector2(anchorPointInView.x, scrollViewRect.yMin + 10), new Vector2(anchorPointInView.x, scrollViewRect.yMax - 10));
				
				// 图片上的锚点
				Handles.color = new Color(0, 1, 0, 0.5f);
				Handles.DrawLine(new Vector2(textureRect.center.x - 10, textureRect.yMax - _spriteAtlasProxy.anchorPointInImage.y), new Vector2(textureRect.center.x + 10, textureRect.yMax - _spriteAtlasProxy.anchorPointInImage.y));
				Handles.DrawLine(new Vector2(textureRect.center.x, textureRect.yMax - _spriteAtlasProxy.anchorPointInImage.y - 10), new Vector2(textureRect.center.x, textureRect.yMax - _spriteAtlasProxy.anchorPointInImage.y + 10));

                GUI.EndScrollView();

                GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
                GUILayout.Label(string.Format("Name:{0} W: {1} H: {2}",CurTexture.name, CurTexture.width, CurTexture.height));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }


    }
}
