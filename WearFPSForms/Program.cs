﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

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

            Log.Initialize(".\\WearFPS.log", LogLevel.All, false);

            menu = new ContextMenuStrip();
            menu.Items.Add("Salir").Click += salir_Click;

            taskIcon = new NotifyIcon();
            taskIcon.Text = "WearFPS Server";
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
            MessageBox.Show(e.Exception.Message, "Unhandled Thread Exception");
            throw e.Exception;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error("Unhandled UI exception: " + (e.ExceptionObject as Exception).ToString());
            MessageBox.Show((e.ExceptionObject as Exception).Message, "Unhandled UI Exception");
            throw e.ExceptionObject as Exception;
        }
    }
}