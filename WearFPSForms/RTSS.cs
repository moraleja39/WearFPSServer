using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;

namespace WearFPSForms
{
    static class RTSS
    {
        private static bool rtssStartedMyself = false;
        private static bool? isRTSSInstalled = null;
        private static Process rtssProcess = null;
        private static String rtssPath = null;

        //static MemoryMappedFile mmf = null;
        //static RTSS_Mem mem;
        //static MemoryMappedViewStream appStream = null;
        //static BinaryReader appReader;
        //const int ARRAY_SIZE = 260;
        //static volatile RTSS_App[] app;
        //static volatile int appSize;
        static volatile int curApp = -1;
        static volatile Process curProcess;

        //static volatile bool runThread;
        //static Thread updateThread;


        private const UInt32 WM_CLOSE = 0x0010;
        private const UInt32 WM_QUIT = 0x0012;
        private const UInt32 RTSS_CLOSE = 0x0287;
        private const UInt32 WM_WINCOMMAND = 0x0112;
        private const UInt32 WM_COMMAND = 0x0111;
        private const UInt32 COMMAND_QUIT_LPARAM = 0x03E9;
        private const UInt32 SC_CLOSE = 0xF060;



        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        private static IntPtr m_hook = IntPtr.Zero;
        static NativeMethods.WinEventDelegate wed = null;

        public static void init()
        {
 
            if (!rtssInstalled())
            {
                Log.Error("RTSS no está instalado en este equipo");
                return;
            }

            if (!isRTSSRunning()) startRTSS();
            Log.Info("Conectando a la memoria compartida de RTSS...");

            int r = NativeMethods.rtssMain();

            if (r >= 0)
            {
                Log.Info("Mapeado a la memoria correcto. RTSS " + (r >> 16) + "." + (r & 0x0000FFFF));
            }
            else
            {
                Log.Error("No se puede conectar a la memoria compartida de RTSS");
                return;
            }


            setForegroundAppThread();

            /////
            wed = new NativeMethods.WinEventDelegate(WinEventProc);
            m_hook = NativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, wed, 0, 0, WINEVENT_OUTOFCONTEXT);

            Log.Info("RTSS iniciado correctamente.");

            GC.Collect();

            Program.componentLoaded();

        }

        static float fps;
        static public float getFPS()
        {
            if (curApp < 0) return 0f;
            fps = NativeMethods.computeFPS(curApp);
            //Log.Data("unmanaged fps: " + fps);
            if (fps < 0 || fps > 999) fps = 0f;
            return fps;
        }

        static System.Timers.Timer fgTimer;
        static ManualResetEvent threadSyncer = new ManualResetEvent(false);
        static Thread fgThread;
        private static void setForegroundAppThread()
        {
            fgTimer = new System.Timers.Timer();
            fgTimer.AutoReset = false;
            fgTimer.Interval = 250;
            fgTimer.Elapsed += onTimedEvent;
            fgTimer.Stop();

            fgThread = new Thread(new ThreadStart(fgThreadLoop));
            fgThread.Start();
        }

        private static void onTimedEvent(object source, ElapsedEventArgs e)
        {
            threadSyncer.Set();
        }

        static bool runFgThread = true;
        private static void fgThreadLoop()
        {
            while (runFgThread)
            {
                threadSyncer.WaitOne();
                for (int i = 0; i < 256; i++)
                {
                    tmp = NativeMethods.getNthProc(i);
                    //Log.Debug(i + "th: " + tmp);
                    if (tmp == pid) curApp = i;
                    else if (tmp == 0) break;
                }
                
                if (curApp != -1) {
                    AppFlags flags = (AppFlags)NativeMethods.getAppFlags(curApp);
                    if ((flags & AppFlags.APPFLAG_API_USAGE_MASK) > 0) {
                        curProcess = Process.GetProcessById((int)pid);
                        Log.Debug("La aplicación " + curProcess.MainWindowTitle + " (" + curProcess.ProcessName + ") usa la API de gráficos " + flags.ToString());
                        Net.Clients.NotifyGameLaunched(curProcess.MainWindowTitle, curProcess.ProcessName + ".exe", flags);
                    } else {
                        Log.Debug("App not using graphical api");
                        curApp = -1;
                    }
                } else {
                    Net.Clients.NotifyGameClosed();
                }
                threadSyncer.Reset();
            }
        }

        public static long GetCurAppRamUsage() {
            if (curApp < 0) return 0;
            else return curProcess.WorkingSet64;
        }

        static uint pid, tmp;
        private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            fgTimer.Stop();
            pid = 0;
            NativeMethods.GetWindowThreadProcessId(hwnd, out pid);
            //Process p = Process.GetProcessById((int)pid);
            //Log.Debug("Focus change. PID: " + pid);
            //p.Dispose();
            curApp = -1;
            fgTimer.Start();
            
        }

        static bool isRTSSRunning()
        {
            Process[] p = Process.GetProcessesByName("RTSS");
            if (p.Length > 0) return true;
            else return false;
        }

        static bool rtssInstalled()
        {
            if (isRTSSInstalled == null)
            {
                isRTSSInstalled = false;
                RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                try
                {
                    key = key.OpenSubKey(@"SOFTWARE\Unwinder\RTSS", false);
                }
                catch (Exception e)
                {
                    throw new InitializeException("No se ha podido acceder al valor del registro", e);
                }
                if (key == null) throw new InitializeException("RTSS no está instalado.");
                else
                {
                    Object o = key.GetValue("InstallPath");
                    if (o == null) throw new InitializeException("RTSS no está instalado.");
                    else
                    {
                        isRTSSInstalled = true;
                        rtssPath = o as String;

                    }
                }
            }
            return (bool)isRTSSInstalled;
        }

        static void startRTSS()
        {

            if (isRTSSRunning()) return;

            rtssStartedMyself = true;

            rtssProcess = new Process();
            rtssProcess.StartInfo.FileName = rtssPath;

            rtssProcess.Start();
        }

        public static void finishRTSS()
        {
            if (m_hook != IntPtr.Zero) NativeMethods.UnhookWinEvent(m_hook);
            runFgThread = false;
            threadSyncer.Set();
            fgTimer.Stop();
            fgTimer.Dispose();

            if (!rtssStartedMyself) return;

            //if (rtssProcess.MainWindowHandle == IntPtr.Zero)
            //{
                // Try closing application by sending WM_CLOSE to all child windows in all threads.
                /*foreach (ProcessThread pt in rtssProcess.Threads)
                {
                    EnumThreadWindows((uint)pt.Id, new EnumThreadDelegate(EnumThreadCallback), IntPtr.Zero);
                }*/
                NativeMethods.EnumThreadWindows((uint)rtssProcess.Threads[0].Id, new NativeMethods.EnumThreadDelegate(EnumThreadCallback), IntPtr.Zero);
            /*}
            else
            {
                // Try to close main window.
                if (rtssProcess.CloseMainWindow())
                {
                    // Free resources used by this Process object.
                    rtssProcess.Close();
                }

            }*/

            rtssProcess.Dispose();
        }

        static bool EnumThreadCallback(IntPtr hWnd, IntPtr lParam)
        {
            // Close the enumerated window.
            NativeMethods.PostMessage(hWnd, WM_COMMAND, (IntPtr)COMMAND_QUIT_LPARAM, IntPtr.Zero);

            return true;
        }
    }
}
