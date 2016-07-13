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
    class Client {
        private volatile int freq = 350;
        private TcpClient client;
        private IPEndPoint clientEP;

        private Thread tcpReceiveThread;
        private Thread dataSender;

        private bool initialized = false;
        private bool run;

        private object toLock = new object();
        // En segundos
        private const int timeout = 40;
        private DateTime lastHB;

        public Client(TcpClient c) {
            this.client = c;
            this.start();
        }

        private void start() {
            run = true;
            client.NoDelay = true;
            clientEP = client.Client.RemoteEndPoint as IPEndPoint;
            Log.Info("Cliente conectado: " + clientEP.ToString());

            lastHB = DateTime.Now;

            tcpReceiveThread = new Thread(new ThreadStart(tcpReceiveRunner));
            tcpReceiveThread.Name = "ClientTcpThread-" + clientEP.ToString();
            tcpReceiveThread.Start();

            dataSender = new Thread(new ThreadStart(DataRunner));
            dataSender.Start();

            initialized = true;
        }

        public void stop() {
            run = false;
            client.Close();
            Clients.Remove(this);
        }

        private void tcpReceiveRunner() {
            NetworkStream stream = client.GetStream();
            int type;
            try {
                while (run && (type = stream.ReadByte()) > -1) {
                    switch (type) {
                        // Hearthbeat
                        case 0:
                            lock (toLock) {
                                lastHB = DateTime.Now;
                            }
                            break;
                        default:
                            Log.Warn("Unknown TCP packet type received: " + type);
                            break;
                    }
                }
                Log.Info("El cliente " + clientEP.ToString() + " ha cerrado la conexión TCP. Eliminando...");
                this.stop();
            } catch (System.IO.IOException) {
                Log.Info("El cliente " + clientEP.ToString() + " se ha desconectado");
                this.stop();
            } catch (Exception e) {
                Log.Error("Exception: " + e.ToString());
                //this.stop();
            }
        }

        private void DataRunner() {

            // Primero enviamos la información del PC
            ComputerInfo ci = new ComputerInfo {
                CpuName = HardwareMonitor.CPUName,
                GpuName = HardwareMonitor.GPUName
            };
            UdpDataSender.Send(0x00, ci.ToByteArray(), clientEP);

            while (run) {
                if ((DateTime.Now - lastHB).TotalSeconds > timeout) {
                    Log.Info("The client " + clientEP.ToString() + " has timed out");
                    this.stop();
                    continue;
                }

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

                UdpDataSender.Send(0x01, dataProto.ToByteArray(), clientEP);

                dataProto = null;



                Thread.Sleep(freq);



            }
        }

        public int Frequency {
            get { return freq; }
            set { freq = value; }
        }
    }
}
