#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO.Compression;
using UnityEditor;
using System.Threading.Tasks;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;


namespace IsotopicSDK.Publishing
{
    public static class IsotopicPublisher
    {
        public static bool InProgress { get; private set; } = false;

        private static IsotopicPublishingConfig PublishingConfig => IsotopicPublishingConfig.GetInstance();
        private static string _session_id;
        private static async Task<bool> Login()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            IsotopicPublisherEditor.Instance.AddDebug($"Logging in with account {PublishingConfig.AccountCredentials.AccountUsername}...");
            IsotopicPublishingNetworkingSystem.APIs.LogIn(new IsotopicPublishingNetworkingSystem.LoginBody { username = PublishingConfig.AccountCredentials.AccountUsername, password = PublishingConfig.AccountCredentials.AccountPassword.AccountPassword }, res =>
            {
                if (res.result != "success")
                {
                    tcs.SetResult(false);
                    return;
                }

                _session_id = res.session_id;
                tcs.SetResult(true);
            });
            await tcs.Task;
            return tcs.Task.Result;
        }

        public async static void UploadBuildFolder(string buildRootPath, string zipFileDestination, Action<bool> cb)
        {
            IsotopicPublisherEditor.Init();
            if (InProgress)
            {
                IsotopicPublisherEditor.Instance.AddDebug("Publishing is already in progress. Please wait...");
                cb?.Invoke(false);
                return;
            }
            InProgress = true;

            IsotopicPublisherEditor.Instance.AddDebug("Zipping Build Directory... (May take a few minutes)");
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            new Task(() =>
            {
                var compressResult = compressDirectory(buildRootPath, zipFileDestination);
                tcs.SetResult(compressResult);
            }).Start();
            await tcs.Task;
            if (tcs.Task.Result == false)
            {
                IsotopicPublisherEditor.Instance.AddDebug("Zip Failed. See error above.");
                InProgress = false;
                cb?.Invoke(false);
                return;
            }
            IsotopicPublisherEditor.Instance.AddDebug("Build Zipped Succesfully.");
            UploadBuildZip(zipFileDestination, cb);
        }

        public static async void UploadBuildZip(string zipPath, Action<bool> cb, bool init_here = false)
        {
            if (InProgress && init_here)
            {
                IsotopicPublisherEditor.Instance.AddDebug("Publishing is already in progress. Please wait...");
                cb?.Invoke(false);
                return;
            }
            InProgress = true;
            IsotopicPublisherEditor.Init();
            bool loginRes = await Login();
            if (!loginRes)
            {
                IsotopicPublisherEditor.Instance.AddDebug($"Error: Failed to login with account {PublishingConfig.AccountCredentials.AccountUsername}. Please check credentials and try again.");
                InProgress = false;
                return;
            }
            FileInfo zipInfo = new FileInfo(zipPath);
            int size = (int)zipInfo.Length;
            IsotopicPublisherEditor.Instance.AddDebug("Initiating Upload...");
            IsotopicPublishingNetworkingSystem.APIs.InitUploadGameBuild(new IsotopicPublishingNetworkingSystem.InitUploadBody
            {
                session_id = _session_id,
                file_size = size,
                app_id = PublishingConfig.GameCredentials.AppID,
                platform = PublishingConfig.GameCredentials.Platform.ToString().ToLower(),
                is_download = "true",
            }, async InitRes =>
            {
                if (InitRes.result != "success")
                {
                    IsotopicPublisherEditor.Instance.AddDebug($"Error: Upload initialization failed.");
                    InProgress = false;
                    cb?.Invoke(false);
                    return;
                }

                float progress = 0;
                bool cancelled = false;
                    
                for (int i=0; i< InitRes.total_chunks; i++)
                {
                    if (cancelled)
                    {
                        break;
                    }
                    long sliceStart = i * InitRes.chunk_size;
                    long sliceEnd = (i + 1) * InitRes.chunk_size;
                    sliceEnd = sliceEnd > zipInfo.Length ? zipInfo.Length : sliceEnd;

                    byte[] bytes = ReadBytesFromFile(zipPath, sliceStart, sliceEnd);
                    string chunk_data = Convert.ToBase64String(bytes);

                    int retries = 0;

                    a_entry:

                    TaskCompletionSource<IsotopicPublishingNetworkingSystem.UploadChunkResult> tcs = new TaskCompletionSource<IsotopicPublishingNetworkingSystem.UploadChunkResult>();
                    IsotopicPublishingNetworkingSystem.APIs.UploadBuildChunk(new IsotopicPublishingNetworkingSystem.UploadChunkBody
                    {
                        app_id = PublishingConfig.GameCredentials.AppID,
                        session_id = _session_id,
                        chunk_data = chunk_data,
                        upload_id = InitRes.upload_id.ToString()
                    }, chunkRes =>
                    {
                        tcs.SetResult(chunkRes);
                    });
                    await tcs.Task;

                    var chunkRes = tcs.Task.Result;
                    if (chunkRes.result == "fail")
                    {
                        retries++;
                        if (retries > 3)
                        {
                            cancelled = true;
                            IsotopicPublisherEditor.Instance.AddDebug("Error: Upload Fatal Error. Please try again.");
                            InProgress = false;
                            cb?.Invoke(false);
                        } else
                        {
                            goto a_entry;
                        }
                            
                    } else if (chunkRes.status == "done")
                    {
                        IsotopicPublisherEditor.Instance.AddDebug("Upload Successful! Pending Moderator Review. Thanks for publishing with Isotopic!");
                        InProgress = false;
                        cb?.Invoke(true);
                    } else
                    {
                        progress = i / (float)(InitRes.total_chunks);
                        IsotopicPublisherEditor.Instance.AddDebug($"Uploading... Progress: {(progress * 100):F1}%");
                    }
                }
            });
        }

        static byte[] ReadBytesFromFile(string filePath, long startIndex, long endIndex)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                long length = endIndex - startIndex;
                byte[] bytes = new byte[length];

                fs.Seek(startIndex, SeekOrigin.Begin);
                fs.Read(bytes, 0, (int)length);

                return bytes;
            }
        }

        private static bool compressDirectory(string DirectoryPath, string OutputFilePath, int CompressionLevel = 9)
        {
            try
            {
                // Depending on the directory this could be very large and would require more attention
                // in a commercial package.
                string[] filenames = Directory.GetFiles(DirectoryPath);

                // 'using' statements guarantee the stream is closed properly which is a big source
                // of problems otherwise.  Its exception safe as well which is great.
                using (ZipOutputStream OutputStream = new ZipOutputStream(File.Create(OutputFilePath)))
                {

                    // Define the compression level
                    // 0 - store only to 9 - means best compression
                    OutputStream.SetLevel(CompressionLevel);

                    byte[] buffer = new byte[4096];

                    foreach (string file in filenames)
                    {

                        // Using GetFileName makes the result compatible with XP
                        // as the resulting path is not absolute.
                        ZipEntry entry = new ZipEntry(Path.GetFileName(file));

                        // Setup the entry data as required.

                        // Crc and size are handled by the library for seakable streams
                        // so no need to do them here.

                        // Could also use the last write time or similar for the file.
                        entry.DateTime = DateTime.Now;
                        OutputStream.PutNextEntry(entry);

                        using (FileStream fs = File.OpenRead(file))
                        {

                            // Using a fixed size buffer here makes no noticeable difference for output
                            // but keeps a lid on memory usage.
                            int sourceBytes;

                            do
                            {
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                OutputStream.Write(buffer, 0, sourceBytes);
                            } while (sourceBytes > 0);
                        }
                    }

                    // Finish/Close arent needed strictly as the using statement does this automatically

                    // Finish is important to ensure trailing information for a Zip file is appended.  Without this
                    // the created file would be invalid.
                    OutputStream.Finish();

                    // Close is important to wrap things up and unlock the file.
                    OutputStream.Close();

                    return true;
                }
            }
            catch 
            {
                return false;
            }
        }
    }
}

#endif