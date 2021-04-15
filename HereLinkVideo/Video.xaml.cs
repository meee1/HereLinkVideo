
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
                    videoPlayer.Url = "rtsp://192.168.42.10:8554/H264Video";
                    videoPlayer.Play();
                    videoPlayer2.Url = "rtsp://192.168.42.10:8554/H264Video1";
                    videoPlayer2.Play();
                });
                return true;
            });

            Task.Run(() =>
            {
                UdpClient client = new UdpClient();
                client.Connect("192.168.42.11", 14552);
                var stream = new ProxyStream(client);
                var parser = new MAVLink.MavlinkParse();
                var data = MavlinkUtil.StructureToByteArray(new MAVLink.mavlink_heartbeat_t());
                client.Send(data, data.Length);
                Task.Run(() =>
                {
                    IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    while (true)
                    {
                        var dataudp = client.Receive(ref iPEndPoint);
                        stream.InjectData(dataudp);
                    }
                });
                while (true)
                {
                    var msg = parser.ReadPacket(stream);
                    Console.WriteLine(JsonConvert.SerializeObject(msg.data, Formatting.None));
                    if(msg.msgid == (uint)MAVLink.MAVLINK_MSG_ID.RADIO_STATUS)
                    {
                        var rs = (MAVLink.mavlink_radio_status_t)msg.data;
                        var rssi = rs.rssi;
                        var noise = rs.noise;
                        Device.BeginInvokeOnMainThread(() => {
                            label.Text = rssi + " " + noise;
                        });
                    }
                }
            });
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
