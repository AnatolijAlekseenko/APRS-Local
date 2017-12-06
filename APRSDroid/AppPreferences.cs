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
using Android.Preferences;

namespace APRSDroid
{
    public class AppPreferences
    {
        private ISharedPreferences nameSharedPrefs;
        private ISharedPreferencesEditor namePrefsEditor; //Declare Context,Prefrences name and Editor name  
        private Context mContext;
        private static String URL_String = "URL_String"; //Value Access Key Name  

        /// <summary>
        /// //////////////////////////Алексеенко - save Cascade
        /// </summary>
        private ISharedPreferencesEditor namePrefsEditorCascade; //Declare Context,Prefrences name and Editor name  
        private static string Cascade_Position = "Cascade_Position"; //Value Access Key Name 



        public AppPreferences(Context context)
        {
            this.mContext = context;
            nameSharedPrefs = PreferenceManager.GetDefaultSharedPreferences(mContext);
            namePrefsEditor = nameSharedPrefs.Edit();
            namePrefsEditorCascade = nameSharedPrefs.Edit();
        }

        public void saveURL_String(string key) // Save data Values  
        {
            namePrefsEditor.PutString(URL_String, key);
            namePrefsEditor.Commit();
        }


        public void saveCascade_String(int key) // Save data Values  
        {
            namePrefsEditorCascade.PutInt(Cascade_Position, key);
            namePrefsEditorCascade.Commit();
        }


        public string getURL_String() // Return Get the Value  
        {
            return nameSharedPrefs.GetString(URL_String, "http://192.168.105.85/" /*@"http://alekseenko-001-site1.ftempurl.com/"*/);
        }

        public int getCascade_Position() // Return Get the Value Cascade
        {
            return nameSharedPrefs.GetInt(Cascade_Position, 0); //по Default будет каскад первый в списке
        }

    }
}