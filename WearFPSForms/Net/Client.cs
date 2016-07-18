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
        private TcpClient client;
        private IPEndPoint clientEP;

        private Thread tcpReceiveThread;
        private Thread tcpSendThread;
        private Thread dataSender;
        private AutoResetEvent dataReset;

        private Queue<byte> tcpQueue;
        private AutoResetEvent tcpReset;

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

            tcpReset = new AutoResetEvent(false);
            tcpQueue = new Queue<byte>();

            tcpReceiveThread = new Thread(new ThreadStart(tcpReceiveRunner));
            tcpReceiveThread.Name = "ClientTcpThread-" + clientEP.ToString();
            tcpReceiveThread.Start();

            dataSender = new Thread(new ThreadStart(DataRunner));
            dataReset = new AutoResetEvent(false);
            dataSender.Start();

            initialized = true;
        }

        public void stop() {
            run = false;
            dataReset.Set();
            tcpReset.Set();
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
                        // Request de informacion del PC
                        case 1:
                            tcpQueue.Enqueue(0x01);
                            tcpReset.Set();
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

        private void tcpSendRunner() {
            var stream = client.GetStream();
            while(run) {
                tcpReset.WaitOne();
                if (!run) continue;
                if (tcpQueue.Count <= 0) continue;
                byte type = tcpQueue.Dequeue();
                switch (type) {
                    //Enviar info del Pc
                    case 0x01:
                        ComputerInfo ci = new ComputerInfo {
                            CpuName = HardwareMonitor.CPUName,
                            GpuName = HardwareMonitor.GPUName
                        };
                        stream.WriteByte(0x01);
                        ci.WriteDelimitedTo(stream);
                        break;
                    default:
                        Log.Warn("tcpSendRunner: No se ha implementado el tipo de paquete " + type);
                        break;
                }
            }
        }

        public void NotifyDataChanged() {
            dataReset.Set();
        }

        private void DataRunner() {

            while (run) {
                dataReset.WaitOne();
                if (!run) return;

                if ((DateTime.Now - lastHB).TotalSeconds > timeout) {
                    Log.Info("The client " + clientEP.ToString() + " has timed out");
                    this.stop();
                    continue;
                }
                //HardwareMonitor.update();

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

            }
        }
    }
}
