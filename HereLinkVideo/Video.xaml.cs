
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
                Device.BeginInvokeOnMainThread(() => {
                    videoPlayer.Url = "rtsp://192.168.0.10:8554/H264Video";
                    videoPlayer.Play();
                    videoPlayer2.Url = "rtsp://192.168.0.10:8554/H264Video1";
                    videoPlayer2.Play();
                });
                return true;
            });
        }
    }
}
