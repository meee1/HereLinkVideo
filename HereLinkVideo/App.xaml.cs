
using System;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace HereLinkVideo
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            MainPage = new Video();
        }

        protected override void OnStart()
        {
            Debug.WriteLine("OnStart");
        }

        protected override void OnSleep()
        {
            Debug.WriteLine("OnSleep"); 
        }

        protected override void OnResume()
        {
            Debug.WriteLine("OnResume");
        }
    }
}
