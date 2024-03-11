using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Threading;


#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace IsotopicSDK.API
{
    public class IsotopicAPI
    {
        private const string BASE_API_URL = "https://dapp.isotopic.io/api/v1";
        public static class OAuth
        {
            // Create a One-Time-Code for user authentication
            public static void CreateOTC(string app_id, Action<CreateOTCResult> cb)
            {
                PostJSON(BASE_API_URL + "/oauth/client/create-one-time", new CreateOTCBody { app_id = app_id }.ToJSON(), jobj =>
                {
                    if (jobj == null) cb?.Invoke(new CreateOTCResult { result = "fail", error = "An unexpected error occured." });
                    cb?.Invoke(new CreateOTCResult
                    {
                        result = jobj["result"]?.ToString(),
                        error = jobj["error"]?.ToString(),
                        client_id = jobj["client_id"]?.ToString(),
                        one_time_code = jobj["one_time_code"]?.ToString(),
                        listen = jobj["listen"]?.ToString(),
                    });
                });
            }

            public static void ConnectToOAuthLoginWS(string wsURI, Action<OAuthWSLoginReponse> cb)
            {
                // Connect to the pending login websocket and wait for user to log in.
                var WS = new ClientWebSocket();
                WS.ConnectAsync(new Uri(wsURI), CancellationToken.None).ContinueWith(task =>
                {
                    var receiveTask = Task.Run(async () =>
                    {
                        var buffer = new byte[1024*4];
                        while (true)
                        {
                            var msg = await WS.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                            if (msg.MessageType == WebSocketMessageType.Close)
                            {
                                break;
                            }

                            var dataString = Encoding.UTF8.GetString(buffer, 0, msg.Count);
                            try
                            {
                                var jobj = JToken.Parse(dataString);
                                cb?.Invoke(new OAuthWSLoginReponse
                                {
                                    result = jobj["result"]?.ToString(),
                                    error = jobj["error"]?.ToString(),
                                    token = jobj["token"]?.ToString(),
                                });
                            }
                            catch
                            {
                                cb?.Invoke(new OAuthWSLoginReponse { result = "fail", error = "Unexpected Error." });
                            }
                        }
                    });
                });
                
            }

            #region DATA-TYPES
            public class CreateOTCResult
            {
                public string result;
                public string error;

                public string client_id;
                public string one_time_code;
                public string listen;
            }
            [Serializable]
            public class CreateOTCBody : JSONAble
            {
                public string app_id;
            }

            public class OAuthWSLoginReponse
            {
                public string result;
                public string error;
                public string token;
            }
            #endregion
        }

        public static class Assets
        {
            public static void GetAssetDetails(string asset_id, Action<AssetDetailsResult> cb)
            {
                GetJSON(BASE_API_URL + "/assets/details/"+asset_id, jobj =>
                {
                    if (jobj == null) cb?.Invoke(new AssetDetailsResult { result = "fail", error = "An unexpected error occured." });
                    cb?.Invoke(new AssetDetailsResult
                    {
                        result = jobj["result"]?.ToString(),
                        error = jobj["error"]?.ToString(),
                        asset_id = asset_id,
                        details_html = jobj["details_html"]?.ToString(),
                        short_description = jobj["short_description"]?.ToString(),
                        special_tags = jobj["special_tags"]?.ToString(),
                        genres = jobj["genres"]?.ToString(),
                        keywords = jobj["keywords"]?.ToString(),
                        publisher_name = jobj["publisher_name"]?.ToString(),
                        title = jobj["title"]?.ToString(),
                    });
                });
            }
            public static void GetAssetDetailsW3(string asset_id, Action<AssetDetailsW3Result> cb)
            {
                GetJSON(BASE_API_URL + "/assets/web3/" + asset_id, jobj =>
                {
                    if (jobj == null) cb?.Invoke(new AssetDetailsW3Result { result = "fail", error = "An unexpected error occured." });
                    cb?.Invoke(new AssetDetailsW3Result
                    {
                        result = jobj["result"]?.ToString(),
                        error = jobj["error"]?.ToString(),
                        contract_abi = jobj["contract_abi"]?.ToString(),
                        contract_address = jobj["contract_address"]?.ToString(),
                        payment_contract_abi = jobj["payment_contract_abi"]?.ToString(),
                        payment_contract_address = jobj["payment_contract_address"]?.ToString(),
                        web3_enabled = (bool)jobj["web3_enabled"],
                        web3_network = jobj["web3_network"]?.ToString(),
                    });
                });
            }


            #region DATA-TYPES
            public class AssetDetailsResult
            {
                public string result;
                public string error;
                public string title, publisher_name, short_description, details_html, keywords, genres, special_tags, asset_id;
            }
            public class AssetDetailsW3Result
            {
                public string result;
                public string error;
                public bool web3_enabled;
                public string web3_network; //FUTURE: Separate Class, Deserialized.
                public string contract_address, contract_abi, payment_contract_address, payment_contract_abi;
            } 
            #endregion
        }

        public static class User
        {
            public static void GetUserDetailsByToken(string token, Action<UserDetailsResult> cb)
            {
                PostJSON(BASE_API_URL + "/user/oauth-lookup", new GetUserDetailsByTokenBody { access_token = token }.ToJSON(), jobj=>
                {
                    if (jobj == null) cb?.Invoke(new UserDetailsResult { result = "fail", error = "An unexpected error occured." });
                    cb?.Invoke(new UserDetailsResult
                    {
                        result = jobj["result"]?.ToString(),
                        error = jobj["error"]?.ToString(),
                        username = jobj["user"]?["username"]?.ToString(),
                        user_id = jobj["user"]?["user_id"]?.ToString(),
                        profile_picture = jobj["user"]?["profile_picture"]?.ToString(),
                    });
                });
            }

            public static void GetUserProfileTextureBytes(string user_id, Action<byte[]> cb)
            {
                GeneralHelper.GetBytesFromURL(BASE_API_URL + $"/user/profile/pic/get?user_id={user_id}", cb);
            }

            public static void GetUserWallets(string token, Action<GetUserWalletsResult> cb)
            {
                PostJSON(BASE_API_URL + "/wallet/get", new GetUserDetailsByTokenBody { access_token = token }.ToJSON(), jobj =>
                {
                    if (jobj == null) cb?.Invoke(new GetUserWalletsResult { result = "fail", error = "An unexpected error occured." });
                    var result = new GetUserWalletsResult();
                    try
                    {
                        result.result = jobj["result"]?.ToString();
                        result.error = jobj["error"]?.ToString();
                    } catch (Exception){
                        result.result = "success";
                    }
                    if (result.result == "fail")
                    {
                        cb?.Invoke(result);
                        return;
                    }
                    List<GetUserWalletsResult.Wallet> wallets = new List<GetUserWalletsResult.Wallet>();
                    foreach (JToken wallet in jobj)
                    {
                        wallets.Add(new GetUserWalletsResult.Wallet()
                        {
                            address = wallet["address"]?.ToString(),
                            is_custodial = (bool)wallet["is_custodial"],
                            provider = wallet["provider"]?.ToString()
                        });
                    }
                    result.wallets = wallets.FindAll(wallet => wallet.address != null).ToArray();
                    cb?.Invoke(result);
                });
            }


            #region DATA-TYPES
            [Serializable]
            public class GetUserDetailsByTokenBody : JSONAble
            {
                public string access_token;
            }
            public class UserDetailsResult
            {
                public string result;
                public string error;

                public string username;
                public string user_id;
                public string profile_picture;
            }
            public class GetUserWalletsResult
            {
                public string result;
                public string error;

                public Wallet[] wallets;
                public class Wallet
                {
                    public string address;
                    public bool is_custodial;
                    public string provider;
                }
            } 
            #endregion

            public static class CloudStorage
            {

                public static void SetCloudSave(string token, string data, Action<SetCloudSaveResult> cb)
                {
                    PostJSON(BASE_API_URL + "/cloud/game_data/set", new SetCloudSaveBody { access_token = token, data=data }.ToJSON(), jobj =>
                    {
                        if (jobj == null) cb?.Invoke(new SetCloudSaveResult { result = "fail", error = "An unexpected error occured." });
                        cb?.Invoke(new SetCloudSaveResult
                        {
                            result = jobj["result"]?.ToString(),
                            error = jobj["error"]?.ToString(),
                        });
                    });
                }
                public static void GetCloudSave(string token, Action<GetCloudSaveResult> cb)
                {
                    PostJSON(BASE_API_URL + "/cloud/game_data/get", new GetCloudSaveBody { access_token = token }.ToJSON(), jobj =>
                    {
                        if (jobj == null) cb?.Invoke(new GetCloudSaveResult { result = "fail", error = "An unexpected error occured." });
                        cb?.Invoke(new GetCloudSaveResult
                        {
                            result = jobj["result"]?.ToString(),
                            error = jobj["error"]?.ToString(),
                            data = jobj["data"]?.ToString()
                        });
                    });
                }

                #region DATA-TYPES
                [Serializable]
                public class GetCloudSaveBody : JSONAble
                {
                    public string access_token;
                }

                [Serializable]
                public class SetCloudSaveBody : JSONAble
                {
                    public string access_token;
                    public string data;
                }

                public class GetCloudSaveResult
                {
                    public string result;
                    public string error;

                    public string data;
                }
                public class SetCloudSaveResult
                {
                    public string result;
                    public string error;
                } 
                #endregion
            }
        }

        public static class GeneralHelper
        {
            public static void GetBytesFromURL(string url, Action<byte[]> cb)
            {
                RequestGetRaw(url, cb);
            }

        }

        public class JSONAble
        {
            public string ToJSON() => JsonUtility.ToJson(this);
        }

        #region NETWORK METHODS
        private static void GetJSON(string url, Action<JToken> callback)
        {
            GetJSONAsyncHandler(url, callback);
        }
        private static void PostJSON(string url, string body, Action<JToken> callback)
        {
            PostJSONAsyncHandler(url, body, callback);
        }

        private static async Task GetRedirectURL(string url, Action<string> callback)
        {
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false
            };
            string redirectedUrl = null;

            using (HttpClient client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Found)
                {
                    HttpResponseHeaders headers = response.Headers;
                    if (headers != null && headers.Location != null)
                    {
                        redirectedUrl = headers.Location.AbsoluteUri;
                    }
                }
            }

            callback?.Invoke(redirectedUrl);
        }


        private static async Task PostJSONAsyncHandler(string url, string body, Action<JToken> callback)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(3);
            var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
            try
            {
                var httpResponse = await client.PostAsync(url, httpContent);
                if (!httpResponse.IsSuccessStatusCode) callback?.Invoke(null);
                string res = await httpResponse.Content.ReadAsStringAsync();
                callback?.Invoke(JToken.Parse(res));
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                // Handle timeout.
                callback?.Invoke(null);
            }
        }

        private static async Task GetJSONAsyncHandler(string url, Action<JToken> callback)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(3);
            var httpResponse = await client.GetAsync(url);
            if (!httpResponse.IsSuccessStatusCode) callback?.Invoke(null);
            string res = await httpResponse.Content.ReadAsStringAsync();
            callback?.Invoke(JToken.Parse(res));
        }

        private static async Task RequestGetRaw(string url, Action<byte[]> callback)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(3);
            var httpResponse = await client.GetAsync(url);
            if (!httpResponse.IsSuccessStatusCode) callback?.Invoke(null);
            byte[] res = await httpResponse.Content.ReadAsByteArrayAsync();
            callback?.Invoke(res);
        }

        public static bool IsConnected => !(Application.internetReachability == NetworkReachability.NotReachable);

        private static IEnumerator WaitForConnectionCoroutine(Action onConnect)
        {
            while (!IsConnected) yield return null;
            onConnect?.Invoke();
        }
        #endregion
    }
}

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
