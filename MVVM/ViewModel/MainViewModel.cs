using Quicksave_Clipboard.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Quicksave_Clipboard.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        private object _currentView;
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


        }

        private void MinimizeToTray()
        {
            Application.Current.MainWindow.Hide();

            // Show balloon tip
            var trayIcon = (Hardcodet.Wpf.TaskbarNotification.TaskbarIcon)
                Application.Current.FindResource("MyTrayIcon");

            trayIcon.ShowBalloonTip("MyApp", "App is running in background", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }

        private void CloseApp()
        {
            var trayIcon = (Hardcodet.Wpf.TaskbarNotification.TaskbarIcon)
                Application.Current.FindResource("MyTrayIcon");

            trayIcon.Dispose();
            Application.Current.Shutdown();
        }

    }
}
