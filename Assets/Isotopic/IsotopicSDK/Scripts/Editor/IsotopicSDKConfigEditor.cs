#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace IsotopicSDK.Editor
{
    public class IsotopicSDKConfigEditor : EditorWindow
    {
        public static IsotopicSDKConfigEditor Instance;
        private static Texture logo;
        private void OnEnable()
        {
            if (!logo)
            {
                logo = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Isotopic/IsotopicSDK/UI/Assets/isotopic_logo_red_small.png");
            }
        }

        [MenuItem("Window/Isotopic/Isotopic SDK")]
        public static void Init()
        {
            var window = (IsotopicSDKConfigEditor)EditorWindow.GetWindow(typeof(IsotopicSDKConfigEditor));
            window.Show();
            window.titleContent = new GUIContent("Isotopic SDK");
            Instance = window;
        }
        private void UnderlineLastField()
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            lastRect.y += lastRect.height; 
            lastRect.height = 2;
            GUI.Box(lastRect, "");
            
        }
        private string FieldWithName(string fieldName, string fieldValue, params GUILayoutOption[] nameLayoutOptions)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fieldName, nameLayoutOptions);
            var rtrn =  GUILayout.TextField(fieldValue);
            GUILayout.EndHorizontal();
            return rtrn;
        }
        

        private void OnGUI()
        {
            var titleStyle = EditorStyles.boldLabel;
            var subTitleStyle = EditorStyles.label;
            subTitleStyle.font = EditorStyles.boldFont;

            GUILayout.BeginHorizontal();

                GUILayout.Box(logo, GUILayout.Width(64f), GUILayout.Height(64f));

                GUILayout.BeginVertical();
                    GUILayout.Label("Welcome to the Isotopic SDK!", titleStyle);
                    GUILayout.Label("You can setup relevant information about your project below.", subTitleStyle);
                GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Space(25f);

            GUILayout.Label("Isotopic App Settings", subTitleStyle);
            UnderlineLastField();
            GUILayout.Space(5f);
            if (GUILayout.Button("Edit Configuration"))
            {
                try
                {
                    var guid = AssetDatabase.FindAssets($"t:{nameof(IsotopicSDKConfig)}")[0];
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
                }
                catch
                {
                    Debug.LogError("Isotopic: Isotopic SDK Config Not Found. Create one through Create > Isotopic > SDK Config.");
                }
            }
        }
    }
}

#endif