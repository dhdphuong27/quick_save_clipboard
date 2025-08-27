using Quicksave_Clipboard.MVVM.Model;
using System;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace Quicksave_Clipboard.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        private object _currentView;
        private TaskbarIcon _trayIcon;

        public RelayCommand ClipboardViewCommand { get; set; }
        public ClipboardViewModel ClipboardVM { get; set; }

        public RelayCommand MinimizeCommand { get; set; }
        public RelayCommand CloseCommand { get; set; }

        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            ClipboardVM = new ClipboardViewModel();
            CurrentView = ClipboardVM;

            ClipboardViewCommand = new RelayCommand(o =>
            {
                CurrentView = ClipboardVM;
            });

            MinimizeCommand = new RelayCommand(MinimizeToTray);
            CloseCommand = new RelayCommand(CloseApp);

            // Initialize tray icon at startup
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = (TaskbarIcon)Application.Current.FindResource("MyTrayIcon");

            // Double click restores window
            _trayIcon.TrayMouseDoubleClick -= TrayIcon_DoubleClick;
            _trayIcon.TrayMouseDoubleClick += TrayIcon_DoubleClick;

            // Context menu
            _trayIcon.ContextMenu = new ContextMenu();

            var showMenuItem = new MenuItem { Header = "Show" };
            showMenuItem.Click += (s, e) => RestoreFromTray();

            var exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (s, e) => CloseApp();

            _trayIcon.ContextMenu.Items.Add(showMenuItem);
            _trayIcon.ContextMenu.Items.Add(exitMenuItem);
        }

        private void MinimizeToTray()
        {
            Application.Current.MainWindow.Hide();

            // Show balloon tip (only on minimize, not on startup)
            _trayIcon.ShowBalloonTip("MyApp", "App is running in background", BalloonIcon.Info);
        }

        private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
        {
            RestoreFromTray();
        }

        private void RestoreFromTray()
        {
            var mainWindow = Application.Current.MainWindow;

            if (mainWindow == null)
                return;

            if (!mainWindow.IsVisible)
                mainWindow.Show();

            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;

            mainWindow.Activate();
        }

        private void CloseApp()
        {
            if (_trayIcon != null)
                _trayIcon.Dispose();

            Application.Current.Shutdown();
        }
    }
}
