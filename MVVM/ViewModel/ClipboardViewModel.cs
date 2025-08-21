using Quicksave_Clipboard.MVVM.Model;
using Quicksave_Clipboard.MVVM.View;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static MaterialDesignThemes.Wpf.Theme;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;

namespace Quicksave_Clipboard.MVVM.ViewModel
{
    class ClipboardViewModel: ObservableObject
    {
        public RelayCommand SaveContentCommand { get; }

        public List<ClipboardContent> contents { get; set; } 

        public PagedCollection<ClipboardContent> PagedData { get; private set; }

        public RelayCommand LoadPreviousPageCommand { get; }
        public RelayCommand LoadNextPageCommand { get; }
        public RelayCommand SelectPageCommand { get; }

        public AsyncRelayCommand LoadedCommand { get; }

        public string TestMsg { get; set; } = "This is the test notification message";

        public ObservableCollection<ButtonModel> Buttons { get; set; }

        public int CurrentPageNumber { get; set; }

        //public RelayCommand ShowMessage {  get; set; } = new RelayCommand(o => { MessageBox.Show("hello"); });

        public RelayCommand ViewCommand { get; }

        public int RowsPerPage = 7;

        public RelayCommand CopyCommand { get; }

        private ClipboardContent _selectedDataItem;
        public ClipboardContent SelectedDataItem
        {
            get => _selectedDataItem;
            set
            {
                if (_selectedDataItem != value)
                {
                    _selectedDataItem = value;
                    OnPropertyChanged(nameof(SelectedDataItem));

                    //if (_selectedDataItem != null)
                    //{
                    //    RowClickCommand.Execute(_selectedDataItem);
                    //}
                }
            }
        }

        public ClipboardViewModel()
        {

            contents = new List<ClipboardContent>();

            LoadPreviousPageCommand = new RelayCommand(LoadPreviousPage);
            
            LoadNextPageCommand = new RelayCommand(LoadNextPage);

            LoadedCommand = new AsyncRelayCommand(OnLoadedAsync);


            SaveContentCommand = new RelayCommand(SaveContent);

            SelectPageCommand = new RelayCommand(SelectPage);

            ViewCommand = new RelayCommand(param => ViewContent(param));

            CopyCommand = new RelayCommand(param => CopyContent(param));

            //new DispatcherTimer(//It will not wait after the application is idle.
            //           TimeSpan.Zero,
            //           //It will wait until the application is idle
            //           DispatcherPriority.ApplicationIdle,
            //           //It will call this when the app is idle
            //           OnLoadedAsync,
            //           //On the UI thread
            //           Application.Current.Dispatcher);

            LoadedCommand.Execute(this);
        }

        public void SaveContent(object parameter)
        {
            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                ClipboardContent content = new TextClipboardContent(clipboardText);

                PagedData.AddItem(content);


                content.SaveToFile();
            }
            else
            {
                MessageBox.Show("No text found in clipboard.", "Clipboard Content");
            }
        }
        public void LoadNextPage(object parameter)
        {
            if (PagedData.CurrentPageIndex < PagedData.TotalPages - 1)
            {
                PagedData.LoadPage(PagedData.CurrentPageIndex + 1);
                OnPropertyChanged(nameof(PagedData));
                CurrentPageNumber = PagedData.CurrentPageIndex;
                OnPropertyChanged(nameof(CurrentPageNumber));
            }
        }

        public void LoadPreviousPage(object parameter)
        {
            if (PagedData.CurrentPageIndex > 0)
            {
                PagedData.LoadPage(PagedData.CurrentPageIndex - 1);
                OnPropertyChanged(nameof(PagedData));
                CurrentPageNumber = PagedData.CurrentPageIndex;
                OnPropertyChanged(nameof(CurrentPageNumber));
            }
        }
        

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            //MessageBox.Show($"Property changed: {propertyName}" + ": "+ CurrentPageNumber);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OnLoadedAsync(object parameter)
        {
            contents = await FetchDataAsync();
            Paging();

        }
        private void Paging()
        {
            Buttons = new ObservableCollection<ButtonModel>();
            PagedData = new PagedCollection<ClipboardContent>(contents, RowsPerPage);
            OnPropertyChanged(nameof(PagedData));
            CurrentPageNumber = PagedData.CurrentPageIndex;
            OnPropertyChanged(nameof(CurrentPageNumber));
            if (PagedData.TotalPages == 0)
            {
                Buttons.Add(new ButtonModel { page = "1"});
            }
            for (int i = 1; i < PagedData.TotalPages + 1; i++)
            {
                Buttons.Add(new ButtonModel { page = i.ToString() });
            }             
        }
        public void SelectPage(object parameter)
        {
            ButtonModel item = parameter as ButtonModel;
            if (item != null)
            {
                int index = int.Parse(item.page)-1;
                PagedData.LoadPage(index);
                OnPropertyChanged(nameof(PagedData));
                CurrentPageNumber = PagedData.CurrentPageIndex;
                OnPropertyChanged(nameof(CurrentPageNumber));
            }
        }
        private Task<List<ClipboardContent>> FetchDataAsync()
        {
            return Task.Run(() =>
            {
                string folderName = "ClipboardHistory";
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);

                // Check if the directory exists, and create it if it doesn't.
                if (!Directory.Exists(folderPath))
                {
                    try
                    {
                        Directory.CreateDirectory(folderPath);
                        // Optionally, you might want to log this creation for debugging or informational purposes.
                        Console.WriteLine($"Created directory: {folderPath}"); // Important:  Use proper logging, not Console.WriteLine in a real app.
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception appropriately.  Don't just swallow it.
                        Console.Error.WriteLine($"Error creating directory: {folderPath}.  Exception: {ex.Message}");
                        // Consider throwing a more specific exception or returning an empty list to indicate failure.
                        return new List<ClipboardContent>(); // Or throw, depending on your error handling policy
                    }
                }

                var textFiles = Directory.EnumerateFiles(folderPath, "*.txt");
                List<ClipboardContent> data = new List<ClipboardContent>();
                foreach (var file in textFiles.Reverse())
                {
                    string tmpStr = File.ReadAllText(file);
                    string tmpFileName = Path.GetFileName(file);
                    // Display or process the content as needed
                    data.Add(new TextClipboardContent(tmpStr, tmpFileName.Split(".")[0]));
                }
                return data;
            });
        }


        private void OpenViewWindow(ClipboardContent parameter)
        {

            var viewContentViewModel = new ViewContentViewModel(parameter);
            
            var viewContentWindow = new ViewContentWindow
            {
                DataContext = viewContentViewModel
            };

            // Show the edit window as a dialog
            viewContentWindow.ShowDialog();

            
        }

        private void ViewContent(object param)
        {
            if (param is ClipboardContent rowData)
            {
                OpenViewWindow(rowData);
            }
        }

        private void CopyContent(object param)
        {
            if (param is TextClipboardContent textContent)
            {
                Clipboard.SetText(textContent.FullText);
            }
            //else if (param is ImageClipboardContent imageContent)
            //{
            //    Clipboard.SetImage(imageContent.ImageSource); // example
            //}
        }



        public class ButtonModel
        {
            public string page { get; set; }
        }

    }
}
