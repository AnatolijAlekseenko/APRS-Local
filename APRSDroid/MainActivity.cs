using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Widget;
using Java.IO;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;
using Android.Runtime;
using Android.Media;
using Android.Webkit;
using Android.Views;
using Android.Database;
using AndroidHUD;
using System.Threading.Tasks;
using Android.Support.V4.App;
using Android;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;

namespace APRSDroid
{
    [Activity(Label = "APRS", /*MainLauncher = true,*/ Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        ImageView imageView;
        View layout;
        int andrSDK;
        static readonly int REQUEST_CAMERA = 0;
        static readonly int REQUEST_STORAGE = 1;

        protected override void OnCreate(Bundle bundle)
        {
            andrSDK = int.Parse(Android.OS.Build.VERSION.Sdk);
            if (andrSDK >= 24)
            {
                StrictMode.VmPolicy.Builder builder = new StrictMode.VmPolicy.Builder();
                StrictMode.SetVmPolicy(builder.Build());
            }

            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);
            layout = FindViewById(Resource.Id.main_layout);

            if (IsThereAnAppToTakePictures())
            {
                CreateDirectoryForPictures();

                // check write storage permission
                var permissionCheck = ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage);
                if (permissionCheck == Permission.Denied)
                    RequestStoragePermission();
                else
                // check update for application
                    Version.StatusVersionApp(App._dir.ToString(), this);


                imageView = FindViewById<ImageView>(Resource.Id.imageView);
                Button btnCamera = FindViewById<Button>(Resource.Id.btnCamera);
                Button btnFile = FindViewById<Button>(Resource.Id.btnFile);
                Button btnSendPic = FindViewById<Button>(Resource.Id.btnSendPic);

                btnCamera.Click += BtnCamera_Click;
                btnFile.Click += BtnFile_Click;
                btnSendPic.Click += BtnSendPic_Click;
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.options_menu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.m_settings)
                M_settings_Click();

            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == REQUEST_CAMERA)
            {
                // Check if the only required permission has been granted
                if (grantResults.Length == 1 && grantResults[0] == Permission.Granted)
                {
                    // Camera permission has been granted, preview can be displayed
                    Toast.MakeText(this, Resource.String.permision_available_camera, ToastLength.Long).Show();
                }
                else
                {
                    Toast.MakeText(this, Resource.String.permissions_not_granted, ToastLength.Long).Show();
                }
            }
            else if (requestCode == REQUEST_STORAGE)
            {
                // Storage if the only required permission has been granted
                if (grantResults.Length == 1 && grantResults[0] == Permission.Granted)
                {
                    // Storage permission has been granted, preview can be displayed
                    Toast.MakeText(this, Resource.String.permision_available_storage, ToastLength.Long).Show();
                    CreateDirectoryForPictures();
                    Version.StatusVersionApp(App._dir.ToString(), this);
                }
                else
                {
                    Toast.MakeText(this, Resource.String.permissions_not_granted, ToastLength.Long).Show();
                }
            }
            else
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
        }

        public static class App
        {
            public static File _file;
            public static File _dir;
            public static Bitmap bitmap;
            public static Uri FilePath;
            public static string FilePathFull;
        }


        private void CreateDirectoryForPictures()
        {
            App._dir = new File(
                Environment.GetExternalStoragePublicDirectory(
                    Environment.DirectoryPictures), "APRSDroid");
            if (!App._dir.Exists())
            {
                var result = App._dir.Mkdirs();
            }
        }

        private bool IsThereAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            // display in ImageView. We will resize the bitmap to fit the display
            // Loading the full sized image will consume to much memory 
            // and cause the application to crash.
            int height = Resources.DisplayMetrics.HeightPixels;
            int width = Resources.DisplayMetrics.WidthPixels;


            if (App._file != null && resultCode == Result.Ok)
            {
                Bitmap bitmap = App._file.Path.LoadAndResizeBitmap(width, height);

                imageView.SetImageBitmap(bitmap);
            }
            else if(data != null && data.Data != null)
            {
                if (!data.Data.ToString().Contains(@"file:///storage/emulated/"))
                {
                    App.FilePath = data.Data;
                    App.FilePathFull = GetFilePath(data.Data);

                    if (App.FilePathFull == null)
                    {
                        App.FilePathFull = GetRealPathFromURI(data.Data);
                        if (App.FilePathFull == null) return;
                        App.FilePath = Uri.Parse(App.FilePathFull);
                    }
                }
                else
                {
                    App.FilePathFull = data.Data.ToString().Replace(@"file://","");
                    App.FilePath = Uri.Parse(App.FilePathFull);
                }

                Bitmap bitmap = App.FilePathFull.LoadAndResizeBitmap(width, height);
                bitmap = scaleDown(bitmap, width, false);
                imageView.SetImageBitmap(bitmap);
            }

            // Dispose of the Java side bitmap.
            GC.Collect();
        }

        /// <summary>
        /// Открыть камеру, сделать фото и вернуть его для обработки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCamera_Click(object sender, EventArgs e)
        {
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != (int)Permission.Granted)
            {
                // Camera permission has not been granted
                RequestCameraPermission();
                return;
            }

            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
            {
                // Storage permission has not been granted
                RequestStoragePermission();
                return;
            }


            App.FilePath = null;

            Intent intent = new Intent(MediaStore.ActionImageCapture);
            App._file = new File(App._dir, String.Format("aprs_{0}.jpg", Guid.NewGuid()));

            var andrVers = int.Parse(Android.OS.Build.VERSION.Sdk);

            if (andrVers >= 24)
            {

                intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                intent.AddFlags(ActivityFlags.GrantWriteUriPermission);
                intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(App._file) );
                intent.PutExtra(MediaStore.ExtraSizeLimit, 1024);
                StartActivityForResult(intent, 0);
            }
            else
            {
                intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(App._file));
                intent.PutExtra(MediaStore.ExtraSizeLimit, 1024);
                StartActivityForResult(intent, 0);
            }
        }

        /// <summary>
        /// Запрос на доступ к Камере
        /// </summary>
        void RequestCameraPermission()
        {
            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.Camera))
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.Camera }, REQUEST_CAMERA);
            }
            else
            {
                // Camera permission has not been granted yet. Request it directly.
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.Camera }, REQUEST_CAMERA);
            }
        }


        /// <summary>
        /// Выбрать существующий файл для распознования
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnFile_Click(object sender, EventArgs e)
        {
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
            {
                // Storage permission has not been granted
                RequestStoragePermission();
                return;
            }

            App._file = null;

            Intent imageIntent = new Intent();
            imageIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
            imageIntent.AddFlags(ActivityFlags.GrantWriteUriPermission);
            imageIntent.SetType("image/*");
            imageIntent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(Intent.CreateChooser(imageIntent, "Select photo"), 0);

        }

        /// <summary>
        /// Запрос на доступ к хранилищу
        /// </summary>
        void RequestStoragePermission()
        {
            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.ReadExternalStorage))
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadExternalStorage }, REQUEST_STORAGE);
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.ReadExternalStorage }, REQUEST_STORAGE);
            }
        }

        /// <summary>
        /// Отправить файл на сервер для распознования
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSendPic_Click(object sender, EventArgs e)
        {
            if (App._file != null)
            {
                //AndHUD.Shared.Show(this, GetString(Resource.String.loadig), -1, MaskType.Clear);
                AndHUD.Shared.ShowSuccess(this, GetString(Resource.String.loadig), MaskType.Clear);
                StartWebActivity(App._file.Path);
            }
            else if (App.FilePathFull != null)
            {
                //AndHUD.Shared.Show(this, GetString(Resource.String.loadig), -1, MaskType.Clear);
                AndHUD.Shared.ShowSuccess(this, GetString(Resource.String.loadig), MaskType.Clear);
                StartWebActivity(App.FilePathFull);
            }
            else
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetMessage(Resource.String.empty_photo);
                alert.SetTitle(Resource.String.warning);
                alert.SetPositiveButton("Ok", (senderAlert, args) => {
                    //change value write your own set of instructions
                    //you can also create an event for the same in xamarin
                    //instead of writing things here
                });


                RunOnUiThread(() => {
                    alert.Show();
                });
            }
          
        }

        /// <summary>
        /// Переход на WebView с результатом распознования
        /// </summary>
        /// <param name="p_path"></param>
        private void StartWebActivity(string p_path)
        {
            Intent webActivity = new Intent(this, typeof(APRSDroid.WebViewer));
            webActivity.PutExtra("filePath", p_path);
            StartActivity(webActivity);
        }


        /// <summary>
        /// Полчить реальный путь к файлу для Android до 5.5
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private string GetFilePath(Android.Net.Uri uri)
        {
            if (uri == null) return null;

            string[] proj = { MediaStore.Images.ImageColumns.Data };

            var cursor = ContentResolver.Query(uri, proj, null, null, null);
            var colIndex = cursor.GetColumnIndex(MediaStore.Images.ImageColumns.Data);
            cursor.MoveToFirst();
            return cursor.GetString(colIndex);
        }

        /// <summary>
        /// Полчить реальный путь к файлу для Android с 5.5
        /// </summary>
        /// <param name="contentURI"></param>
        /// <returns></returns>
        private string GetRealPathFromURI(Uri contentURI)
        {
            if (contentURI == null) return null;

            ICursor cursor = ContentResolver.Query(contentURI, null, null, null, null);
            cursor.MoveToFirst();
            string documentId = cursor.GetString(0);
            documentId = documentId.Split(':')[1];
            cursor.Close();

            cursor = ContentResolver.Query(
            Android.Provider.MediaStore.Images.Media.ExternalContentUri,
            null, MediaStore.Images.Media.InterfaceConsts.Id + " = ? ", new[] { documentId }, null);
            cursor.MoveToFirst();
            string path = cursor.GetString(cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Data));
            cursor.Close();

            return path;
        }

        /// <summary>
        /// Уменьшение размера фото для старых телефонов
        /// </summary>
        /// <param name="realImage"></param>
        /// <param name="maxImageSize"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static Bitmap scaleDown(Bitmap realImage, float maxImageSize, bool filter)
        {
            float ratio = Math.Min(
                    (float)maxImageSize / realImage.Width,
                    (float)maxImageSize / realImage.Height);
            int width = (int)Math.Round((float)ratio * realImage.Width);
            int height = (int)Math.Round((float)ratio * realImage.Height);

            Bitmap newBitmap = Bitmap.CreateScaledBitmap(realImage, width, height, filter);
            return newBitmap;
        }

        /// <summary>
        /// Переход к Настройкам приложения
        /// </summary>
        private void M_settings_Click()
        {
            Intent settView = new Intent(this, typeof(APRSDroid.SettingsView));
            StartActivity(settView);
        }

    }

}

