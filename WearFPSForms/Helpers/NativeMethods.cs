using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WearFPSForms
{
    class NativeMethods
    {

        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("kernel32.dll")]
        public static extern uint GetProcessIdOfThread(UIntPtr thr);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("rtssbridge.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint getNthProc(int i);

        [DllImport("rtssbridge.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern float computeFPS(int i);

        [DllImport("rtssbridge.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtssMain();

        [DllImport("rtssbridge.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void printTest();

        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

        // INI helper
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);
    }
}
