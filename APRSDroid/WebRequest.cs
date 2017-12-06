using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static APRSDroid.MainActivity;
using Android.Net;

namespace APRSDroid
{
    public static class WebRequest
    {
        public static async Task<string> SendPostFile(string p_filePath)
        {
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                Context mContext = Android.App.Application.Context;
                AppPreferences ap = new AppPreferences(mContext);

                client.BaseAddress = new System.Uri(ap.getURL_String());
                var values = new[]
                                   {
                                       new KeyValuePair<string, string>("Name", "filename"),
                                       new KeyValuePair<string, string>("id", "id"),
                                   };
                foreach (var keyValuePair in values)
                {
                    content.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
                }
                string fileName = p_filePath;
                var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(fileName));
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = Path.GetFileName(fileName)
                };
                content.Add(fileContent);

                var requestUri = "api/apisrv/";

                var result = client.PostAsync(requestUri, content).Result;

                string contents = result.Content.ReadAsStringAsync().Result.ToString().Replace(@"\", string.Empty).Replace("\"", "'").Replace("'{", "{").Replace("}'", "}");

                ResultResponse resultURL = JsonConvert.DeserializeObject<ResultResponse>(contents);

                return resultURL.UrlString;
            }
        }

    }

    public class ResultResponse
    {
        [JsonProperty(PropertyName = "UrlString")]
        public string UrlString { get; set; }
    }
}