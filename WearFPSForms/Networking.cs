using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WearFPSForms
{
    static class Networking
    {
        private static String localIP = null;
        private static String localSubnetMaskt = null;

        private static UdpClient listener;
        private static int udpPort = 55632;
        private static Thread udpThread;
        private volatile static bool runUdpServer;

        private static TcpListener tcpServer;
        private static int tcpPort = 55633;
        private static Thread tcpThread;
        private volatile static bool runTcpServer;

        private static volatile int freq = 200;

        public static void findLocalIP()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                foreach (var x in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (x.Address.AddressFamily == AddressFamily.InterNetwork && x.IsDnsEligible)
                    {
                        Log.Info(String.Format(" IPAddress ........ : {0:x}", x.Address.ToString()));
                        if (localIP == null)
                        {
                            localIP = x.Address.ToString();
                            localSubnetMaskt = x.IPv4Mask.ToString();
                        }
                    }
                }
            }
        }

        public static void startUdpListener()
        {
            runUdpServer = true;
            udpThread = new Thread(new ThreadStart(() =>
            {
                listener = new UdpClient(udpPort);
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, udpPort);
                
                try
                {
                    while (runUdpServer)
                    {
                        Log.Debug("Waiting for broadcast");
                        byte[] bytes = listener.Receive(ref groupEP);
                        String data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                        Log.Info(String.Format("Received broadcast from {0} :\n {1}\n", groupEP.ToString(), data));
                        if (data == "IP_REQ")
                        {
                            byte[] res = Encoding.ASCII.GetBytes(localIP);
                            listener.Send(res, res.Length, groupEP);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warn(e.ToString());
                    runUdpServer = false;
                }
                finally
                {
                    listener.Close();
                }
            }));
            udpThread.Start();

        }

        public static void stopUdpListener()
        {
            listener.Close();
        }
        
        public static void startTcpListener()
        {
            runTcpServer = true;

            findLocalIP();
            //long ip = long.Parse(localIP);
            //long snm = long.Parse(localSubnetMaskt);
            IPAddress local = new IPAddress(IPAddress.Parse(localIP).Address & IPAddress.Parse(localSubnetMaskt).Address);

            bool run = true;

            tcpServer = new TcpListener(IPAddress.Any, tcpPort);
            tcpServer.Start();

            tcpThread = new Thread(new ThreadStart(() =>
            {
                Log.Info("Servidor TCP inicializado correctamente");
                while (run)
                {
                    try
                    {
                        TcpClient client = tcpServer.AcceptTcpClient();
                        client.NoDelay = true;
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));

                        clientThread.Start(client);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Exception: " + e.ToString());
                        run = false;
                        tcpServer.Stop();
                    }
                }
            }));
            tcpThread.Start();
        }

        public static void stopTcpListener()
        {
            runTcpServer = false;
            tcpServer.Stop();
        }

        private static void HandleClientComm(object client)
        {
            
            TcpClient tcpClient = (TcpClient)client;
            tcpClient.NoDelay = true;

            Log.Info("Cliente conectado: " + tcpClient.Client.RemoteEndPoint.ToString());

            NetworkStream clientStream = tcpClient.GetStream();
            string str = "";
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] buffer = new byte[256];
            int len = 0;
            bool run = true;

            var sendStr = new Action (() =>
           {
               len = encoder.GetBytes(str, 0, str.Length, buffer, 0);

               try
               {
                   clientStream.Write(buffer, 0, len);
                   clientStream.Flush();
                   //GC.Collect();
               }
               catch (System.IO.IOException)
               {
                   Log.Info("El cliente " + tcpClient.Client.RemoteEndPoint.ToString() + " se ha desconectado");
                   run = false;
               }
               catch (Exception e)
               {
                   Log.Error("Exception: " + e.ToString());
                   run = false;
               }
           });

            Thread.Sleep(150);

            str = ":cpu=" + HardwareMonitor.CPUName + "\n";
            sendStr();
            str = ":gpu=" + HardwareMonitor.GPUName + "\n";
            sendStr();



            while (runTcpServer && run)
            {
                
                //bufferincmessage = encoder.GetString(message, 0, bytesRead);

                HardwareMonitor.update();
                str = (int)HardwareMonitor.CPULoad + ";" + (int)HardwareMonitor.GPULoad + ";";
                str += (int)RTSS.getFPS();
                str += ";" + (int)HardwareMonitor.CPUTemp + ";" + (int)HardwareMonitor.GPUTemp;
                str += ";" + (int)Math.Round(HardwareMonitor.CPUFreq) + ";" + (int)Math.Round(HardwareMonitor.GPUFreq) + "\n";
                //Log.Data(s);
                //byte[] buffer = encoder.GetBytes(buf,);

                sendStr();
                

                Thread.Sleep(freq);

            }
        }

        public static int Frequency
        {
            get { return freq; }
            set { freq = value; }
        }

    
    }
}
