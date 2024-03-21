using UnityEngine;
using ChainSafe.Gaming.UnityPackage;
#if UNITY_EDITOR
using IsotopicSDK.Utils;
using IsotopicSDK.API;
using System;
using System.Collections.Generic;
using UnityEditor;
#endif

namespace IsotopicSDK {

    [CreateAssetMenu(fileName = "IsotopicSDKConfig", menuName = "Isotopic/SDK Config")]
    public class IsotopicSDKConfig : ScriptableObject
    {
        public string IsotopicAppID;
        public IsotopicAsset[] IsotopicAssets;

#if UNITY_EDITOR
        [SerializeField]
        [Tooltip("Use this if you are testing functionality of Isotopic Assets, but should be disabled for production.")]
        private bool useTestnet = true;

        [SerializeField]
        [Tooltip("Lookup UUIDs in registry (use to ensure correct IDs)")]
        private bool lookupAssetUuids = false;
        private bool isCheckingAssets = false;
#endif

        [System.Serializable]
        public class IsotopicAsset
        {
            public string AssetUUID;
            [HideInInspector] public string AssetContractAddress;
            [HideInInspector] public string AssetContractABI;
            [HideInInspector] public string Title;
            [HideInInspector] public string Description;
            public GameObject AssetPrefab;
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            SetupChainSafeConfig();
            if (lookupAssetUuids)
            {
                if (!isCheckingAssets)
                {
                    ConfirmAssetUUIDS();
                }
                lookupAssetUuids = false;
            }
        }
        private void SetupChainSafeConfig()
        {
            var chainsafeConfig = ProjectConfigUtilities.CreateOrLoad();
            EditorUtility.SetDirty(chainsafeConfig);
            if (useTestnet)
            {
                chainsafeConfig.Chain = "SKALE Nebula Hub Testnet";
                chainsafeConfig.ChainId = "37084624";
                chainsafeConfig.Network = "lanky-ill-funny-testnet";
                chainsafeConfig.ProjectId = "832b3033-ff8a-472a-a922-eb6eb5068efe";
                chainsafeConfig.Symbol = "sFUEL";
                chainsafeConfig.Rpc = "https://testnet.skalenodes.com/v1/lanky-ill-funny-testnet";
            }
            else
            {
                chainsafeConfig.Chain = "SKALE Nebula Hub";
                chainsafeConfig.ChainId = "1482601649";
                chainsafeConfig.Network = "green-giddy-denebola";
                chainsafeConfig.ProjectId = "832b3033-ff8a-472a-a922-eb6eb5068efe";
                chainsafeConfig.Symbol = "sFUEL";
                chainsafeConfig.Rpc = "https://mainnet.skalenodes.com/v1/green-giddy-denebola";
            }
            ProjectConfigUtilities.Save(chainsafeConfig);
            EditorUtility.ClearDirty(chainsafeConfig);
        }
        private void ConfirmAssetUUIDS()
        {
            isCheckingAssets = true;
            Debug.Log("=======================================");
            Debug.Log("Isotopic SDK: Confirming Asset UUIDs...");
            List<Action<Action>> assetLoadingTasks = new List<Action<Action>>();
            foreach (var asset in IsotopicAssets)
            {
                var cacheAsset = new IsotopicAsset();
                bool assetInvalid = false;
                void taskW3(Action task_cb)
                {
                    IsotopicAPI.Assets.GetAssetDetailsW3(asset.AssetUUID, details =>
                    {
                        if (details.result == "fail") assetInvalid = true;
                        cacheAsset.AssetContractAddress = details.contract_address;
                        cacheAsset.AssetContractABI = details.contract_abi;
                        task_cb?.Invoke();
                    });
                }

                void taskDetails(Action task_cb)
                {
                    IsotopicAPI.Assets.GetAssetDetails(asset.AssetUUID, details =>
                    {
                        if (details.result == "fail") assetInvalid = true;
                        cacheAsset.Title = details.title;
                        cacheAsset.Description = details.short_description;
                        task_cb?.Invoke();
                    });
                }

                void getAllAssetDetails(Action task_cb)
                {
                    IsoUtils.RunTasksParallel(new List<Action<Action>> { taskDetails, taskW3 }, () =>
                    {
                        if (assetInvalid)
                        {
                            Debug.Log($"\"{asset.AssetUUID}\" not found in Isotopic registry.");
                        } else
                        {
                            Debug.Log($"\"{asset.AssetUUID}\" found: \"{cacheAsset.Title}\".");
                        }
                        task_cb?.Invoke();
                    });
                }

                assetLoadingTasks.Add(getAllAssetDetails);
            }
            IsoUtils.RunQueuedTasks(assetLoadingTasks, 2, ()=> {
                Debug.Log("Isotopic SDK: Finished assets lookup.");
                Debug.Log("=====================================");
                isCheckingAssets = false;
            });
        }
#endif
    }
}