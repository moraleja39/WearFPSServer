using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Net;
using System.IO;

namespace WearFPSForms
{
    static class Program
    {
        static NotifyIcon taskIcon;
        static ContextMenuStrip menu;
        static short toInicialize = 2;
        //static Icon icon;

        [STAThread]
        static void Main()
        {

            // Catchear todas las excepciones no controladas para loguearlas
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            //icon = new Icon();

            Log.Initialize(".\\WearFPS.log", LogLevel.All, true);

            if (File.Exists(@"update-info"))
            {
                Log.Debug("Eliminando archivos de actualización...");
                File.Delete(@"update-info");
                File.Delete(@"updater.exe");
            }

            using (WebClient client = new WebClient())
            {
                int ver = -1;
                try
                {
                    string s = client.DownloadString("http://pc.oviedo.me/wfs/v");
                    ver = Int32.Parse(s);
                } catch (Exception e)
                {
                    Log.Warn("No se ha podido conectar con el servidor de actualización: " + e.Message);
                }
                if (ver > 0)
                {
                    Log.Info("Última versión en línea: " + ver + ". Versión local: " + Properties.Settings.Default.version);
                    if (ver > Properties.Settings.Default.version)
                    {
                        File.WriteAllBytes(@"updater.exe", Properties.Resources.updater);
                        using (StreamWriter sw = new StreamWriter("update-info"))
                        {
                            sw.Write("zip;http://pc.oviedo.me/wfs/" + ver + ".zip;release.zip");
                        }
                        Process.Start("updater.exe");
                        return;
                    }
                }
            }

            if (Properties.Settings.Default.firstRun)
            {
                FirewallHelper fh = FirewallHelper.Instance;
                if (fh.IsFirewallInstalled)
                {
                    Log.Debug("Windows Firewall is installed. Adding exception...");
                    var path = Application.ExecutablePath;
                    Log.Debug("Executable path is " + path);
                    fh.GrantAuthorization(path, "WearFPS");
                    fh = null;
                    Properties.Settings.Default.firstRun = false;
                    Properties.Settings.Default.Save();
                }
                else Log.Debug("Windows Firewall is not installed.");
            }

            menu = new ContextMenuStrip();
            menu.Items.Add("Salir").Click += salir_Click;

            taskIcon = new NotifyIcon();
            taskIcon.Text = "WearFPS Server 0.1." + Properties.Settings.Default.version;
            taskIcon.Icon = Properties.Resources.NotifyIcon;
            taskIcon.ContextMenuStrip = menu;
            taskIcon.Visible = true;

            Console.WriteLine((int)-1f);



            //Application.Run(new Form1());

            HardwareMonitor.initThreaded();
            RTSS.init();

            Application.Run();

            //taskIcon.Visible = false;

            //taskIcon.Dispose();
        }


        public static void componentLoaded()
        {
            toInicialize--;
            if (toInicialize == 0)
            {
                //RTSS.getFPS();
                Log.Debug("Todos los componentes cargados. Iniciando servidores de escucha...");
                NativeMethods.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
                Networking.startUdpListener();
                Networking.startTcpListener();
            }
        }

        private static void salir_Click(object sender, EventArgs e)
        {
            taskIcon.Visible = false;
            Networking.stopUdpListener();
            Networking.stopTcpListener();
            HardwareMonitor.stopThreaded();
            RTSS.finishRTSS();
            Application.Exit();
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Error("Unhandled thread exception: " + e.ToString());
            MessageBox.Show(sender.ToString() + "\n" + e.Exception.Message, "Unhandled Thread Exception");
            //throw e.Exception;
            salir_Click(null, null);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error("Unhandled UI exception: " + (e.ExceptionObject as Exception).ToString());
            MessageBox.Show(sender.ToString() + "\n" + (e.ExceptionObject as Exception).Message, "Unhandled UI Exception");
            //throw e.ExceptionObject as Exception;
            salir_Click(null, null);
        }
    }
}
