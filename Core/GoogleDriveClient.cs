using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class GoogleDriveClient
    {
        // Ignore search engine
        private readonly string GoogleBackupAndSyncClientId = Encoding.ASCII.GetString(new byte[] { 0x36, 0x34, 0x35, 0x35, 0x32, 0x39, 0x36, 0x31, 0x39, 0x32, 0x39, 0x39, 0x2E, 0x61, 0x70, 0x70, 0x73, 0x2E, 0x67, 0x6F, 0x6F, 0x67, 0x6C, 0x65, 0x75, 0x73, 0x65, 0x72, 0x63, 0x6F, 0x6E, 0x74, 0x65, 0x6E, 0x74, 0x2E, 0x63, 0x6F, 0x6D });
        private readonly string GoogleBackupAndSyncClientSecret = Encoding.ASCII.GetString(new byte[] { 0x6E, 0x75, 0x36, 0x70, 0x58, 0x44, 0x56, 0x30, 0x4E, 0x4F, 0x5F, 0x48, 0x55, 0x79, 0x77, 0x79, 0x50, 0x38, 0x68, 0x75, 0x68, 0x32, 0x58, 0x6C });

        private HttpClient client;
        private string accessToken;
        public string AccessToken
        {
            get
            {
                return accessToken;
            }
            set
            {
                accessToken = value;
                HeaderSet("Authorization", $"OAuth {AccessToken}");
            }
        }
        public string RefreshToken { get; set; }

        public GoogleDriveClient(IWebProxy proxy = null)
        {
            var handler = new HttpClientHandler();
            if (proxy != null)
            {
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromDays(1);
        }

        private void HeaderSet(string name, string value)
        {
            if (client.DefaultRequestHeaders.Contains(name))
            {
                client.DefaultRequestHeaders.Remove(name);
            }

            client.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
        }

        public async Task Auth(string code)
        {
            var content = new FormUrlEncodedContent(
                new Dictionary<string, string>()
                {
                    { "code", code },
                    { "grant_type", "authorization_code" },
                    { "redirect_uri", "urn:ietf:wg:oauth:2.0:oob" },
                    { "client_id", GoogleBackupAndSyncClientId },
                    { "client_secret", GoogleBackupAndSyncClientSecret }
                });

            var response = await client.PostAsync("https://accounts.google.com/o/oauth2/token", content);
            var str = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(str);
            AccessToken = obj["access_token"].Value<string>();
            RefreshToken = obj["refresh_token"].Value<string>();
        }

        public async Task GetAccessTokenFromRefreshToken()
        {
            var content = new FormUrlEncodedContent(
                new Dictionary<string, string>()
                {
                    { "refresh_token", RefreshToken },
                    { "grant_type", "refresh_token" },
                    { "redirect_uri", "urn:ietf:wg:oauth:2.0:oob" },
                    { "client_id", GoogleBackupAndSyncClientId },
                    { "client_secret", GoogleBackupAndSyncClientSecret }
                });

            var response = await client.PostAsync("https://accounts.google.com/o/oauth2/token", content);
            var str = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(str);
            AccessToken = obj["access_token"].Value<string>();
        }

        private string GenerateQueryString(Dictionary<string, string> keyValuePairs)
        {
            return string.Join("&", keyValuePairs.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
        }

        public async Task<JObject> UploadFile(string filePath, string mimeType, string title, bool showInPhotos)
        {
            var parameters = new Dictionary<string, string>
            {
                ["convert"] = "false",
                ["fields"] = "title,parents/id,mimeType,modifiedDate,labels/restricted,userPermission/role,version,shared,fullFileExtension,shortcutDetails,id,fileSize,md5Checksum,photosCompressionStatus",
                ["pinned"] = "false",
                ["reason"] = "202",
                ["storagePolicy"] = "highQuality",
                ["uploadType"] = "resumable",
                ["alt"] = "json",
                ["updateViewedDate"] = "false",
            };

            var content = new StringContent(JsonConvert.SerializeObject(
                new Dictionary<string, object>()
                {
                    ["title"] = title,
                    ["mimeType"] = mimeType,
                    ["alwaysShowInPhotos"] = showInPhotos
                }), Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"https://www.googleapis.com/upload/drive/v2internal/files?{GenerateQueryString(parameters)}", content);
            var url = response.Headers.Location;
            var obj = await UploadFileInternal(url, filePath);

            return obj;
        }

        private async Task<JObject> UploadFileInternal(Uri url, string filePath)
        {
            var content = new StreamContent(File.OpenRead(filePath));

            var response = await client.PutAsync(url, content);
            var str = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(str);

            return obj;
        }

        public async Task<JObject> UpdateFileProperty(string fileId, string key, string value)
        {
            var content = new StringContent(JsonConvert.SerializeObject(
                new Dictionary<string, object>()
                {
                    [key] = value
                }), Encoding.UTF8, "application/json");

            var response = await client.PatchAsync($"https://www.googleapis.com/drive/v3/files/{fileId}", content);
            var str = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(str);

            return obj;
        }

        public async Task<JObject> GetFileInfo(string fileId)
        {
            var response = await client.GetAsync($"https://www.googleapis.com/drive/v2/files/{fileId}");
            var str = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(str);

            return obj;
        }
    }
}
