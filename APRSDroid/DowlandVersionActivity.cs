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
using System.IO;
using System.Threading;

namespace APRSDroid
{
    [Activity(Label = "Обновление")]
    public class DowlandVersionActivity : Activity
    {

        private TextView TextView;
        private ProgressBar ProgressBar;

        private string bytes = string.Empty;
        private string name = string.Empty;
        private string pach = string.Empty;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.DowlandVersion);

            TextView = FindViewById<TextView>(Resource.Id.textView);
            ProgressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);

            bytes = Intent.GetStringExtra("BYTE") ?? string.Empty;
            name = Intent.GetStringExtra("NAME") ?? string.Empty;
            pach = Intent.GetStringExtra("DIR") ?? string.Empty;
        }
        protected override void OnStart()
        {
            ThreadPool.QueueUserWorkItem(o => DownloadVersion());
            base.OnStart();
        }
        public override bool OnTouchEvent(MotionEvent e)
        {
            return false;
        }
        public override void OnBackPressed()
        {
        }

        private void DownloadVersion()
        {
            try
            {
                RunOnUiThread(() => TextView.Text = "Загрузка обновлений...");
                RunOnUiThread(() => ProgressBar.Max = Convert.ToInt32(bytes));
                ThreadPool.QueueUserWorkItem(o => Version.DownloadFiles(name));
                while (true)
                {
                    Thread.Sleep(100);
                    FileInfo fileInfo = new FileInfo(pach + "/aprs.apk");
                    if (fileInfo.Exists)
                    {
                        long fileSize = fileInfo.Length;
                        if (fileInfo.Length > 0)
                        {
                            if (Convert.ToInt32(fileSize) != Convert.ToInt32(bytes))
                            {
                                RunOnUiThread(() => ProgressBar.Progress = Convert.ToInt32(fileSize));
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                Intent intent = new Intent(Intent.ActionView);
                intent.SetDataAndType(Android.Net.Uri.FromFile(new Java.IO.File(pach + "/aprs.apk")), "application/vnd.android.package-archive");
                intent.SetFlags(ActivityFlags.NewTask);
                this.StartActivity(intent);
                this.Finish();
            }
            catch (Exception)
            {
            }
        }
    }
}