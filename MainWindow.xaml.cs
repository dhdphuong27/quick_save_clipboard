using Quicksave_Clipboard.MVVM.Model;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Quicksave_Clipboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int HOTKEY_ID = 9000;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {

                HotkeyManager.RegisterHotKey(this, HOTKEY_ID, () =>
                {
                    // Get VM and call SaveContentCommand
                    if (DataContext is Quicksave_Clipboard.MVVM.ViewModel.MainViewModel mainVM)
                    {
                        var clipboardVM = mainVM.ClipboardVM;
                        if (clipboardVM.SaveContentCommand.CanExecute(null))
                            clipboardVM.SaveContentCommand.Execute(null);
                    }
                });
            };

            Closed += (s, e) =>
            {
                HotkeyManager.UnregisterHotKey(this, HOTKEY_ID);
            };
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the screen's working area (excluding taskbar)
            var workingArea = SystemParameters.WorkArea;

        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

    }
}