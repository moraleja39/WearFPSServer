using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WearFPSForms.Net {
    static class UdpDataSender {
        private static Thread udpThread;
        private static volatile bool run = false;
        private static UdpClient udpClient;

        private static ManualResetEvent reset = new ManualResetEvent(false);
        
        public struct Packet {
            public IPEndPoint dest;
            public byte type;
            public byte[] data;

            public Packet(IPEndPoint d, byte t, byte[] da) {
                this.dest = d;
                this.type = t;
                this.data = da;
            }
        }
        private static Queue<Packet> packets = new Queue<Packet>();
        private static MemoryStream memStream;

        public static void Start() {
            run = true;

            memStream = new MemoryStream(512);

            udpClient = new UdpClient();

            udpThread = new Thread(new ThreadStart(UdpRunner));
            udpThread.Start();
        }

        public static void Stop() {
            run = false;
            //udpThread.Abort();
            reset.Set();
            udpThread = null;
            udpClient.Close();
            udpClient = null;
        }

        private static void UdpRunner() {
            int dataLength = 0;
            while (run) {
                reset.WaitOne(Timeout.Infinite);
                if (!run) continue;
                lock (packets) {
                    if (packets.Count < 1) {
                        Log.Warn("Udp sender thread notified, but there are no packets awaiting to be sent");
                        continue;
                    }
                    var packet = packets.Dequeue();
                    memStream.SetLength(0);
                    memStream.WriteByte(packet.type);
                    dataLength = packet.data.Length;
                    Log.Debug("Data size: " + dataLength);
                    memStream.WriteByte((byte)(dataLength >> 8));
                    memStream.WriteByte((byte)dataLength);
                    memStream.Write(packet.data, 0, packet.data.Length);


                    udpClient.SendAsync(memStream.ToArray(), (int)memStream.Length, packet.dest);
                }
                reset.Reset();
            }
        }

        public static void Send(byte type, byte[] data, IPEndPoint destination) {
            lock (packets) {
                packets.Enqueue(new Packet(destination, type, data));
            }
            reset.Set();
        }


    }
}
