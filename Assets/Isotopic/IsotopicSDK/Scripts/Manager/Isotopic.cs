using System.Collections.Generic;
using UnityEngine;
using ChainSafe.Gaming.UnityPackage;
using ChainSafe.Gaming.Web3.Build;
using ChainSafe.Gaming.Evm.JsonRpc;
using ChainSafe.Gaming.Web3.Unity;
using System;
using Scripts.EVM.Token;
using System.Threading.Tasks;
using ChainSafe.Gaming.Web3;
using System.Numerics;
using IsotopicSDK.Utils;
using IsotopicSDK.API;
using Newtonsoft.Json;

namespace IsotopicSDK
{
    public class Isotopic : MonoBehaviour
    {
        public static Isotopic Instance { get; private set; }

        /// <summary>
        /// Fires when connection with web3 RPC endpoints is complete and web3 services become available.
        /// </summary>
        public static event Action<Web3> Web3Set;

        /// <summary>
        /// Instance object for interacting with web3.
        /// </summary>
        public static Web3 Web3Instance;

        /// <summary>
        /// Fires when all required systems are ready and all available APIs of the SDK can be safely called.
        /// </summary>
        public static event Action Ready;

        [SerializeField] private IsotopicSDKConfig sdkConfig;
        public static IsotopicSDKConfig SDKConfig => Instance.sdkConfig;

        public string AppID => sdkConfig.IsotopicAppID;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
                AwakeInstance();
            }
            else
            {
                Destroy(this);
            }
        }

        private async void AwakeInstance()
        {
            await LoadWeb3();

            LoadAssetDetails(() =>
            {
                Ready?.Invoke();
            });
        }

        // Loads Web3 functionalities from ChainSafe and set the Web3 Instance.
        private async Task LoadWeb3()
        {
            var projectConfig = ProjectConfigUtilities.Load();
            var web3 = await new Web3Builder(projectConfig).Configure(services =>
            {
                services.UseUnityEnvironment();
                services.UseRpcProvider();
            }).LaunchAsync();

            Web3Accessor.Set(web3);
            Web3Instance = web3;
            Web3Set?.Invoke(web3);
        }

        // Loads the details for all registered Isotopic Assets in the SDK Config.
        private void LoadAssetDetails(Action cb)
        {
            List<Action<Action>> assetLoadingTasks = new List<Action<Action>>();
            foreach (var asset in SDKConfig.IsotopicAssets)
            {
                void taskW3(Action task_cb)
                {
                    IsotopicAPI.Assets.GetAssetDetailsW3(asset.AssetUUID, details =>
                    {
                        asset.AssetContractAddress = details.contract_address;
                        asset.AssetContractABI = details.contract_abi;
                        task_cb?.Invoke();
                    });
                }
                assetLoadingTasks.Add(taskW3);

                void taskDetails(Action task_cb)
                {
                    IsotopicAPI.Assets.GetAssetDetails(asset.AssetUUID, details =>
                    {
                        asset.Title = details.title;
                        asset.Description = details.short_description;
                        task_cb?.Invoke();
                    });
                }
                assetLoadingTasks.Add(taskDetails);
            }
            IsoUtils.RunQueuedTasks(assetLoadingTasks, 4, cb);
        }

        /// <summary>
        /// Access to methods that connect with Isotopic online services.
        /// </summary>
        public static class Network
        {
            public static class IsotopicAssets
            {
                public static async void GetIsotopicAssetBalance(IsotopicSDKConfig.IsotopicAsset asset, Action<BigInteger> cb)
                {
                    BigInteger balance = 0;
                    foreach (var wallet in User.UserProfile.Wallets)
                    {
                        balance += await GetERC721Balance(asset.AssetContractAddress, wallet.address);
                    }
                    cb?.Invoke(balance);
                }

                public static async Task<BigInteger> GetERC721Balance(string contractAddress, string walletAddress)
                {
                    var contract = Web3Instance.ContractBuilder.Build(ABI.Erc721, contractAddress);
                    var output = await contract.Call("balanceOf", new object[] { walletAddress });
                    try
                    {
                        return (BigInteger)output[0];
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Contract returned non-integer output.", e);
                    }
                }
            }
        }

        public static class User
        {
            private static Profile userProfile;
            public static Profile UserProfile
            {
                get => userProfile;
                set
                {
                    var oldProfile = userProfile;
                    userProfile = value;
                    UserProfileChange?.Invoke((oldProfile, value));
                }
            }
            public static event Action<(Profile oldProfile, Profile newProfile)> UserProfileChange;


            public class Profile
            {
                public string Username { get; private set; }
                public string AccessToken { get; private set; }
                public string UserID { get; private set; }
                public Texture2D ProfileImage { get; private set; }
                public IsotopicAPI.User.GetUserWalletsResult.Wallet[] Wallets { get; private set; }

                public Profile(string username, string accessToken, string userID, Texture2D profileImage, IsotopicAPI.User.GetUserWalletsResult.Wallet[] wallets)
                {
                    this.Username = username;
                    this.ProfileImage = profileImage;
                    this.AccessToken = accessToken;
                    this.UserID = userID;
                    this.Wallets = wallets;
                }
            }

            public static class CloudStore
            {
                public static bool ChangesSaved { get; private set; }
                public static bool SyncingInProgress {get; private set;}

                [System.Serializable]
                private class CloudStorageObject
                {
                    public Dictionary<string, int> integers;
                    public Dictionary<string, string> strings;
                    public Dictionary<string, float> floats;
                }

                private static Dictionary<string, int> cachedStoreInt = new Dictionary<string, int>();
                private static Dictionary<string, string> cachedStoreString = new Dictionary<string, string>();
                private static Dictionary<string, float> cachedStoreFloat = new Dictionary<string, float>();

                // Set key-values.
                public static void SetInt(string key, int value) {
                    cachedStoreInt[key] = value;
                    ChangesSaved = false;
                }
                public static void SetString(string key, string value) {
                    cachedStoreString[key] = value; 
                    ChangesSaved = false;
                }
                public static void SetFloat(string key, float value){
                    cachedStoreFloat[key] = value; 
                    ChangesSaved = false;
                }

                // Get key-values
                public static int? GetInt(string key) => cachedStoreInt.TryGetValue(key, out int value) ? value : null;
                public static string GetString(string key) => cachedStoreString.TryGetValue(key, out string value) ? value : null;
                public static float? GetFloat(string key) => cachedStoreFloat.TryGetValue(key, out float value) ? value : null;

                /// <summary>
                /// Upload all changes online to Isotopic Cloud
                /// </summary>
                /// <param name="cb">Callback, contains true if succeeded.</param>
                public static void SaveVariablesToCloud(Action<bool> cb)
                {
                    SyncingInProgress = true;
                    IsotopicAPI.User.CloudStorage.SetCloudSave(UserProfile.AccessToken, JsonConvert.SerializeObject(new CloudStorageObject()
                    {
                        strings = cachedStoreString,
                        floats = cachedStoreFloat,
                        integers = cachedStoreInt
                    }), res =>
                    {
                        SyncingInProgress = false;
                        ChangesSaved = true;
                        cb?.Invoke(res.result == "success");
                    });
                }

                /// <summary>
                /// Does NOT need to be called every time you access a variable. This is called automatically at the start.
                /// </summary>
                /// <param name="cb">Callback, true or false based on success.</param>
                public static void SyncFromCloud(Action<bool> cb)
                {
                    SyncingInProgress = true;
                    IsotopicAPI.User.CloudStorage.GetCloudSave(UserProfile.AccessToken, res =>
                    {
                        if (res.result != "success") cb?.Invoke(false);

                        try
                        {
                            var deserialized = JsonConvert.DeserializeObject<CloudStorageObject>(res.data);
                            cachedStoreString = deserialized.strings ?? new Dictionary<string, string>();
                            cachedStoreFloat = deserialized.floats ?? new Dictionary<string, float>();
                            cachedStoreInt = deserialized.integers ?? new Dictionary<string, int>();
                            SyncingInProgress = false;
                            cb?.Invoke(true);
                        }
                        catch
                        {
                            SyncingInProgress = false;
                            cb?.Invoke(false);
                        }
                    });
                }
            }
        }
    }
}