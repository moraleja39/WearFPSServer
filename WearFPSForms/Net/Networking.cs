using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WearFPSForms.Net;
using Google.Protobuf;

namespace WearFPSForms {
    static class Networking {
        private static UdpBroadcastReceiver udpBroadcastReceiver;
        private static TcpServer tcpServer;

        public static void findLocalIP() {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics) {
                foreach (var x in adapter.GetIPProperties().UnicastAddresses) {
                    if (x.Address.AddressFamily == AddressFamily.InterNetwork && x.IsDnsEligible) {
                        LocalInterfaces.Add(x.Address.ToString(), x.IPv4Mask.ToString(), adapter.Name);
                    }
                }
            }
        }

        public static void start() {
            findLocalIP();
            udpBroadcastReceiver = new UdpBroadcastReceiver();
            udpBroadcastReceiver.start();
            tcpServer = new TcpServer();
            tcpServer.Start();
            UdpDataSender.Start();
        }

        public static void stop() {
            if (udpBroadcastReceiver != null) udpBroadcastReceiver.stop();
            udpBroadcastReceiver = null;
            if (tcpServer != null) tcpServer.Stop();
            tcpServer = null;
            Clients.StopAll();
            UdpDataSender.Stop();
        }


    }
}
