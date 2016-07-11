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
        private static String localIP = null;
        private static String localSubnetMaskt = null;

        private static UdpBroadcastReceiver udpBroadcastReceiver;

        private static TcpListener tcpServer;
        private static int tcpPort = 55633;
        private static Thread tcpThread;
        private volatile static bool runTcpServer;

        private static volatile int freq = 200;

        public static void findLocalIP() {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics) {
                foreach (var x in adapter.GetIPProperties().UnicastAddresses) {
                    if (x.Address.AddressFamily == AddressFamily.InterNetwork && x.IsDnsEligible) {
                        Log.Info(String.Format(" IPAddress ........ : {0:x}", x.Address.ToString()));
                        if (localIP == null) {
                            localIP = x.Address.ToString();
                            localSubnetMaskt = x.IPv4Mask.ToString();
                        }
                    }
                }
            }
        }

        public static void start() {
            findLocalIP();
            udpBroadcastReceiver = new UdpBroadcastReceiver(localIP);
            udpBroadcastReceiver.start();
            startTcpListener();
        }

        public static void stop() {
            if (udpBroadcastReceiver != null) udpBroadcastReceiver.stop();
            udpBroadcastReceiver = null;
            stopTcpListener();
        }

        public static void startTcpListener() {
            runTcpServer = true;

            //findLocalIP();
            //long ip = long.Parse(localIP);
            //long snm = long.Parse(localSubnetMaskt);
            byte[] locIpBytes = IPAddress.Parse(localIP).GetAddressBytes();
            byte[] maskIpBytes = IPAddress.Parse(localSubnetMaskt).GetAddressBytes();
            byte[] ip = new byte[locIpBytes.Length];
            for (int i = 0; i < locIpBytes.Length; i++) {
                ip[i] = (byte)(locIpBytes[i] & maskIpBytes[i]);
            }
            IPAddress local = new IPAddress(ip);

            bool run = true;

            tcpServer = new TcpListener(IPAddress.Any, tcpPort);
            tcpServer.Start();

            tcpThread = new Thread(new ThreadStart(() => {
                Log.Info("Servidor TCP inicializado correctamente");
                while (run) {
                    try {
                        TcpClient client = tcpServer.AcceptTcpClient();
                        client.NoDelay = true;
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));

                        clientThread.Start(client);
                    } catch (Exception e) {
                        Log.Error("Exception: " + e.ToString());
                        run = false;
                        tcpServer.Stop();
                    }
                }
            }));
            tcpThread.Start();
        }

        public static void stopTcpListener() {
            runTcpServer = false;
            tcpServer.Stop();
        }

        private static void HandleClientComm(object client) {

            TcpClient tcpClient = (TcpClient)client;
            tcpClient.NoDelay = true;

            Log.Info("Cliente conectado: " + tcpClient.Client.RemoteEndPoint.ToString());

            NetworkStream clientStream = tcpClient.GetStream();
            /*string str = "";
            ASCIIEncoding encoder = new ASCIIEncoding();*/
            //byte[] buffer = new byte[256];
            //int len = 0;
            bool run = true;

            /*var sendStr = new Action(() => {
                len = encoder.GetBytes(str, 0, str.Length, buffer, 0);

                try {
                    clientStream.Write(buffer, 0, len);
                    clientStream.Flush();
                    //GC.Collect();
                } catch (System.IO.IOException) {
                    Log.Info("El cliente " + tcpClient.Client.RemoteEndPoint.ToString() + " se ha desconectado");
                    run = false;
                } catch (Exception e) {
                    Log.Error("Exception: " + e.ToString());
                    run = false;
                }
            });*/

            //Thread.Sleep(150);

            ComputerInfo ci = new ComputerInfo {
                CpuName = HardwareMonitor.CPUName,
                GpuName = HardwareMonitor.GPUName
            };

            /*str = ":cpu=" + HardwareMonitor.CPUName + "\n";
            sendStr();
            str = ":gpu=" + HardwareMonitor.GPUName + "\n";
            sendStr();*/

            try {
                // Tipo
                clientStream.WriteByte(0x00);
                // Tamaño
                /*int size = ci.CalculateSize();
                clientStream.WriteByte((byte)(size >> 8));
                clientStream.WriteByte((byte)size);*/
                // Datos
                //ci.WriteTo(clientStream);
                ci.WriteDelimitedTo(clientStream);
                clientStream.Flush();
            } catch (System.IO.IOException) {
                Log.Info("El cliente " + tcpClient.Client.RemoteEndPoint.ToString() + " se ha desconectado");
                run = false;
            } catch (Exception e) {
                Log.Error("Exception: " + e.ToString());
                run = false;
            }

            ci = null;



            while (runTcpServer && run) {

                //bufferincmessage = encoder.GetString(message, 0, bytesRead);

                HardwareMonitor.update();

                DataInt dataProto = new DataInt {
                    CpuFreq = (int)Math.Round(HardwareMonitor.CPUFreq),
                    CpuLoad = (int)Math.Round(HardwareMonitor.CPULoad),
                    CpuTemp = (int)Math.Round(HardwareMonitor.CPUTemp),
                    Fps = (int)Math.Round(RTSS.getFPS()),
                    GpuFreq = (int)Math.Round(HardwareMonitor.GPUFreq),
                    GpuLoad = (int)Math.Round(HardwareMonitor.GPULoad),
                    GpuTemp = (int)Math.Round(HardwareMonitor.GPUTemp)
                };
                //byte[] type = { 0x01 };
                //clientStream.Write(type, 0, 1);
                try {
                    clientStream.WriteByte(0x01);
                    //dataProto.WriteTo(clientStream);
                    dataProto.WriteDelimitedTo(clientStream);
                    clientStream.Flush();
                } catch (System.IO.IOException) {
                    Log.Info("El cliente " + tcpClient.Client.RemoteEndPoint.ToString() + " se ha desconectado");
                    run = false;
                } catch (Exception e) {
                    Log.Error("Exception: " + e.ToString());
                    run = false;
                }

                /*str = (int)Math.Round(HardwareMonitor.CPULoad) + ";" + (int)Math.Round(HardwareMonitor.GPULoad) + ";";
                str += (int)Math.Round(RTSS.getFPS());
                str += ";" + (int)Math.Round(HardwareMonitor.CPUTemp) + ";" + (int)Math.Round(HardwareMonitor.GPUTemp);
                str += ";" + (int)Math.Round(HardwareMonitor.CPUFreq) + ";" + (int)Math.Round(HardwareMonitor.GPUFreq) + "\n";
                //Log.Data(s);
                //byte[] buffer = encoder.GetBytes(buf,);

                sendStr();*/
                //dataProto.WriteTo()


                Thread.Sleep(freq);

            }
        }

        public static int Frequency {
            get { return freq; }
            set { freq = value; }
        }


    }
}
