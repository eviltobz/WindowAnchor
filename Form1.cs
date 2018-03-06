using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowAnchor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Tips about making the Windows system calls found at:
        // http://www.pinvoke.net/search.aspx?search=monitors&namespace=[All]

        private void Refresh_Click(object sender, EventArgs e)
        {
            Output.Clear();
            var disp = new Monitors();
            var d = disp.GetDisplays();

            Func<Rect, string> r = x => $"({x.left},{x.top}),({x.right},{x.bottom})";

            foreach (var screen in d)
            {
                Output.AppendText($"Availabliity:{screen.Availability}, W:{screen.ScreenWidth}, H:{screen.ScreenHeight}, Mon:{r(screen.MonitorArea)}, Work:{r(screen.WorkArea)}" + Environment.NewLine);
            }


            Output.AppendText(Environment.NewLine + "---------------------------" + Environment.NewLine + "WINDOWS:" + Environment.NewLine);

            //var wins = new EnumerateOpenedWindows();
            string[] strWindowsTitles = EnumerateOpenedWindows.GetDesktopWindows();
            foreach (string strTitle in strWindowsTitles)
            {
                //Console.WriteLine(strTitle);
                Output.AppendText(strTitle + Environment.NewLine);
            }

        }
    }



    public class EnumerateOpenedWindows
    {
        const int MAXTITLE = 255;

        private static List<string> lstTitles;

        private delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumDesktopWindows(IntPtr hDesktop,
        EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowText",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int _GetWindowText(IntPtr hWnd,
        StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "GetWindowPlacement",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int _GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT windowPlacement);

        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public Rectangle rcNormalPosition;
            //public System.Drawing.Point ptMinPosition;
            //public System.Drawing.Point ptMaxPosition;
            //public System.Drawing.Rectangle rcNormalPosition;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        public static bool EnumWindowsProc(IntPtr hWnd, int lParam)
        {
            string strTitle = GetWindowText(hWnd);
            if (strTitle != "" & IsWindowVisible(hWnd)) //
            {
                lstTitles.Add(strTitle);
            }
            return true;
        }

        /// <summary>
        /// Return the window title of handle
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static string GetWindowText(IntPtr hWnd)
        {
            StringBuilder strbTitle = new StringBuilder(MAXTITLE);
            int nLength = _GetWindowText(hWnd, strbTitle, strbTitle.Capacity + 1);
            strbTitle.Length = nLength;

            WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
            wp.length = Marshal.SizeOf(wp);
            var result = _GetWindowPlacement(hWnd, ref wp);

            //Func<Point, string> p = x => x.IsEmpty ? "NA".PadRight(13) : x.X.ToString().PadLeft(6) + "," + x.Y.ToString().PadLeft(6);
            Func<Point, string> p = x =>  x.x.ToString().PadLeft(6) + "," + x.y.ToString().PadLeft(6);
            Func<Rectangle, string> r = x => x.IsEmpty ? "NA".PadRight(23) : x.X.ToString().PadLeft(5) + "," + x.Y.ToString().PadLeft(5) + "/" +
                x.Right.ToString().PadLeft(5) + "," + x.Bottom.ToString().PadLeft(5);// + "/" + x.;


            var place = $"rect:{r(wp.rcNormalPosition)}  min:{p(wp.ptMinPosition)}, max:{p(wp.ptMaxPosition)}, FC:{wp.flags}/{wp.showCmd} :: ";

            return place + strbTitle.ToString();
        }

        /// <summary>
        /// Return titles of all visible windows on desktop
        /// </summary>
        /// <returns>List of titles in type of string</returns>
        public static string[] GetDesktopWindows()
        {
            lstTitles = new List<string>();
            EnumDelegate delEnumfunc = new EnumDelegate(EnumWindowsProc);
            bool bSuccessful = EnumDesktopWindows(IntPtr.Zero, delEnumfunc, IntPtr.Zero); //for current desktop

            if (bSuccessful)
            {
                return lstTitles.ToArray();
            }
            else
            {
                // Get the last Win32 error code
                int nErrorCode = Marshal.GetLastWin32Error();
                string strErrMsg = String.Format("EnumDesktopWindows failed with code {0}.", nErrorCode);
                throw new Exception(strErrMsg);
            }
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }


    public class Monitors
    {
        delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);
        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
           MonitorEnumDelegate lpfnEnum, IntPtr dwData);
        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);
        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);




        // size of a device name string
        private const int CCHDEVICENAME = 32;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct MONITORINFO
        {
            /// <summary>
            /// The size, in bytes, of the structure. Set this member to sizeof(MONITORINFOEX) (72) before calling the GetMonitorInfo function.
            /// Doing so lets the function determine the type of structure you are passing to it.
            /// </summary>
            //public int Size;
            public uint Size;

            /// <summary>
            /// A RECT structure that specifies the display monitor rectangle, expressed in virtual-screen coordinates.
            /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
            /// </summary>
            public Rect Monitor;

            /// <summary>
            /// A RECT structure that specifies the work area rectangle of the display monitor that can be used by applications,
            /// expressed in virtual-screen coordinates. Windows uses this rectangle to maximize an application on the monitor.
            /// The rest of the area in rcMonitor contains system windows such as the task bar and side bars.
            /// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
            /// </summary>
            public Rect WorkArea;

            /// <summary>
            /// The attributes of the display monitor.
            ///
            /// This member can be the following value:
            ///   1 : MONITORINFOF_PRIMARY
            /// </summary>
            public uint Flags;

            public void Init()
            {
                this.Size = 40 + 2 * CCHDEVICENAME;
            }
        }





        /// <summary>
        /// The struct that contains the display information
        /// </summary>
        public class DisplayInfo
        {
            public string Availability { get; set; }
            public string ScreenHeight { get; set; }
            public string ScreenWidth { get; set; }
            //public string blurb { get; set; }
            public Rect MonitorArea { get; set; }
            public Rect WorkArea { get; set; }
        }

        /// <summary>
        /// Collection of display information
        /// </summary>
        public class DisplayInfoCollection : List<DisplayInfo>
        {
        }

        /// <summary>
        /// Returns the number of Displays using the Win32 functions
        /// </summary>
        /// <returns>collection of Display Info</returns>
        public DisplayInfoCollection GetDisplays()
        {
            DisplayInfoCollection col = new DisplayInfoCollection();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
                {
                    MONITORINFO mi = new MONITORINFO();
            //MONITORINFOEX mi = new MONITORINFOEX();
            mi.Size = (uint)Marshal.SizeOf(mi);
                    bool success = GetMonitorInfo(hMonitor, ref mi);
                    if (success)
                    {
                        DisplayInfo di = new DisplayInfo();
                        di.ScreenWidth = (mi.Monitor.right - mi.Monitor.left).ToString();
                        di.ScreenHeight = (mi.Monitor.bottom - mi.Monitor.top).ToString();
                        di.MonitorArea = mi.Monitor;
                        di.WorkArea = mi.WorkArea;
                        di.Availability = mi.Flags.ToString();
                //di.blurb = mi.DeviceName;
                col.Add(di);
                    }
                    return true;
                }, IntPtr.Zero);
            return col;
        }
    }
}
