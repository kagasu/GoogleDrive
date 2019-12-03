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
using Core;

namespace Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var proxy = new WebProxy("127.0.0.1", 8008);
            var client = new GoogleDriveClient(proxy);

            // https://accounts.google.com/o/oauth2/auth?client_id=645529619299.apps.googleusercontent.com&redirect_uri=urn:ietf:wg:oauth:2.0:oob&response_type=code&scope=https://www.googleapis.com/auth/drive
            await client.Auth("4/...");
            // Console.WriteLine(client.AccessToken);
            // Console.WriteLine(client.RefreshToken);

            // client.RefreshToken = "1//...";
            // await client.GetAccessTokenFromRefreshToken();

            var uploadFileResponse = await client.UploadFile("input.mp4", "video/mp4", "MyPerfectVideo.mp4", true);
            var fileId = uploadFileResponse["id"].Value<string>();
            await client.UpdateFileProperty(fileId, "description", "My perfect description");
            await client.GetFileInfo(fileId);
        }
    }
}
