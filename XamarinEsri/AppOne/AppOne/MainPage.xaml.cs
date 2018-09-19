using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppContracts;
using Xamarin.Forms;

namespace AppOne
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_OnClicked(object sender, EventArgs e)
        {
            if (int.TryParse(LogHeaderEntry.Text, out int logHeaderId))
            {
                App.PlatformSpecific.StartActivity("com.companyname.AppTwo.Android", new AssetContract() { LogHeaderId = logHeaderId });
            }
        }
    }
}
