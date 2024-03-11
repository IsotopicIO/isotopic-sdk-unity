#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace IsotopicSDK.Publishing
{
    public class IsotopicPublisherEditor : EditorWindow
    {
        public static IsotopicPublisherEditor Instance;

        [MenuItem("Window/Isotopic/Publishing")]
        public static void Init()
        {
            var window = (IsotopicPublisherEditor)EditorWindow.GetWindow(typeof(IsotopicPublisherEditor));
            window.Show();
            window.titleContent = new GUIContent("Isotopic Publisher");
            Instance = window;
            Instance.ClearDebug();
        }
        private string BuildRootPath = null;
        private string BuildZipPath = null;
        private List<string> DebugLines = new List<string>();
        Vector2 scrollPos;
        bool buildZippedAlready = false;
        
        private void OnGUI()
        {
            if (GUILayout.Button("Add/Edit Isotopic User"))
            {
                try
                {
                    var guid = AssetDatabase.FindAssets($"t:{nameof(IsotopicPublishingConfig)}")[0];
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
                } catch
                {
                    Debug.LogError("Isotopic: Publishing Config Not Found. Create one through Create > ScriptableObjects > Isotopic > Publishing Config.");
                }
            }
            
            if (string.IsNullOrEmpty(IsotopicPublishingConfig.GetInstance().AccountCredentials.AccountUsername))
            {
                GUILayout.Label("Username: -");
                return;
            }
            GUILayout.Label("Username: " + IsotopicPublishingConfig.GetInstance().AccountCredentials.AccountUsername);
            GUILayout.Space(10);

            if (IsotopicPublisher.InProgress)
            {
                GUILayout.Label("Upload in Progress...", EditorStyles.boldLabel);
            } else
            {
                buildZippedAlready = GUILayout.Toggle(buildZippedAlready, "Build Zipped Already?");
                GUILayout.Space(10);
                if (buildZippedAlready)
                {
                    if (GUILayout.Button("Select .zip Location"))
                    {
                        BuildZipPath = EditorUtility.OpenFilePanel("Select ZIP File", "", "zip");
                    }
                    GUILayout.Label(BuildZipPath ?? "-");
                    GUILayout.Space(10);
                } else
                {
                    if (GUILayout.Button("Select Build Folder"))
                    {
                        BuildRootPath = EditorUtility.OpenFolderPanel("Select root folder of build", "", "");
                    }
                    GUILayout.Label(BuildRootPath ?? "-");
                    GUILayout.Space(10);

                    if (GUILayout.Button("Select zip Destination"))
                    {
                        BuildZipPath = EditorUtility.SaveFilePanel("Select ZIP destination", "", "build-" + DateTime.Today.ToString("yyyy-MM-dd"), "zip");
                    }
                    GUILayout.Label(BuildZipPath ?? "-");
                    GUILayout.Space(10);
                }
                

                if (GUILayout.Button("Publish to Isotopic"))
                {
                    if (buildZippedAlready)
                    {
                        IsotopicPublisher.UploadBuildZip(BuildZipPath, res =>
                        {
                            Repaint();
                        }, true);
                        
                    } else
                    {
                        IsotopicPublisher.UploadBuildFolder(BuildRootPath, BuildZipPath, res =>
                        {
                            Repaint();
                        });
                    }
                    
                }
            }
            GUILayout.Space(10);
            GUILayout.Label("Console: ", EditorStyles.boldLabel);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (string s in DebugLines) GUILayout.Label("- " + s);
            GUILayout.EndScrollView();
        }

        public void AddDebug(string message)
        {
            DebugLines.Add(message);
        }

        public void ClearDebug()
        {
            DebugLines.Clear();
        }

        public void Refresh()
        {
            Instance?.Refresh();
        }
    }
}
#endif