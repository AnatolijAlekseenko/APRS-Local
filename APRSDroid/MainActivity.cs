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
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;
using FaceDetection;
using System.Linq;
using APRSDroid.CropImg;

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

                CreateDefaultCascade();

                imageView = FindViewById<ImageView>(Resource.Id.imageView);
                Button btnCrop = FindViewById<Button>(Resource.Id.btnCrop);
                Button btnCamera = FindViewById<Button>(Resource.Id.btnCamera);
                Button btnFile = FindViewById<Button>(Resource.Id.btnFile);
                Button btnSendPic = FindViewById<Button>(Resource.Id.btnSendPic);
                Button btnSendLoc = FindViewById<Button>(Resource.Id.btnLocal);

                btnCrop.Click += BtnCrop_Click;
                btnCamera.Click += BtnCamera_Click;
                btnFile.Click += BtnFile_Click;
                btnSendPic.Click += BtnSendPic_Click;
                btnSendLoc.Click += BtnSendLoc_Click;
            }
        }

        public Bitmap BtmpFact(string picture)
        {
            BitmapFactory.Options options;
            Bitmap bitmap;

            try
            {
    
                bitmap = BitmapFactory.DecodeFile(picture);
                return bitmap;
            }
            catch (Java.Lang.OutOfMemoryError e)
            {
                try
                {
                    options = new BitmapFactory.Options();
                    options.InSampleSize = 2;
                    bitmap = BitmapFactory.DecodeFile(picture, options);
                    return bitmap;
                }
                catch (Exception ee)
                {
                    Toast.MakeText(this, "Ошибка в BtmpFact: " + ee, ToastLength.Short).Show();
                    //return bitmap;
                }
                
            }
            return bitmap=null;

        }

        public static void loadLibrary(string libName)
        {
            try
            {
                Java.Lang.Runtime.GetRuntime().LoadLibrary(libName);
            }
            catch (Exception eee)
            {
                Android.Util.Log.Debug("Ошибка Runtime", eee.ToString());

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

        public static class App
        {
            public static File _file;
            public static File _dir;
            public static File _APRSDir;
            public static Bitmap bitmap;
            public static Uri FilePath;
            public static string FilePathFull;

            public const string def_cascade = "def-cascade.xml";
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

        /// <summary>
        /// Создать на устройстве файл def-cascade
        /// </summary>
        private async void CreateDefaultCascade()
        {
            var permissionCheck = ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage);
            if (permissionCheck == Permission.Denied)
                return;


                App._APRSDir = new File(Environment.GetExternalStoragePublicDirectory(Environment.DataDirectory.Parent), "APRSDroid");
            
            if (!App._APRSDir.Exists())
            {
                var result = App._APRSDir.Mkdirs();
            }

            var get_path_files = App._APRSDir.AbsolutePath;
            if (!System.IO.File.Exists(string.Format("{0}/{1}", get_path_files, App.def_cascade)))
            {
                try
                {
                    var localFolder = get_path_files;
                    var MyFilePath = System.IO.Path.Combine(localFolder, App.def_cascade);

                    using (var streamReader = new System.IO.StreamReader(Assets.Open(App.def_cascade)))
                    {
                        using (var memstream = new System.IO.MemoryStream())
                        {
                            streamReader.BaseStream.CopyTo(memstream);
                            var bytes = memstream.ToArray();
                            //write to local storage
                            System.IO.File.WriteAllBytes(MyFilePath, bytes);

                            MyFilePath = $"file://{localFolder}/{App.def_cascade}";

                        }
                    }
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, string.Format("Error def-cascade:{0}", ex.Message), ToastLength.Long).Show();
                }
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

            if(requestCode == 0)
            {
                if (App._file != null && resultCode == Result.Ok)
                {
                    Bitmap bitmap = App._file.Path.LoadAndResizeBitmap(width, height);

                    imageView.SetImageBitmap(bitmap);
                }
                else if (data != null && data.Data != null)
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
                        App.FilePathFull = data.Data.ToString().Replace(@"file://", "");
                        App.FilePath = Uri.Parse(App.FilePathFull);
                    }

                    Bitmap bitmap = App.FilePathFull.LoadAndResizeBitmap(width, height);
                    bitmap = scaleDown(bitmap, width, false);
                    imageView.SetImageBitmap(bitmap);
                }
            }
            else if(requestCode == 1)
            {
                string filePath = GetImgPathWithOutMessage();

                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    //Uri pathUri = Android.Net.Uri.Parse(filePath);
                    //imageView.SetImageURI(pathUri);

                    Bitmap bitmap = filePath.Resize(width, height);
                    imageView.SetImageBitmap(bitmap);
                }
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
        /// Вызвать интерфейс обрезки изображения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCrop_Click(object sender, EventArgs e)
        {
            string imgPath = GetImgPath();

            if(!string.IsNullOrEmpty(imgPath))
            {
                Intent intent = new Intent(this, typeof(CropImage));
                intent.PutExtra("image-path", imgPath);
                intent.PutExtra("scale", true);
                //StartActivity(intent);
                StartActivityForResult(intent, 1);
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

            CreateDefaultCascade();
        }

        /// <summary>
        /// Отправить файл на сервер для распознования
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSendPic_Click(object sender, EventArgs e)
        {
            string filePath = GetImgPath();

            if (!string.IsNullOrEmpty(filePath))
            {
                AndHUD.Shared.ShowSuccess(this, GetString(Resource.String.loadig), MaskType.Clear);
                StartWebActivity(filePath);
            }
        }

        /// <summary>
        /// Распознать изображение offline
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSendLoc_Click(object sender, EventArgs e)
        {
            var p_cascade = APRSDroid.SettingsView.CascadeSaveFileName;

            // получаем путь к файлу для распознования
            string filePath = GetImgPath();

            // проверка существования файла для распознования
            if (string.IsNullOrEmpty(filePath)) return;

            //Если каскад не пустой
            if (!String.IsNullOrEmpty(p_cascade))
            {
                try
                {

                    GC.Collect();
                    Toast.MakeText(this, string.Format("Используется Каскад:{0}", p_cascade), ToastLength.Short).Show();
                    Recognize(filePath, p_cascade);

                }
                catch (Exception error)
                {
                    Toast.MakeText(this, "Error recognize: " + error, ToastLength.Short).Show();
                    Android.Util.Log.Debug("Вот ошибка", error.ToString());
                }
            }
            else
            {

                try
                {
                    GC.Collect();
                    if (!CheckDefCascadeExists()) return;
                    Toast.MakeText(this, "Используется Каскад по умолчанию", ToastLength.Short).Show();
                    Recognize(filePath, App.def_cascade);
                }
                catch (Exception error)
                {
                    Toast.MakeText(this, "Error recognize: " + error.Message, ToastLength.Long).Show();
                    Android.Util.Log.Debug("Вот ошибка", error.ToString());
                }

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
        /// Метод разпознования объектов
        /// </summary>
        /// <param name="filenameForRecognize"></param>
        /// <param name="cascade"></param>
        private void Recognize(string filenameForRecognize, string cascade)
        {
            string filename = string.Format("{0}/{1}", App._APRSDir, cascade);

            long time;

            List<Rectangle> tubes = new List<Rectangle>();
            List<Rectangle> old = new List<Rectangle>();
            List<double> areaList = new List<double>();
            CircleF circle = new CircleF();
            int counter = 0;
            int i = 1; //counter tubes
            MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_COMPLEX_SMALL, 1.0, 1.0);


            var ArchFile = filenameForRecognize.LoadAndResizeBitmap(1024, 1024);
            //Haar cascade
            var FileForRecognize = new Image<Bgr, byte>(ArchFile);
            DetectFace.Detect(FileForRecognize, filename, filename, tubes, old, out time);
            double AVGarea = 0.00;
            foreach (Rectangle tube in tubes)
            {
                var area = (3.14) * tube.Width * tube.Height;
                areaList.Add(area);
            }
            try
            {
                AVGarea = areaList.Average();
            }
            catch (Exception nullObjDetect)
            {
                Toast.MakeText(this, "Нет найденых объектов!!!", ToastLength.Short).Show();
            }
            foreach (var tube in tubes.OrderBy(s => s.X).ThenBy(u => u.Y))
            {
                System.Drawing.Point point = new System.Drawing.Point(tube.X + tube.Width / 3, tube.Y + tube.Height / 2);
                circle.Center = new System.Drawing.PointF(tube.X + tube.Width / 2, tube.Y + tube.Height / 2);
                circle.Radius = tube.Width / 2;
                var area = (3.14) * tube.Width * tube.Height;
                if (area / AVGarea * 100 <= 40) // меньше или равно 20 % от среднего по детектируемым объектам - не выводить в детектор
                    continue;
                counter = i++;
                if (FileForRecognize.Width <= 1024 && FileForRecognize.Height <= 768)
                {
                    FileForRecognize.Draw(circle, new Bgr(System.Drawing.Color.Yellow), 1);
                    FileForRecognize.Draw(string.Format("{0}", counter), ref font, point, new Bgr(System.Drawing.Color.Red));
                }
                else
                {
                    FileForRecognize.Draw(circle, new Bgr(System.Drawing.Color.Yellow), 7);
                    FileForRecognize.Draw(string.Format("{0}", counter), ref font, point, new Bgr(System.Drawing.Color.Red));
                }
            }
            //Toast.MakeText(this, "Количество: " + counter + "  Затрачено времени: " + time, ToastLength.Long).Show();
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle("Подтверждение");
            alert.SetMessage(string.Format("Распознано объектов: {0} , " +
                "                           Время распознавания: {1}", counter.ToString(), time.ToString()));

            alert.SetPositiveButton("Подтверждение", (senderAlert, args) => { Toast.MakeText(this, "Подтверждено!", ToastLength.Short).Show(); });
            RunOnUiThread(() => { alert.Show(); });
            imageView.SetImageBitmap(FileForRecognize.ToBitmap());

            GC.Collect();

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

        /// <summary>
        /// Получить путь в файлу
        /// </summary>
        /// <returns></returns>
        private string GetImgPath()
        {
            string filePath = string.Empty;

            // Проверка пути к файлу
            if (App._file != null)
            {
                filePath = App._file.Path;
            }
            else if (App.FilePathFull != null)
            {
                filePath = App.FilePathFull;
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

            return filePath;
        }

        /// <summary>
        /// Получить путь в файлу без окна предупреждения
        /// </summary>
        /// <returns></returns>
        private string GetImgPathWithOutMessage()
        {
            string filePath = string.Empty;

            // Проверка пути к файлу
            if (App._file != null)
            {
                filePath = App._file.Path;
            }
            else if (App.FilePathFull != null)
            {
                filePath = App.FilePathFull;
            }

            return filePath;
        }

        /// <summary>
        /// Проврека наличия каскада по-умолчанию
        /// </summary>
        /// <returns></returns>
        private bool CheckDefCascadeExists()
        {
            bool result = true;

            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
            {
                // Storage permission has not been granted
                RequestStoragePermission();
                return false;
            }

            string filename = string.Format("{0}/{1}", App._APRSDir, App.def_cascade);

            try
            {
                if (!System.IO.File.Exists(filename))
                {
                    AlertDialog.Builder alert = new AlertDialog.Builder(this);
                    alert.SetMessage(Resource.String.defCasNotExst);
                    alert.SetTitle(Resource.String.warning);
                    alert.SetPositiveButton("Ok", (senderAlert, args) => {
                        //change value write your own set of instructions
                        //you can also create an event for the same in xamarin
                        //instead of writing things here
                    });


                    RunOnUiThread(() => {
                        alert.Show();
                    });

                    result = false;
                }
            }
            catch (Exception ex)
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetMessage(ex.Message);
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

            return result;
        }

    }

}

