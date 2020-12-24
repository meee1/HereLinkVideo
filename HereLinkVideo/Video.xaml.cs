
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace HereLinkVideo
{
    public partial class Video : ContentPage
    {
        public Video()
        {
            InitializeComponent();

            Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
            {
                Device.BeginInvokeOnMainThread(() => { videoPlayer.Play(); });
                return true;
            });
        }
    }
}
