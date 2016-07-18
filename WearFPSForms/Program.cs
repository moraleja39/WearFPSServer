﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Net;
using System.IO;

namespace WearFPSForms {
    static class Program {

        static readonly int VERSION = 6;

        static CustomConsole console = null;

        static NotifyIcon taskIcon;
        static ContextMenuStrip menu;
        static short toInicialize = 2;
        //static Icon icon;

        [STAThread]
        static void Main() {

            // Catchear todas las excepciones no controladas para loguearlas
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            //icon = new Icon();

            Log.Initialize(".\\WearFPS.log", LogLevel.All, true);

            var ini = new IniFile();

            if (ini.Read("first_run") != "false") {
                FirewallHelper fh = FirewallHelper.Instance;
                if (fh.IsFirewallInstalled) {
                    Log.Debug("Windows Firewall is installed. Adding exception...");
                    var path = Application.ExecutablePath;
                    Log.Debug("Executable path is " + path);
                    fh.GrantAuthorization(path, "WearFPS");
                    fh = null;
                    /*Properties.Settings.Default.firstRun = false;
                    Properties.Settings.Default.Save();*/
                    ini.Write("first_run", "false");
                } else Log.Debug("Windows Firewall is not installed.");
            }
            if (ini.KeyExists("show_console") && Boolean.Parse(ini.Read("show_console"))) {
                toggleconsole_Click(null, null);
            }
            ini = null;

            if (File.Exists(@"update-info")) {
                Log.Debug("Eliminando archivos de actualización...");
                File.Delete(@"update-info");
                File.Delete(@"updater.exe");
            }

            using (MyWebClient client = new MyWebClient()) {
                int ver = -1;

                try {
                    string s = client.DownloadString("http://pc.oviedo.me/wfs/v");
                    ver = Int32.Parse(s);
                } catch (Exception e) {
                    Log.Warn("No se ha podido conectar con el servidor de actualización: " + e.Message);
                }
                if (ver > 0) {
                    Log.Info("Última versión en línea: " + ver + ". Versión local: " + VERSION);
                    if (ver > VERSION) {
                        File.WriteAllBytes(@"updater.exe", Properties.Resources.updater);
                        using (StreamWriter sw = new StreamWriter("update-info")) {
                            sw.Write("zip;http://pc.oviedo.me/wfs/" + ver + ".zip;release.zip");
                        }
                        Process.Start("updater.exe");
                        return;
                    }
                }
            }

            menu = new ContextMenuStrip();
            menu.Items.Add("Toggle console").Click += toggleconsole_Click;
            menu.Items.Add("Salir").Click += salir_Click;

            taskIcon = new NotifyIcon();
            taskIcon.Text = "WearFPS Server 0.1." + VERSION;
            taskIcon.Icon = Properties.Resources.NotifyIcon;
            taskIcon.ContextMenuStrip = menu;
            taskIcon.Visible = true;

            //Application.Run(new Form1());

            HardwareMonitor.initThreaded();
            RTSS.init();

            Application.Run();

            //taskIcon.Visible = false;

            //taskIcon.Dispose();
        }


        public static void componentLoaded() {
            toInicialize--;
            if (toInicialize == 0) {
                //RTSS.getFPS();
                Log.Debug("Todos los componentes cargados. Iniciando servidores de escucha...");
                NativeMethods.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
                /*Networking.startUdpListener();
                Networking.startTcpListener();*/
                Networking.start();
            }
        }

        public static void ConsoleClosing() {
            console = null;
            Log.DisableCustomConsole();
        }

        private static void salir_Click(object sender, EventArgs e) {
            taskIcon.Visible = false;
            /*Networking.stopUdpListener();
            Networking.stopTcpListener();*/
            Networking.stop();
            HardwareMonitor.stopThreaded();
            RTSS.finishRTSS();

            var ini = new IniFile();
            if (console != null) {
                ini.Write("show_console", "true");
            } else {
                ini.Write("show_console", "false");
            }

            Application.Exit();
        }

        private static void toggleconsole_Click(object sender, EventArgs e) {
            Log.Debug("toggle console clicked");
            if (console == null) {
                console = new CustomConsole();
                console.Show();
                Log.EnableCustomConsole(console);
            } else {
                Log.DisableCustomConsole();
                console.Close();
                console = null;
            }
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
            Log.Error("Unhandled thread exception: " + e.Exception.StackTrace);
            MessageBox.Show(sender.ToString() + "\n" + e.Exception.Message, "Unhandled Thread Exception");
            //throw e.Exception;
            salir_Click(null, null);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            Log.Error("Unhandled UI exception: " + (e.ExceptionObject as Exception).ToString());
            MessageBox.Show(sender.ToString() + "\n" + (e.ExceptionObject as Exception).Message, "Unhandled UI Exception");
            //throw e.ExceptionObject as Exception;
            salir_Click(null, null);
        }
    }
}
