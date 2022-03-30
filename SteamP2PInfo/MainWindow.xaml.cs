using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.IO;
using Steamworks;
using System.Security.Permissions;
using System.Media;
using SteamP2PInfo.Config;

namespace SteamP2PInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private ObservableCollection<SteamPeerBase> peers;
        private OverlayWindow overlay;
        private DispatcherTimer timer;
        private int timerTicks = 0;
        private bool saveRequest = false;

        private WindowSelectDialog.WindowInfo wInfo;

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowUnhandledException((Exception)e.ExceptionObject, "CurrentDomain", e.IsTerminating);
            TaskScheduler.UnobservedTaskException += (s, e) => ShowUnhandledException(e.Exception, "TaskScheduler", false);
            Dispatcher.UnhandledException += (s, e) => { if (!Debugger.IsAttached) ShowUnhandledException(e.Exception, "Dispatcher", true); };

            InitializeComponent();
            Closing += MainWindow_Closed;

            peers = new ObservableCollection<SteamPeerBase>();
            dataGridSession.DataContext = peers;
            Title = "Steam P2P Info " + VersionCheck.CurrentVersion;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            Settings.Default.PropertyChanged += (s, e) => { saveRequest = true; };

            Task.Run(() =>
            {
                if (VersionCheck.FetchLatest())
                {
                    string v = VersionCheck.LatestRelease["tag_name"].ToString();
                    if (string.Compare(VersionCheck.CurrentVersion, v) < 0)
                    {
                        this.Invoke(() =>
                        {
                            linkUpdate.NavigateUri = new Uri("https://github.com/tremwil/SteamP2PInfo/releases/tag/" + v);
                            textUpdate.Text = string.Format("NEW VERSION ({0}), DOWNLOAD HERE", v);
                            this.ShowMessageAsync("New Version Available", string.Format("{0} is out! Click the link in the title bar to download it.", v));
                        });
                    }
                }
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Necessary to close the program after the game exits, as SteamAPI_Shutdown isn't
            // sufficient to have steam recognize the game is no longer running
            if (!WinAPI.User32.IsWindow(wInfo.Handle))
                Close();

            if (HotkeyManager.Enabled && !GameConfig.Current.HotkeysEnabled)
                HotkeyManager.Disable();

            if (!HotkeyManager.Enabled && GameConfig.Current.HotkeysEnabled)
                HotkeyManager.Enable();

            if ((timerTicks = (timerTicks + 1) % 5) == 0)
            {
                // Rather not have the settings update on a loop, but 
                // Fody generated OnChange seems to break PropertyChanged 
                // for GameConfig. So do this for now.
                GameConfig.Current?.Save();
                SteamPeerManager.UpdatePeerList();
            }

            peers.Clear();
            foreach (SteamPeerBase p in SteamPeerManager.GetPeers())
                peers.Add(p);

            // Update session info column sizes
            foreach (var col in dataGridSession.Columns)
            {
                col.Width = new DataGridLength(1, DataGridLengthUnitType.Pixel);
                col.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }
            dataGridSession.UpdateLayout();

            // Update overlay column sizes
            foreach (var col in overlay.dataGrid.Columns)
            {
                col.Width = new DataGridLength(1, DataGridLengthUnitType.Pixel);
                col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
            }
            overlay.dataGrid.UpdateLayout();

            // Queue position update after the overlay has re-rendered
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(overlay.UpdatePosition));
            overlay.UpdateVisibility();

            if (saveRequest)
            {
                GameConfig.Current.Save();
                Settings.Default.Save();
                saveRequest = false;
            }
        }

        private void ShowUnhandledException(Exception err, string type, bool fatal)
        {
            MetroDialogSettings diagSettings = new MetroDialogSettings()
            {
                ColorScheme = MetroDialogColorScheme.Accented,
                AffirmativeButtonText = "Copy",
                NegativeButtonText = "Close"
            };

            SystemSounds.Exclamation.Play();
            var result = this.ShowModalMessageExternal($"Unhandled Exception: {err.GetType().Name}", $"{err.Message}\n{err.StackTrace}", MessageDialogStyle.AffirmativeAndNegative, diagSettings);
            if (result == MessageDialogResult.Affirmative)
                Clipboard.SetText($"{err.GetType().Name}: {err.Message}\n{err.StackTrace}");

            Close();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (GameConfig.Current != null) GameConfig.Current.Save();
            Settings.Default.Save();
            if (overlay != null) overlay.Close();
            HotkeyManager.Disable();
            ETWPingMonitor.Stop();
        }


        private void headerFmt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
        }

        private void webLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void labelGameState_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (wInfo == null)
            {
                WindowSelectDialog dialog = new WindowSelectDialog();
                if (dialog.ShowDialog() == true)
                {
                    wInfo = dialog.SelectedWindow;
                    if (GameConfig.LoadOrCreate(wInfo.ProcessName) || GameConfig.Current.SteamAppId == 0)
                    {
                        string input = Microsoft.VisualBasic.Interaction.InputBox("Please enter the Steam App ID to use with this game:", "Steam App ID Required");
                        if (!uint.TryParse(input, out uint result))
                        {
                            MessageBox.Show("Please input a valid number", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            wInfo = null;
                            return;
                        }
                        GameConfig.Current.SteamAppId = (int)result;
                    }

                    GameConfig.Current.WindowName = wInfo.Title;

                    HotkeyManager.AddHotkey(wInfo.Handle, () => GameConfig.Current.OverlayConfig.Hotkey, () => GameConfig.Current.OverlayConfig.Enabled ^= true);
                    overlay = new OverlayWindow(wInfo.Handle, wInfo.ProcessId, wInfo.ThreadId);
                    overlay.dataGrid.DataContext = peers;

                    SteamConsoleHelper();

                    if (!SteamPeerManager.Init())
                    {
                        MessageBox.Show("Could not initialize Steam API", "Steam API Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        wInfo = null;
                        return;
                    }
                    if (!overlay.InstallMsgHook())
                    {
                        MessageBox.Show("Could not setup overlay message hook", "WINAPI Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        wInfo = null;
                        return;
                    }

                    textGameState.Text = wInfo.Title;
                    textGameState.Foreground = Brushes.LawnGreen;

                    Grid configEditor = ConfigUIBuilder.CreateConfigEditor(GameConfig.Current);
                    ConfigTab.Children.Add(configEditor);

                    ETWPingMonitor.Start();
                    timer.Start();
                }
            }
        }

        private void SteamConsoleHelper()
        {
            Process.Start(new ProcessStartInfo("steam://open/console"));

            MetroDialogSettings diagSettings = new MetroDialogSettings()
            {
                ColorScheme = MetroDialogColorScheme.Accented,
                AffirmativeButtonText = "Copy Command",
                NegativeButtonText = "Close"
            };

            var result = this.ShowModalMessageExternal("Necessary Step", "The Steam console has just been opened. Please enter the following to enable matchmaking call logging: \"log_ipc IClientMatchmaking\"",
                MessageDialogStyle.AffirmativeAndNegative, diagSettings);
            if (result == MessageDialogResult.Affirmative)
                Clipboard.SetText("log_ipc IClientMatchmaking");
        }

        private void dataGridSession_DoubleClick(object sender, MouseButtonEventArgs e)
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
                SteamFriends.ActivateGameOverlayToUser("steamid", peers[row.GetIndex()].SteamID);
            }
        }
    }
}
