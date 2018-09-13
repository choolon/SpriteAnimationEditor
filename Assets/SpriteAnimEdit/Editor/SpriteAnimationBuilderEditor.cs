using System.IO;
using SpriteAnimEdit.Core.Sprite;
using SpriteAnimEdit.Editor.Ani;
using UnityEditor;
using UnityEngine;

namespace SpriteAnimEdit
{
    [CustomEditor(typeof(SpriteAnimationData))]
    public class SpriteAnimationBuilderEditor : UnityEditor.Editor
    {
        public static void CreateSpriteCollection(string prefabName)
        {
            string path = GameEditorUtility.CreateNewPrefab(prefabName);
            if (path.Length != 0)
            {
                SpriteAnimationData spriteAnimationData = ScriptableObject.CreateInstance<SpriteAnimationData>();
                spriteAnimationData.version = SpriteAnimationData.CURRENT_VERSION;
                SetAtlas(path, spriteAnimationData);
                AssetDatabase.CreateAsset(spriteAnimationData, path);
                //创建后拾取
                Selection.activeObject = AssetDatabase.LoadAssetAtPath(path, typeof(SpriteAnimationData));
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.BeginVertical();
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("打开动画编辑器", GUILayout.MinWidth(120)))
            {
                SpriteAnimationData gen = (SpriteAnimationData)target;

                SpriteAnimationEditorPopup v = EditorWindow.GetWindow(typeof(SpriteAnimationEditorPopup), false, "动画编辑器") as SpriteAnimationEditorPopup;

                v.SetGenerator(gen);
                v.Show();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private static void SetAtlas(string path, SpriteAnimationData spriteAnimationData)
        {
            DirectoryInfo info = new DirectoryInfo(Path.GetDirectoryName(path));
            FileInfo[] files = info.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.FullName.EndsWith("prefab")) //only prefab
                {
                    string fullPath = getAssetPath(file.FullName);
                    SpriteAtlasData objSpriteAtlasData = AssetDatabase.LoadAssetAtPath(fullPath, typeof(SpriteAtlasData)) as SpriteAtlasData;
                    if (objSpriteAtlasData != null)
                    {
                        spriteAnimationData.SpriteAtlasData = objSpriteAtlasData;
                    }
                }
            }
        }

        static string getAssetPath(string fullPath)
        {
            fullPath = fullPath.Replace('\\', '/');
            return fullPath.Replace(Application.dataPath, "Assets");
        }
    }
}
