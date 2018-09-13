using UnityEditor;
using UnityEngine;
using SpriteAnimEdit.Core.Sprite;

namespace SpriteAnimEdit.Editor.Ani
{
    public class SpriteAnimationEditorTextureView
    {
        public Texture2D CurTexture;
        private int textureBorderPixels;
        private float editorDisplayScale;
        public SpriteAnimationData.FrameData CurFrameData;
        private Vector2 textureScrollPos;

        public SpriteAnimationEditorTextureView()
        {
            CurTexture = null;
            CurFrameData = null;
            textureBorderPixels = 0;
            editorDisplayScale = 1f;
            textureScrollPos = new Vector2(0.0f, 0.0f);
        }

        public void Draw()
        {
            if (editorDisplayScale <= 1.0f) editorDisplayScale = 1.0f;
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Rect rect = GUILayoutUtility.GetRect(128.0f, 128.0f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            TextureGrid.Draw(rect);

			int heightOffset = 50;
            Handles.color = new Color(1, 1, 0, 0.5f);
			Handles.DrawLine(new Vector2(rect.x+10, rect.center.y + heightOffset), new Vector2(rect.x + rect.width - 10, rect.center.y + heightOffset));
            Handles.DrawLine(new Vector2(rect.center.x, rect.y+10), new Vector2(rect.center.x, rect.y + rect.height-10));

            if (CurTexture != null)
            {
                // middle mouse drag and scroll zoom
                if (rect.Contains(Event.current.mousePosition))
                {
					// 拖动时修改偏移量
                    if (Event.current.type == EventType.MouseDrag )	// && Event.current.button == 2)
                    {
						//放大后调整偏移值
						CurFrameData.Offset += Event.current.delta / editorDisplayScale;
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
                textureScrollPos = GUI.BeginScrollView(rect, textureScrollPos,
                    new Rect(0, 0, textureBorderPixels * 2 + (CurTexture.width) * editorDisplayScale, textureBorderPixels * 2 + (CurTexture.height) * editorDisplayScale));

                Rect textureRect = new Rect(textureBorderPixels, textureBorderPixels, CurTexture.width * editorDisplayScale, CurTexture.height * editorDisplayScale);
                
				// 支持放大后调整偏移值
				textureRect.x = rect.width / 2 - textureRect.width / 2 + CurFrameData.Offset.x * editorDisplayScale;
				textureRect.y = rect.height / 2 + heightOffset - textureRect.height + CurFrameData.Offset.y * editorDisplayScale;

                CurTexture.filterMode = FilterMode.Point;
                GUI.DrawTexture(textureRect, CurTexture, ScaleMode.ScaleAndCrop, alphaBlend);
                
                GUI.EndScrollView();

               
            }
            GUILayout.EndVertical();
        }


    }
}
