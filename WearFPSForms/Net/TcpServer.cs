using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WearFPSForms.Net {
    class TcpServer {
        private TcpListener tcpServer;
        private static readonly int tcpPort = 55633;

        private Thread tcpThread;

        public void Start() {

            bool run = true;

            tcpServer = new TcpListener(IPAddress.Any, tcpPort);
            tcpServer.Start();

            tcpThread = new Thread(new ThreadStart(() => {
                Log.Info("Servidor TCP inicializado correctamente");
                while (run) {
                    try {
                        TcpClient client = tcpServer.AcceptTcpClient();
                        //client.NoDelay = true;
                        Clients.Add(client);
                    } catch (Exception e) {
                        Log.Error("Exception: " + e.ToString());
                        run = false;
                        tcpServer.Stop();
                    }
                }
            }));
            tcpThread.Start();
        }

        public void Stop() {
            tcpServer.Stop();
        }

        

    }
}
