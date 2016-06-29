using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.IO.Compression;



namespace Updater
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Thread worker;
        Thread updater;
        ManualResetEvent oThreadReset = new ManualResetEvent(false);
        

        public MainWindow()
        {
#if DEBUG
#warning Modo de compilación DEBUG habilitado
            Console.WriteLine("Atención: modo debug habilitado.");
#endif
            InitializeComponent();

            worker = new Thread(Worker);
            updater = new Thread(doUpdate);

            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            progressBar1.Visibility = Visibility.Hidden;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            /*BitmapImage logo = new BitmapImage();
            try
            {
                logo.BeginInit();
                logo.UriSource = new Uri("pack://application:,,,/VisualUpdater;component/img/load15.png");
                logo.EndInit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }*/
            
            worker.Start();
            this.Closing += new CancelEventHandler(Window_CancelClosing);
            this.Closing -= new CancelEventHandler(Window_Closing);
            updater.Start();
            
        }

        private void Window_CancelClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        public delegate void loaderUpdater(int n);
        private void lU(int n)
        {
            image1.Source = new BitmapImage(new Uri("pack://application:,,,/Updater;component/img/load" + n.ToString() + ".png"));
        }

        private void Worker()
        {
            int i = 0;
            while (true)
            {
                this.Dispatcher.BeginInvoke(new loaderUpdater(lU),i);
                if (i == 30) i = 0; else i++;
                Thread.Sleep(30);
            }
        }

        public delegate void textUpdater(object s);
        private void tU(object s)
        {
            textBlock1.Text = (string)s;
        }
        private void pB(object s)
        {
            if (image1.IsVisible)
            {
                image1.Visibility = Visibility.Hidden;
                worker.Abort();
            }
            if (!progressBar1.IsVisible) progressBar1.Visibility = Visibility.Visible;
            if (TaskbarItemInfo.ProgressState == TaskbarItemProgressState.Indeterminate)
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                TaskbarItemInfo.ProgressValue = 0.0;
            }

            TaskbarItemInfo.ProgressValue = (double)s*0.01;
            progressBar1.Value = (double)s;
        }
        private void titleU(object s)
        {
            this.Title = "Descargando " + (string)s + "...";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            worker.Abort();
            updater.Abort();
            //GC.Collect();
        }


        public void doUpdate()
        {
            //Window1 w = new Window1();
            //w.Show();

            
            //Console.WriteLine(Process.GetProcessById(Process.GetCurrentProcess().Id).Parent().ToString());
            Process parent = ParentProcessUtilities.GetParentProcess();
            if (parent != null && parent.ProcessName != "WearFPSForms" && parent.ProcessName != "devenv")
            {
                Console.WriteLine(parent.ProcessName);
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.Title = "Error";
                    image1.Source = new BitmapImage(new Uri("pack://application:,,,/Updater;component/img/error.png"));
                    textBlock1.Text = "El actualizador no se puede ejecutar directamente";
                }));
                //Console.WriteLine("El actualizador no se puede ejecutar directamente.");
                this.Closing -= new CancelEventHandler(Window_CancelClosing);
                this.Closing += new CancelEventHandler(Window_Closing);
                worker.Abort();
                System.Environment.Exit(0);
                updater.Abort();
                return;
            }

            string parentpath = "";
            if (parent != null)
            {
                parentpath = parent.MainModule.FileName;
                parent.Kill();
                parent.WaitForExit();
#if DEBUG
                Console.WriteLine(parent.ProcessName);
                Console.WriteLine(parent.MainModule.FileName);
                Console.WriteLine(parent.MainModule.BaseAddress);
                Console.WriteLine(parent.MainModule.ModuleName);
#endif
            }
            else
            {
                parentpath = "WearFPS.exe";
            }

            FileStream file;
            Console.WriteLine("ASD");
            try
            {
                file = new FileStream("update-info", FileMode.Open);
            }
            catch
            {
                this.Dispatcher.Invoke(new textUpdater(tU),"Error: no se ha encontrado la información de la actualización");
                //worker.Abort();
                return;
            }
            StreamReader reader = new StreamReader(file);
            List<string> lines = new List<string>();
            while (!reader.EndOfStream)
            {
                lines.Add(reader.ReadLine());
            }
            reader.Dispose();
            List<File_> files = new List<File_>(lines.Count);
            foreach (string s in lines)
            {
                Console.WriteLine(s);
                s.Trim();
                if (s[0] == ':') { }
                else
                {
                    string[] parts = s.Split(';');
                    files.Add(new File_(parts[0],parts[1],parts[2]));
                    Console.WriteLine(parts[0] + "   /   " + parts[1] + "   /   " + parts[2]);
                }
            }
            lines.Clear();
            foreach (File_ f in files)
            {
                if (f.CheckUri())
                {
                    Console.WriteLine(f.Url + " es una URL válida.");
                }
                else
                {
                    Console.WriteLine(f.Url + " NO es una URL válida.");
                    //this.Dispatcher.Invoke(new textUpdater(tU), "Error: no se han encontrado uno o varios archivos");
                    //worker.Abort();
                    //return;
                }
            }

            //this.Dispatcher.BeginInvoke(new textUpdater(pB),0);

            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadCompleted);
            /*webClient.DownloadFileAsync(new Uri(files[0].Url), @files[0].Path);
            oThreadReset.WaitOne();
            oThreadReset.Reset();*/

            foreach (File_ f in files)
            {
                if (f.Exists)
                {
                    this.Dispatcher.Invoke(new textUpdater(titleU), @f.Path);
                    webClient.DownloadFileAsync(new Uri(f.Url), @f.Path);
                    oThreadReset.WaitOne();
                    oThreadReset.Reset();
                    Thread.Sleep(500);
                }
            }
            webClient.Dispose();
            //GC.Collect(1);

            foreach (File_ f in files)
            {
                if (f.Mode == "zip")
                {
                    this.Dispatcher.Invoke(new textUpdater(tU), "Extrayendo " + f.Path + "...");
                    ZipStorer zip = ZipStorer.Open(@f.Path, FileAccess.Read);
                    List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();
                    foreach (ZipStorer.ZipFileEntry entry in dir)
                    {
                            // File found, extract it
                        zip.ExtractFile(entry, @".\" + entry.FilenameInZip);
                    }
                    zip.Close();
                    File.Delete(@f.Path);
                }
            }

            //File.Delete(@"update-info");

            
            this.Closing -= new CancelEventHandler(Window_CancelClosing);
            this.Closing += new CancelEventHandler(Window_Closing);

            this.Dispatcher.Invoke(new Action(() =>
            {
                this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                TaskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/Updater;component/img/load0.png"));
            }));
            Thread.Sleep(1568);
            Process.Start(parentpath);
            System.Environment.Exit(0);

        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;

            this.Dispatcher.Invoke(new textUpdater(tU), (bytesIn / 1048576).ToString("0.00") + "/" + (totalBytes / 1048576).ToString("0.00") + "MB");
            this.Dispatcher.BeginInvoke(new textUpdater(pB), percentage);
        }
        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            oThreadReset.Set();
            Console.WriteLine("Download completed! " + e.UserState);
        }


    }

    public class File_
    {
        public string Mode { get; set; }
        public string Url { get; set; }
        public string Path { get; set; }
        public bool Exists { get; set;  }

        public File_(string m, string u, string p)
        {
            Mode = m; Url = u; Path = p;
        }

        public bool CheckUri()
        {
            HttpWebResponse response;
            try
            {
                HttpWebRequest request = WebRequest.Create(Url) as HttpWebRequest;
                request.Method = "HEAD";
                response = request.GetResponse() as HttpWebResponse;
                Console.WriteLine(response.StatusCode);
                response.Close();
                this.Exists = true;
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                //Any exception will returns false.
                //Console.WriteLine(e.ToString());
                Exists = false;
                return false;
            }
        }
    }

    /// <summary>
    /// ////////////
    /// </summary>

    #region ProcExt
    /*public static class ProcessExtensions
    {
        private static string FindIndexedProcessName(int pid)
        {
            var processName = Process.GetProcessById(pid).ProcessName;
            var processesByName = Process.GetProcessesByName(processName);
            string processIndexdName = null;

            for (var index = 0; index < processesByName.Length; index++)
            {
                processIndexdName = index == 0 ? processName : processName + "#" + index;
                var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
                if ((int)processId.NextValue() == pid)
                {
                    return processIndexdName;
                }
            }

            return processIndexdName;
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
            return Process.GetProcessById((int)parentId.NextValue());
        }

        public static Process Parent(this Process process)
        {
            return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
        }
    }*/
    #endregion


    #region ParentProcess
    /// <summary>
    /// A utility class to determine a process parent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ParentProcessUtilities
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        /// Gets the parent process of specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(int id)
        {
            Process process = Process.GetProcessById(id);
            return GetParentProcess(process.Handle);
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(IntPtr handle)
        {
            ParentProcessUtilities pbi = new ParentProcessUtilities();
            int returnLength;
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }
    }
#endregion
}
