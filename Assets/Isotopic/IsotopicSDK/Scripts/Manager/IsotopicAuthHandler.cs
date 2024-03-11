using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using IsotopicSDK.Utils;
using IsotopicSDK.API;

namespace IsotopicSDK.OAuthScene { 
    public class IsotopicAuthHandler : MonoBehaviour
    {
        public IsotopicOAuthUIManager UIManager;
        public UnityEvent<Isotopic.User.Profile> OnIsotopicUserLoggedIn;

        public SceneField SceneToLoadAfterLogin;
        public SceneField SceneToUnloadAfterLogin;

        private readonly Queue<Action> mainThreadActionsBuffer = new Queue<Action>();
        private void ExecuteOnMainThread(Action action)
        {
            mainThreadActionsBuffer.Enqueue(action);
        }

        public void Awake()
        {
            Isotopic.User.UserProfileChange += ((Isotopic.User.Profile oldProfile, Isotopic.User.Profile newProfile) args) =>
            {
                OnIsotopicUserLoggedIn?.Invoke(args.newProfile);
            };
        }

        public void Start()
        {
            UIManager.SetOAuthUILoading(null, null);
            Isotopic.Ready += () =>
            {
                IsotopicAPI.OAuth.CreateOTC(Isotopic.Instance.AppID, otc_res =>
                {
                    UIManager.SetOAuthCodeUI(otc_res.one_time_code);

                    IsotopicAPI.OAuth.ConnectToOAuthLoginWS(otc_res.listen, loginRes =>
                    {
                        if (loginRes.result != "success")
                        {
                            return;
                        }
                        IsotopicAPI.User.GetUserDetailsByToken(loginRes.token, user =>
                        {
                            if (user.result == "fail")
                            {
                                return;
                            }
                            IsotopicAPI.User.GetUserProfileTextureBytes(user.user_id, profile_pic_tex_bytes =>
                            {

                                ExecuteOnMainThread(() =>
                                {
                                    Texture2D profile_pic_tex = new Texture2D(393, 393);
                                    ImageConversion.LoadImage(profile_pic_tex, profile_pic_tex_bytes);
                                    UIManager.SetOAuthUILoading(user.username, profile_pic_tex);
                                    IsotopicAPI.User.GetUserWallets(loginRes.token, wallets =>
                                    {
                                        ExecuteOnMainThread(() =>
                                        {
                                            Isotopic.User.UserProfile = new Isotopic.User.Profile(user.username, loginRes.token, user.user_id, profile_pic_tex, wallets.wallets);

                                            Isotopic.User.CloudStore.SyncFromCloud(null);

                                            if (SceneToLoadAfterLogin != null)
                                            {
                                                var sceneLoad = SceneManager.LoadSceneAsync(SceneToLoadAfterLogin);
                                            }
                                            else if (SceneToUnloadAfterLogin != null)
                                            {
                                                SceneManager.UnloadSceneAsync(SceneToUnloadAfterLogin);
                                            }

                                        });
                                    });
                                });
                            });
                        });

                    });
                });
            };
        }

        private void Update()
        {
            if (mainThreadActionsBuffer.Count > 0)
            {
                mainThreadActionsBuffer.Dequeue()?.Invoke();
            }
        }
    }
}
