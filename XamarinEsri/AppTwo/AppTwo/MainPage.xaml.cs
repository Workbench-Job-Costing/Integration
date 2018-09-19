using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppContracts;
using Xamarin.Forms;

namespace AppTwo
{
    public partial class MainPage : ContentPage
    {
        public int LogHeaderId { get; set; }

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            var parameters = App.PlatformSpecific.GetParameter<AssetContract>();
            if (parameters != null)
                LogHeaderId = parameters.LogHeaderId;
            else
                LogHeaderId = -1;

            ParamLabel.Text = LogHeaderId.ToString();

            base.OnAppearing();
        }

        private void Button_OnClicked(object sender, EventArgs e)
        {
            var asset = new AssetContract()
            {
                AssetId = "A123",
                LogHeaderId = LogHeaderId,
                AssetAttributes = new List<AssetAttributeContract>()
                {
                    new AssetAttributeContract()
                    {
                        AttributeType = AssetAttributeType.Text,
                        Key = "Diameter",
                        Value = "100mm", // not sure if 100mm or Diameter1 here
                        ValidValues = new List<AssetAttributeValueContract>()
                        {
                            new AssetAttributeValueContract()
                            {
                                Value = "Diameter1",
                                Label = "100mm"
                            },
                            new AssetAttributeValueContract()
                            {
                                Value = "Diameter2",
                                Label = "150mm"
                            },
                            new AssetAttributeValueContract()
                            {
                                Value = "Diameter3",
                                Label = "200mm"
                            },
                        }
                    }
                }
            };

            App.PlatformSpecific.StartActivity("com.companyname.AppOne.Android", asset);
        }
    }
}
