using UnityEditor;
using System.Collections;
using UnityEngine;
namespace SpriteAnimEdit
{
    public class SetPrefabName : EditorWindow
    {
        static SetPrefabName window;
        string prefabName = "";

        static bool showAtlasWindow = false;
        static bool showAnimationWindow = false;
        private void OnEnable()
        {
            showAtlasWindow = false;
            showAnimationWindow = false;
        }

        [MenuItem("Assets/Create SpriteAtlasData Prefab")]
        static void AddAtlasWindow()
        {
            CreateWindow();
            showAtlasWindow = true;
        }

        [MenuItem("Assets/Create SpriteAnimationData Prefab")]
        static void AddAnimationWindow()
        {
            CreateWindow();
            showAnimationWindow = true;
        }

        static void CreateWindow()
        {
            //创建窗口
            Rect wr = new Rect(Screen.width, Screen.height, 300f, 80f);
            window = (SetPrefabName)EditorWindow.GetWindowWithRect(typeof(SetPrefabName), wr, true, "创建Prefab");
            window.Show();
        }

        private void OnGUI()
        {
            BeginWindows();
            if (showAtlasWindow)
            {
                ShowTextFiled("请输入SpriteAtlas Prefab Name");
                if (GUILayout.Button( "确认", GUILayout.Width(50)))
                {
                    if (prefabName.Length < 1)
                    {
                        if (!EditorUtility.DisplayDialog("错误", "prefab name erro", "TryAgain", "Exit"))
                        {
                            showAtlasWindow = false;
                            window.Close();
                        }
                        return;
                    }
                    showAtlasWindow = false;
                    SpriteAtlasBuilderEditor.CreateSpriteAtlas(prefabName);
                    window.Close();
                }
            }else if (showAnimationWindow)
            {
                ShowTextFiled("请输入SpriteAnimation Prefab Name");
                if (GUILayout.Button("确认", GUILayout.Width(50)))
                {
                    if (prefabName.Length < 1)
                    {
                        if (!EditorUtility.DisplayDialog("错误", "prefab name erro", "TryAgain", "Exit"))
                        {
                            showAnimationWindow = false;
                            window.Close();
                        }
                        return;
                    }
                    showAnimationWindow = false;
                    SpriteAnimationBuilderEditor.CreateSpriteCollection(prefabName);
                    window.Close();
                }
            }
            
            EndWindows();
            if (window == null)
            {
                showAtlasWindow = false;
                showAnimationWindow = false;
            }
        }

        void OnLostFocus()
        {
            showAtlasWindow = false;
            showAnimationWindow = false;
        }

        void ShowTextFiled(string descrip)
        {
            GUILayout.Label(descrip, GUILayout.Width(300));
            prefabName = GUILayout.TextField(prefabName, GUILayout.Width(300));
        }
    }
}