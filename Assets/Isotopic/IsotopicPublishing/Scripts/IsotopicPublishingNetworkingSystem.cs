#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;

namespace IsotopicSDK.Publishing
{
    public class IsotopicPublishingNetworkingSystem
    {

        #region API Calls
        public static class APIs
        {
            private const string BaseURL = "https://dapp.isotopic.io/api/v1/";

            public static void LogIn(LoginBody body, Action<LoginResult> callback)
            {
                string URL = BaseURL+"user/login";
                PostJSON<LoginResult>(URL, body.ToJSON(), res =>
                {
                    if (res.Equals(default(LoginResult))) callback?.Invoke(new LoginResult { result = "fail", error = "No response." });
                    callback?.Invoke(res);
                });
            }

            public static void InitUploadGameBuild(InitUploadBody body, Action<InitUploadResult> callback)
            {
                string URL = BaseURL+ "store/game/upload/init";
                PostJSON<InitUploadResult>(URL, body.ToJSON(), res =>
                {
                    if (res.Equals(default(InitUploadResult))) callback?.Invoke(new InitUploadResult { result = "fail" });
                    callback?.Invoke(res);
                });
            }

            public static void UploadBuildChunk(UploadChunkBody body, Action<UploadChunkResult> callback)
            {
                string URL = BaseURL + "store/game/upload/chunk";
                PostJSON<UploadChunkResult>(URL, body.ToJSON(), res =>
                {
                    if (res.Equals(default(UploadChunkResult))) callback?.Invoke(new UploadChunkResult { result = "fail" });
                    callback?.Invoke(res);
                });
            }
        }
        #endregion

        #region API Objects
        //bodies
        public class LoginBody : JSONable
        {
            public string username;
            public string password;
        }

        public class InitUploadBody : JSONable
        {
            public string app_id;
            public int file_size;
            public string is_download;
            public string platform;
            public string session_id;
        }

        public class UploadChunkBody : JSONable
        {
            public string upload_id;
            public string app_id;
            public string chunk_data;

            public string session_id;
        }

        public struct LoginResult
        {
            public string result;
            public string username;
            public string user_id;
            public string error;
            public string session_id;
        }

        public struct InitUploadResult
        {
            public string result;
            public string upload_id;
            public int total_chunks;
            public int chunk_size;
        }

        public struct UploadChunkResult
        {
            public string result;
            public string status;
            public int remaining_chunks;
            public string error;
        }

        #endregion

        public class JSONable
        {
            public string ToJSON() => JsonUtility.ToJson(this);
        }

        #region Main Methods
        private static void GetJSON<T>(string url, Action<T> callback)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            GetJSONAsyncHandler<T>(url, callback);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
        private static void PostJSON<T>(string url, string body, Action<T> callback)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            PostJSONAsyncHandler<T>(url, body, callback);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        }

        private static async Task PostJSONAsyncHandler<T>(string url, string body, Action<T> callback)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(3);
            var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
            try
            {
                var httpResponse = await client.PostAsync(url, httpContent);
                if (!httpResponse.IsSuccessStatusCode) callback?.Invoke(default);
                string res = await httpResponse.Content.ReadAsStringAsync();
                callback?.Invoke(JsonUtility.FromJson<T>(res));
            }
            catch (Exception) 
            {
                callback?.Invoke(default);
            }
        }

        private static async Task GetJSONAsyncHandler<T>(string url, Action<T> callback)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(3);
            var httpResponse = await client.GetAsync(url);
            if (!httpResponse.IsSuccessStatusCode) callback?.Invoke(default);
            string res = await httpResponse.Content.ReadAsStringAsync();
            callback?.Invoke(JsonUtility.FromJson<T>(res));

        }

        public static bool IsConnected => !(Application.internetReachability == NetworkReachability.NotReachable);
        #endregion
    }
}
#endif