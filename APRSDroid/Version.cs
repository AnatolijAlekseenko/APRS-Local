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
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Threading;

namespace APRSDroid
{
    class Version
    {
        private static string URL = String.Format("{0}plugins/AndroidWebservices/", "https://172.17.1.29/");
        private static string Pach = string.Empty;

        public static void StatusVersionApp(string pach, Context mContext)
        {
            try
            {
                Pach = pach;
                string HashLocal = GetHash();
                string json = Get("api.php?method=GetVersionAprs");
                Dictionary<string, string> DIC = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (!(DIC["hesh"] == HashLocal))
                {
                    var dialog = new AlertDialog.Builder(mContext);
                    dialog.SetTitle("Система");
                    dialog.SetMessage("Доступно обновление.\n Загрузить и установить?\n\n Имя файла: " + DIC["name"] + "\n Размер: " + DIC["size"] + "\n");
                    dialog.SetPositiveButton("Да", (s, c) =>
                    {
                        Intent intent = new Intent(mContext, typeof(DowlandVersionActivity));
                        intent.PutExtra("BYTE", DIC["bytes"]);
                        intent.PutExtra("NAME", DIC["name"]);
                        intent.PutExtra("DIR", Pach);
                        mContext.StartActivity(intent);
                    });
                    dialog.SetNegativeButton("Нет", (s, c) =>
                    {});
                    dialog.Show();
                }
            }
            catch (Exception)
            {
            }
        }

        private static string GetHash()
        {
            try
            {
                string str = string.Empty;
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(String.Format("{0}/aprs.apk", Pach)))
                    {
                        byte[] MyByte = md5.ComputeHash(stream);
                        str = BitConverter.ToString(MyByte);
                        str = str.Replace("-", "").ToLower();
                    }
                }
                return str;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static  bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static  string Get(string Data)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL + Data);
                request.ContentType = "application/json";
                request.Method = "GET";
                request.Timeout = 10000;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return string.Empty;
                    }
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        var content = reader.ReadToEnd();
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            return string.Empty;
                        }
                        else
                        {
                            return content;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static bool DownloadFiles(string name)
        {
            try
            {
                string NameFileVersion = name;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                WebClient webClient = new WebClient();
                webClient.DownloadFile("https://172.17.1.29/glpi/plugins/AndroidWebservices/aprs/" + NameFileVersion, String.Format("{0}/aprs.apk", Pach));
                return true;
            }
            catch (Exception ex)
            {
                string mess = ex.Message;
                string mess2 = ex.ToString();
                return false;
            }
        }
    }
}