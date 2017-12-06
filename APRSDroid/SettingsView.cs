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

namespace APRSDroid
{
    [Activity(Label = "SettingsView")]
    public class SettingsView : Activity
    {
        private ListView listnames;
        private List<string> itemlist;
        public static string CascadeSaveFileName;
        int positionCascade;

        protected async override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.SettingsLayout);

            ActionBar.Title = GetString(Resource.String.m_settings);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowHomeEnabled(true);

            Button btn_save = FindViewById<Button>(Resource.Id.btn_save);
            btn_save.Click += Btn_save_Click;

            Context mContext = Android.App.Application.Context;
            AppPreferences ap = new AppPreferences(mContext);

            var connString = FindViewById<AutoCompleteTextView>(Resource.Id.connString);
            connString.Text = ap.getURL_String();

            //Alekseenko-//////////////////////////////////////////////////////////////////////////
            //return при повторном входе
            listnames = FindViewById<ListView>(Resource.Id.listviewFiles);
            Context mContextCascade = Android.App.Application.Context;
            AppPreferences apCascade = new AppPreferences(mContextCascade);
            var position = apCascade.getCascade_Position();
            listnames.SetItemChecked(position, true);    ////////////////Устанавливает позицию 0 или новую после сохранения
            listnames.ChoiceMode = ChoiceMode.Single;
            itemlist = new List<string>();

            //вытягиваем и записываем файлы каскадов из папки files

            var get_path = Application.Context.GetExternalFilesDir(null).AbsolutePath;
            string[] filename = Directory.GetFiles(get_path, "*.xml");
            List<string> lst = new List<string>(filename);
            foreach (string cascade in lst)
            {
                FileInfo nameFile = new FileInfo(cascade);
                itemlist.Add(nameFile.Name);
            }

            ArrayAdapter<string> adapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleListItemSingleChoice, itemlist);
            listnames.Adapter = adapter;
            listnames.SetItemChecked(position, true);
            listnames.ChoiceMode = ChoiceMode.Single;
            listnames.ItemClick += Listnames_ItemClick;
            //////////////////////////////////////////////////////////////////////////

        }

        private void Listnames_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            //Выбор необходимого каскада.
            ListView lv = FindViewById<ListView>(Resource.Id.listviewFiles);
            lv.SetItemChecked(e.Position, true);
            lv.ChoiceMode = ChoiceMode.Single;
            CascadeSaveFileName = lv.GetItemAtPosition(e.Position).ToString();
            positionCascade = e.Position;
            string filePath = Intent.GetStringExtra("filePath");
            Toast.MakeText(this, "Выбран каскад: " + lv.GetItemAtPosition(e.Position), ToastLength.Short).Show();
            
        }



        private void Btn_save_Click(object sender, EventArgs e)
        {
            var connString = FindViewById<AutoCompleteTextView>(Resource.Id.connString);

            Context mContext = Android.App.Application.Context;
            AppPreferences ap = new AppPreferences(mContext);

            //Сохранение выбраного каскада
            Context mContextCascade = Android.App.Application.Context;
            AppPreferences apCascade = new AppPreferences(mContextCascade);
            apCascade.saveCascade_String(positionCascade);

            ap.saveURL_String(connString.Text);
            this.Finish();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId != Android.Resource.Id.Home)
                return base.OnOptionsItemSelected(item);

            Finish();
            // hhhh
            return base.OnOptionsItemSelected(item);
        }
    }
}