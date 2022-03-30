using SteamP2PInfo.WinAPI;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Threading;
using SteamP2PInfo.Config;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace SteamP2PInfo
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private WindowInteropHelper interopHelper;
        private User32.WinEventDelegate locEvtHook, focusEvtHook;
        private IntPtr hLocHook, hFocusHook;
        bool isDragging = false;
        bool closed = false;

        private DispatcherTimer headerUpdateTimer;

        public IntPtr TgtWinHandle { get; private set; }
        public uint TgtWinThreadId { get; private set; }
        public uint TgtProcessId { get; private set; }

        public OverlayWindow(IntPtr TgtWinHandle, uint TgtProcessId, uint TgtThreadId)
        {
            this.TgtWinHandle = TgtWinHandle;
            this.TgtProcessId = TgtProcessId;
            this.TgtWinThreadId = TgtThreadId;

            InitializeComponent();

            interopHelper = new WindowInteropHelper(this);
            int exStyle = User32.GetWindowLongPtr(interopHelper.Handle, -20).ToInt32();
            User32.SetWindowLongPtr(interopHelper.Handle, -20, new IntPtr(exStyle | 0x80 | 0x20));
            UpdateVisibility();

            headerUpdateTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
            headerUpdateTimer.Tick += UpdateHeaderText;
            headerUpdateTimer.Start();
        }

        public void UpdateHeaderText(object sender, EventArgs evt)
        {
            header.Text = FormatUtils.NamedFormat(GameConfig.Current.OverlayConfig.BannerFormat,
                new string[1] { "time" }, DateTime.Now);

            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(UpdatePosition));
        }

        public void UpdateVisibility()
        {
            if (closed) return;

            bool shouldShow = !User32.IsIconic(TgtWinHandle);
            if (!IsVisible && GameConfig.Current.OverlayConfig.Enabled && shouldShow)
            {
                Show();
                UpdatePosition();
            }
            if (IsVisible && !(GameConfig.Current.OverlayConfig.Enabled && shouldShow)) Hide();

            bool shouldTopmost = User32.GetForegroundWindow() == TgtWinHandle || IsActive;
            bool isTopmost = (User32.GetWindowLongPtr(interopHelper.Handle, -20).ToInt32() & 0x8) != 0;
            
            if (shouldTopmost && !isTopmost) User32.SetWindowZOrder(interopHelper.Handle, new IntPtr(-1), 0x010);
            if (!shouldTopmost && isTopmost)
            {   // Place the overlay right above the DS3 window
                User32.SetWindowZOrder(interopHelper.Handle, TgtWinHandle, 0x010);
                User32.SetWindowZOrder(TgtWinHandle, interopHelper.Handle, 0x010);
                // For some reason setting the Z order from topmost to non-topmost clears the WS_EX_TOOLWINDOW flag
                // which allows the window to be invisible in ALT+TAB, so we have to re-apply it here.
                int exStyle = User32.GetWindowLongPtr(interopHelper.Handle, -20).ToInt32();
                User32.SetWindowLongPtr(interopHelper.Handle, -20, new IntPtr(exStyle | 0x80 | 0x20));
            }
        }

        public bool InstallMsgHook()
        {
            locEvtHook = new User32.WinEventDelegate(LocChangeHook);
            focusEvtHook = new User32.WinEventDelegate(FocusChangeHook);
            hLocHook = User32.SetWinEventHook(0x800B, 0x800B, IntPtr.Zero, locEvtHook, TgtProcessId, TgtWinThreadId, 3);
            hFocusHook = User32.SetWinEventHook(0x8005, 0x8005, IntPtr.Zero, focusEvtHook, 0, 0, 0);
            return hLocHook != IntPtr.Zero && hFocusHook != IntPtr.Zero;
        }

        private void LocChangeHook(IntPtr hWinEventHook, uint eventType, IntPtr lParam, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == 0x800b && lParam == TgtWinHandle && idObject == 0)
                UpdatePosition();
        }

        private void FocusChangeHook(IntPtr hWinEventHook, uint eventType, IntPtr lParam, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == 0x8005 && idChild == 0)
                UpdateVisibility();
        }

        private Rect GetWPFRect()
        {
            // Handle Windows DPI scaling (thanks anmer)
            var psc = PresentationSource.FromVisual(this);
            if (psc != null)
            {
                double dpi = psc.CompositionTarget.TransformToDevice.M11;
                User32.GetWindowRect(TgtWinHandle, out RECT targetRect);

                Rect wpfRect = new Rect(targetRect.x1 / dpi, targetRect.y1 / dpi,
                    (targetRect.x2 - targetRect.x1) / dpi, (targetRect.y2 - targetRect.y1) / dpi);

                return wpfRect;
            }

            return Rect.Empty;
        }

        public void UpdatePosition()
        {
            if (!isDragging)
            {
                Rect wpfRect = GetWPFRect();

                Left = wpfRect.Left + (((int)GameConfig.Current.OverlayConfig.Anchor % 2 == 1) ? GameConfig.Current.OverlayConfig.XOffset * wpfRect.Width :
                        (1 - GameConfig.Current.OverlayConfig.XOffset) * wpfRect.Width - ActualWidth);
                Top = wpfRect.Top + (((int)GameConfig.Current.OverlayConfig.Anchor < 2) ? GameConfig.Current.OverlayConfig.YOffset * wpfRect.Height :
                    (1 - GameConfig.Current.OverlayConfig.YOffset) * wpfRect.Height - ActualHeight);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            DragMove();
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Rect wpfRect = GetWPFRect();

                GameConfig.Current.OverlayConfig.XOffset = ((int)GameConfig.Current.OverlayConfig.Anchor % 2 == 1) ? (Left - wpfRect.Left) / wpfRect.Width :
                    1 - (Left - wpfRect.Left + ActualWidth) / wpfRect.Width;

                GameConfig.Current.OverlayConfig.YOffset  = ((int)GameConfig.Current.OverlayConfig.Anchor < 2) ? (Top - wpfRect.Top) / wpfRect.Height :
                    1 - (Top - wpfRect.Top + ActualHeight) / wpfRect.Height;

                isDragging = false;
            }
        }

        private void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep == null) return;

            if (dep is DataGridRow)
            {
                DataGridRow row = dep as DataGridRow;
                var peers = (ObservableCollection<SteamPeerBase>)(dataGrid.ItemsSource);
                Steamworks.SteamFriends.ActivateGameOverlayToUser("steamid", peers[row.GetIndex()].SteamID);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            closed = true;
            headerUpdateTimer.Stop();
            User32.UnhookWinEvent(hLocHook);
            User32.UnhookWinEvent(hFocusHook);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            UpdatePosition();
        }
    }
}
