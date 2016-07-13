using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WearFPSForms.Net {
    class UdpBroadcastReceiver : IDisposable{
        private UdpClient listener;

        private int udpPort = 55632;

        private Thread udpThread;
        private volatile bool runUdpServer;

        /*public UdpBroadcastReceiver(string ip) {
            this.localIP = ip;
            if (!IPAddress.TryParse(ip, out localIPAddress)) {
                throw new InitializeException("La cadena " + ip + " no representa una dirección IP válida");
            }
        }*/
        public UdpBroadcastReceiver() {
            if (LocalInterfaces.Count <= 0) {
                throw new InitializeException("No hay direcciones IP locales");
            }
        }

        public void start() {
            runUdpServer = true;
            udpThread = new Thread(new ThreadStart(() => {
                listener = new UdpClient(udpPort);
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, udpPort);

                try {
                    while (runUdpServer) {
                        Log.Debug("Waiting for broadcast");
                        byte[] bytes = listener.Receive(ref groupEP);
                        //String data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                        /*if (data == "IP_REQ")
                        {
                            byte[] res = Encoding.ASCII.GetBytes(localIP);
                            listener.Send(res, res.Length, groupEP);
                        }*/
                        if (bytes.Length < 1) {
                            Log.Warn(String.Format("Received an empty UDP broadcast from {0}", groupEP.ToString()));
                            continue;
                        } else {
                            Log.Info(String.Format("Received broadcast from {0}: {1}", groupEP.ToString(), BitConverter.ToString(bytes)));
                        }
                        switch (bytes[0]) {
                            // Server IP Request
                            case 0x00:
                                IPAddress ip = LocalInterfaces.GetAssociatedIp(groupEP.Address);
                                if (ip == null) throw new Exception("De alguna extraña manera, la dirección IP del cliente no se corresponde con ninguna de las interfaces de red locales");
                                byte[] res = Encoding.ASCII.GetBytes(ip.ToString());
                                listener.Send(res, res.Length, groupEP);
                                break;
                            default:
                                Log.Warn("Unsupported UDP broadcast type: " + bytes[0].ToString());
                                break;
                        }
                    }
                } catch (Exception e) {
                    Log.Warn(e.ToString());
                    runUdpServer = false;
                } finally {
                    listener.Close();
                    listener = null;
                }
            }));
            udpThread.Start();

        }

        public void stop() {
            listener.Close();
            this.Dispose();
        }

        public void Dispose() {
            if (listener != null) listener.Close();
        }
    }
}
