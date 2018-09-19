using System;
using AppContracts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace AppOne
{
    public partial class App : Application
    {
        public static IPlatformSpecific PlatformSpecific { get; set; }

        public App()
        {
            InitializeComponent();

            var assetContract = App.PlatformSpecific.GetParameter<AssetContract>();
            if (assetContract != null)
            {
                MainPage = new SecondPage();
            }
            else
            {
                MainPage = new MainPage();
            }
            
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
