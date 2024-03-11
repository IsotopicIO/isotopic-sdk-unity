#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace IsotopicSDK.Publishing
{
    [CreateAssetMenu(fileName = "Isotopic Publishing Config", menuName = "Isotopic/Publishing Config")]
    public class IsotopicPublishingConfig : ScriptableObject
    {
        [Header("This an optional config.")] 
        [Header("Use this to:")]
        [Header("- Upload your game to Isotopic from inside Unity")]
        [Header("- Automate game uploads to Isotopic")]
        [Space(10)]
        public IsotopicAccountCredentials AccountCredentials;
        public IsotopicGameCredentials GameCredentials;

        [Space(20)]
        [Header("=========================")]
        [Space(10)]
        [SerializeField, InspectorName("Click to Open Publishing Window:")] private bool openPublishingWindow;
        [SerializeField, InspectorName("Do not have an app on Isotopci yet? Click here:")] private bool openWebsite;

        private static IsotopicPublishingConfig _Instance;
        public static IsotopicPublishingConfig GetInstance()
        {
            if (_Instance != null) return _Instance;
            var guid = AssetDatabase.FindAssets($"t:{nameof(IsotopicPublishingConfig)}")[0];
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            _Instance = AssetDatabase.LoadAssetAtPath(assetPath, typeof(IsotopicPublishingConfig)) as IsotopicPublishingConfig;
            return _Instance;
        }

        private void OnValidate()
        {
            if (openPublishingWindow)
            {
                IsotopicPublisherEditor.Init();
                openPublishingWindow = false;
            }
            if (openWebsite)
            {
                Application.OpenURL("https://isotopic.io/game-store/#publish-game");
                openWebsite = false;
            }
        }
    }

    [System.Serializable]
    public class IsotopicAccountCredentials
    {
        public string AccountUsername;
        public Password AccountPassword;
    }

    [System.Serializable]
    public class Password
    {
        [Header("Note: Do not share, do not push online.")]
        [Header("We recommend you add this asset to your .gitignore")]
        public string AccountPassword;
    }

    [System.Serializable]
    public class IsotopicGameCredentials
    {
        [Tooltip("Find this in \"game\" field of URL in editing page or store page of your app on Isotopic.")]
        public string AppID;
        public E_Platforms Platform;

        public enum E_Platforms
        {
            Mobile,
            VR,
            Windows,
            Linux,
            Mac,
            Web
        }
    }
}
#endif