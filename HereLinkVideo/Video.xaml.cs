
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FormsVideoLibrary;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Zeroconf;

namespace HereLinkVideo
{
    public partial class Video : ContentPage
    {
        internal bool run;

        public static string DeviceID = "";

        public Video()
        {
            InitializeComponent();
        }

        internal void Init()
        {
            // when running on gcs unit
            string ip = "192.168.0.10";
            bool herelink = false;
            bool ipset = false;

            Device.StartTimer(TimeSpan.FromMilliseconds(3000), () =>
            {
                // do a scan
                if (!ipset && !herelink)
                {
                    if (new Ping().Send("192.168.0.11", 2000).Status == IPStatus.Success &&
                        new Ping().Send("192.168.0.10", 2000).Status == IPStatus.Success)
                    {
                        Console.WriteLine("RSTP is a herelink set ip to 192.168.0.10");
                        herelink = true;
                        ip = "192.168.0.10";
                    }

                    if(!herelink) { 
                        ZeroconfResolver.Resolve("_mavlink._udp.local.").Subscribe(host =>
                        {
                            if (DeviceID != "" && host.DisplayName.Contains(DeviceID))
                            { 
                                Console.WriteLine("RTSP Detected self - ignoreing");
                                return;
                            }

                            if (!herelink && !ipset)
                            {
                                Console.WriteLine("RSTP  zeroconf set ip to " + host.IPAddress);
                                ip = host.IPAddress;
                                ipset = true;
                            }
                        });

                        return true;
                    }
                }

                // scan has detected something, play it
                Device.BeginInvokeOnMainThread(() =>
                {
                    try { 
                        if (herelink)
                        {
                            videoPlayer.Url = "rtsp://" + ip + ":8554/H264Video";
                            videoPlayer.Play();
                            videoPlayer2.Url = "rtsp://" + ip + ":8554/H264Video1";
                            videoPlayer2.Play();
                        }
                        else if (ipset)
                            {
                                videoPlayer.Url = "rtsp://" + ip + ":8554/fpv_stream";
                            videoPlayer.Play();
                                videoPlayer2.Url = "rtsp://" + ip + ":8554/fpv_stream1";
                                videoPlayer2.Play();
                        }
                        

                        if (videoPlayer.Status == VideoStatus.Playing)
                            v1.IsVisible = true;
                        else
                            v1.IsVisible = false;


                        if (videoPlayer2.Status == VideoStatus.Playing)
                            v2.IsVisible = true;
                        else
                            v2.IsVisible = false;
                    } 
                    catch (Exception ex)
                    {
                        Console.WriteLine("RTSP "+ex);
                    }
                });
                return run;
            });


            Task.Run(() =>
            {
                while (!ipset && !herelink)
                    Thread.Sleep(100);

                UdpClient client;
                // bind
                if (herelink)
                {
                    client = new UdpClient(14551);
                }
                else
                {
                    client = new UdpClient();
                    client.Connect(ip, 14552);
                    var data = new byte[1];
                    client.Send(data, data.Length);
                }

                var stream = new ProxyStream(client);
                var parser = new MAVLink.MavlinkParse();
                Task.Run(() =>
                {
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    while (run)
                    {
                        var dataudp = client.Receive(ref iPEndPoint);
                        client.Connect(iPEndPoint);
                        stream.InjectData(dataudp);
                    }

                    client?.Close();
                });
                while (run)
                {
                    var msg = parser.ReadPacket(stream);
                    //Console.WriteLine(JsonConvert.SerializeObject(msg.data, Formatting.None));
                    if (msg.msgid == (uint)MAVLink.MAVLINK_MSG_ID.RADIO_STATUS)
                    {
                        var rs = (MAVLink.mavlink_radio_status_t)msg.data;
                        var rssi = rs.rssi;
                        var noise = rs.noise;
                    }

                    if (msg.msgid == (uint)MAVLink.MAVLINK_MSG_ID.STATUSTEXT)
                    {
                        Console.WriteLine(ASCIIEncoding.ASCII.GetString(((MAVLink.mavlink_statustext_t)msg.data).text));
                    }
                }
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            run = true;
            Init();
        }

        protected override void OnDisappearing()
        {
            run = false;
            base.OnDisappearing();
        }
    }

    public class ProxyStream : Stream
    {
        private long _length;

        MemoryStream ms = new MemoryStream();
        private long _position;

        public ProxyStream(UdpClient client)
        {
            Client = client;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _length;

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {

            }
        }

        public UdpClient Client { get; }

        public override void Flush()
        {
           
        }

        public override int Read(byte[] buffer, int offset, int count)
        {

            while (true)
            {
                if (Position + count < Length)
                    break;
                Thread.Sleep(1);
            }

            lock (ms)
            {
                var read = ms.Read(buffer, offset, count);
                _position += read;
                return read;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Position;
        }

        public override void SetLength(long value)
        {
           
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Client.Send(buffer.Skip(offset).ToArray(), count);
        }

        public void InjectData(byte[] data)
        {
            lock(ms)
            {
                _length += data.Length;
                if (ms.Length > 0 && ms.Position == ms.Length)
                    ms.SetLength(0);
                var pos = ms.Position;
                ms.Seek(0, SeekOrigin.End);
                ms.Write(data, 0, data.Length);
                ms.Position = pos;
            }
        }
    }
}
