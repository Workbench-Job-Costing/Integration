using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppContracts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AppOne
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SecondPage : ContentPage
	{
	    public string AssetId { get; set; }

		public SecondPage ()
		{
			InitializeComponent ();
		}

	    protected override void OnAppearing()
	    {
            base.OnAppearing();

	        var parameters = App.PlatformSpecific.GetParameter<AssetContract>();
	        if (parameters != null)
	            AssetId = parameters.AssetId;
	        else
	            AssetId = "Empty";

	        AssetIdLabel.Text = AssetId;
	    }
	}
}