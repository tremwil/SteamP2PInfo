using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using SteamP2PInfo.WinAPI;
using MahApps.Metro.Controls;

namespace SteamP2PInfo
{
    /// <summary>
    /// Interaction logic for WindowSelectDialog.xaml
    /// </summary>
    public partial class WindowSelectDialog : MetroWindow
    {
        public class WindowInfo
        {
            public IntPtr Handle;
            public string Title;
            public string ProcessName;
            public uint ProcessId;
            public uint ThreadId;
        }

        private List<WindowInfo> windows = new List<WindowInfo>();
        public WindowInfo SelectedWindow = null;
        public bool skipSteamConsole = false;

        public WindowSelectDialog()
        {
            InitializeComponent();

            IntPtr shellWindow = User32.GetShellWindow();

            User32.EnumWindows((hWnd, lParam) =>
            {
                if (hWnd == shellWindow) return true;
                if (!User32.IsWindowVisible(hWnd)) return true;

                int length = User32.GetWindowTextLength(hWnd);
                if (length == 0) return true;

                StringBuilder builder = new StringBuilder(length);
                User32.GetWindowText(hWnd, builder, length + 1);

                WindowInfo wInfo = new WindowInfo() { Handle = hWnd, Title = builder.ToString() };
                wInfo.ThreadId = User32.GetWindowThreadProcessId(hWnd, out wInfo.ProcessId);
                wInfo.ProcessName = Process.GetProcessById((int)wInfo.ProcessId).ProcessName;
                windows.Add(wInfo);

                return true;
            }, 0);

            foreach (WindowInfo wInfo in windows)
            {
                WindowListBox.Items.Add($"{wInfo.Title} ({wInfo.ProcessName})");
            }
        }

        private void WindowListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectedWindow = windows[WindowListBox.SelectedIndex];
            DialogResult = true;
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedWindow = windows[WindowListBox.SelectedIndex];
            DialogResult = true;
        }

        private void OpenSkipConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedWindow = windows[WindowListBox.SelectedIndex];
            skipSteamConsole = true;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void WindowListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnOpen.IsEnabled = true;
            btnOpenSkipConsole.IsEnabled = true;
        }
    }
}
