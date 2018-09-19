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
using AppTwo;
using Newtonsoft.Json;

namespace AppTwo.Droid
{
    public class PlatformSpecificImpl : IPlatformSpecific
    {
        private readonly MainActivity mainActivity;

        public PlatformSpecificImpl(MainActivity mainActivity)
        {
            this.mainActivity = mainActivity;
        }

        public T GetParameter<T>()
        {
            Bundle b = mainActivity.Intent.GetBundleExtra("Extra");
            if (b?.GetString(nameof(T)) != null)
            {
                var parameter = b.GetString(nameof(T));
                var parameters = JsonConvert.DeserializeObject<T>(parameter);
                return parameters;
            }
            return default(T);
        }

        public void StartActivity<T>(string componentName, T parameters)
        {
            var intent = mainActivity.PackageManager.GetLaunchIntentForPackage(componentName);
            var bundle = new Bundle();
            var parameter = JsonConvert.SerializeObject(parameters);
            bundle.PutString(nameof(T), parameter);
            intent.PutExtra("Extra", bundle);
            mainActivity.StartActivity(intent);
        }
    }
}