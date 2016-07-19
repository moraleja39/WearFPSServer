using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static WearFPSForms.GameStarted.Types;

namespace WearFPSForms.Net {
    class Client {
        private TcpClient client;
        private IPEndPoint clientEP;

        private Thread tcpReceiveThread;
        private Thread tcpSendThread;
        private Thread dataSender;
        private AutoResetEvent dataReset;

        private struct Message {
            public byte type;
            public IMessage msg;
            public Message (byte t, IMessage m) {
                this.type = t;
                this.msg = m;
            }
        }
        private Queue<Message> tcpQueue;
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
            tcpQueue = new Queue<Message>();

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
                            ComputerInfo ci = new ComputerInfo {
                                CpuName = HardwareMonitor.CPUName,
                                GpuName = HardwareMonitor.GPUName
                            };
                            tcpQueue.Enqueue(new Message(0x01, ci));
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

        public void NotifyGameLaunched(string title, string exe, AppFlags apiflag) {

            Api api;
            #region UglyApiSwitch
            switch (apiflag) {
                case AppFlags.APPFLAG_DD:
                    api = Api.Dd;
                    break;
                case AppFlags.APPFLAG_D3D9:
                    api = Api.D3D9;
                    break;
                case AppFlags.APPFLAG_D3D9EX:
                    api = Api.D3D9Ex;
                    break;
                case AppFlags.APPFLAG_OGL:
                    api = Api.Ogl;
                    break;
                case AppFlags.APPFLAG_D3D10:
                    api = Api.D3D10;
                    break;
                case AppFlags.APPFLAG_D3D11:
                    api = Api.D3D11;
                    break;
                default:
                    api = Api.Unknown;
                    break;
            }
            #endregion

            GameStarted gs = new GameStarted {
                Api = api,
                Exe = exe,
                Name = title
            };
            tcpQueue.Enqueue(new Message(0x02, gs));
            tcpReset.Set();
        }

        public void NotifyGameClosed() {
            tcpQueue.Enqueue(new Message(0x03, null));
            tcpReset.Set();
        }

        private void tcpSendRunner() {
            var stream = client.GetStream();
            while(run) {
                tcpReset.WaitOne();
                if (!run) continue;
                if (tcpQueue.Count <= 0) continue;
                Message message = tcpQueue.Dequeue();
                stream.WriteByte(message.type);
                if (message.msg != null) message.msg.WriteDelimitedTo(stream);
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

                DataInt2 dataProto = new DataInt2 {
                    CpuFreq = (int)Math.Round(HardwareMonitor.CPUFreq),
                    CpuLoad = (int)Math.Round(HardwareMonitor.CPULoad),
                    CpuTemp = (int)Math.Round(HardwareMonitor.CPUTemp),
                    Fps = (int)Math.Round(RTSS.getFPS()),
                    GpuFreq = (int)Math.Round(HardwareMonitor.GPUFreq),
                    GpuLoad = (int)Math.Round(HardwareMonitor.GPULoad),
                    GpuTemp = (int)Math.Round(HardwareMonitor.GPUTemp),
                    AvailableMem = (int)HardwareMonitor.AvailableMem,
                    UsedMem = (int)HardwareMonitor.UsedMem,
                    GameMem = (int)RTSS.GetCurAppRamUsage()
                };

                UdpDataSender.Send(0x01, dataProto.ToByteArray(), clientEP);

                dataProto = null;

            }
        }
    }
}
