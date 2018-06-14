#define OFFLINE_SYNC_ENABLED

using System;
using Windows.UI.Xaml.Navigation;
using Microsoft.WindowsAzure.MobileServices;

namespace MobileApp.Client.Views
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Uri uri)
            {
                App.MobileClient.ResumeWithURL(uri);
            }
        }
    }
}
