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
using System.Threading.Tasks;
using Android.Util;

namespace APRSDroid
{
    [Activity(MainLauncher = true, Theme = "@style/Splash", NoHistory = true)]
    public class SplashActivity : Activity
    {
        protected override void OnResume()
        {
            base.OnResume();

            Task startupWork = new Task(() => { SimulateStartup(); });
            startupWork.Start();
        }

        static readonly string TAG = "X:" + typeof(SplashActivity).Name;
        async void SimulateStartup()
        {
            await Task.Delay(3000); 
            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }

    }
}