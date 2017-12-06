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
using Android.Webkit;
using static APRSDroid.MainActivity;
using AndroidHUD;

namespace APRSDroid
{

    [Activity(Label = "WebViewer")]
    public class WebViewer : Activity
    {
        WebView webView;

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            AndHUD.Shared.Dismiss();

            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.ViewWebSite);

            ActionBar.Title = "APRS";
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowHomeEnabled(true);

            string filePath = Intent.GetStringExtra("filePath");

            string resultURL = string.Empty;
            string error_val = "Server not respone!";
            try
            {
                resultURL = await WebRequest.SendPostFile(filePath);
            }
            catch (Exception ex)
            {
                error_val = ex.ToString();
            }


            if (resultURL == string.Empty)
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetMessage(error_val);
                alert.SetTitle("Error!");
                alert.SetPositiveButton("Ok", (senderAlert, args) =>
                {
                    //change value write your own set of instructions
                    //you can also create an event for the same in xamarin
                    //instead of writing things here
                });

                RunOnUiThread(() => {
                    alert.Show();
                });
            }
            else
            {
                Context mContext = Android.App.Application.Context;
                AppPreferences ap = new AppPreferences(mContext);

                webView = FindViewById<WebView>(Resource.Id.webView);
                webView.SetWebViewClient(new ExtentWebViewClient());
                webView.LoadUrl(ap.getURL_String() + resultURL);

                WebSettings wset = webView.Settings;
                wset.JavaScriptEnabled = true;
            }
           
        }


        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId != Android.Resource.Id.Home)
                return base.OnOptionsItemSelected(item);

            Finish();

            return base.OnOptionsItemSelected(item);
        }
    }

    internal class ExtentWebViewClient : WebViewClient
    {

        public override bool ShouldOverrideUrlLoading(WebView view, string url)
        {
            view.LoadUrl(url);

            return true;
        }

    }
}