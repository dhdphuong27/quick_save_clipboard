using Quicksave_Clipboard.MVVM.Model;
using Quicksave_Clipboard.MVVM.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Quicksave_Clipboard.MVVM.ViewModel
{
    public class ClipboardViewModel : ObservableObject
    {
        // --- backing fields ---
        private List<ClipboardContent> contents;
        private PagedCollection<ClipboardContent> _pagedData;
        private ObservableCollection<ButtonModel> _buttons;
        private int _currentPageNumber;
        public int _loadingCount;
        private bool _isLoaded;

        // --- properties bound to UI ---
        public PagedCollection<ClipboardContent> PagedData
        {
            get => _pagedData;
            private set => SetProperty(ref _pagedData, value);
        }

        public ObservableCollection<ButtonModel> Buttons
        {
            get => _buttons;
            set => SetProperty(ref _buttons, value);
        }

        public int CurrentPageNumber
        {
            get => _currentPageNumber;
            set => SetProperty(ref _currentPageNumber, value);
        }

        // true when any operation is running
        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }


        // --- commands (ICommand-compatible) ---
        public AsyncRelayCommand LoadedCommand { get; }
        public AsyncRelayCommand LoadPreviousPageCommand { get; }
        public AsyncRelayCommand LoadNextPageCommand { get; }
        public AsyncRelayCommand SelectPageCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public RelayCommand SaveContentCommand { get; }
        public RelayCommand CopyCommand { get; }
        public RelayCommand ViewCommand { get; }

        public int RowsPerPage = 7;

        public ClipboardViewModel()
        {
            contents = new List<ClipboardContent>();

            // async commands (so we can await and show spinner reliably)
            LoadedCommand = new AsyncRelayCommand(OnLoadedAsync);
            LoadPreviousPageCommand = new AsyncRelayCommand(async _ => await LoadPreviousPageAsync());
            LoadNextPageCommand = new AsyncRelayCommand(async _ => await LoadNextPageAsync());
            SelectPageCommand = new AsyncRelayCommand(async p => await SelectPageAsync(p));
            DeleteCommand = new AsyncRelayCommand(async p => await DeleteContentAsync(p));

            // quick sync commands (UI-thread only)
            SaveContentCommand = new RelayCommand(SaveContent);
            CopyCommand = new RelayCommand(param => CopyContent(param));
            ViewCommand = new RelayCommand(param => ViewContent(param));


        }

        // ---------------- Loading counter helpers ----------------
        private void BeginLoading()
        {
            _loadingCount++;
            SetIsLoading();
            OnPropertyChanged(nameof(IsLoading));
        }
        private void SetIsLoading()
        {
            if (_loadingCount > 0) IsLoading = true;
            else IsLoading = false;
            OnPropertyChanged(nameof(IsLoading));
        }

        private void EndLoading()
        {
            if (_loadingCount > 0) _loadingCount--;
            
            SetIsLoading();
            OnPropertyChanged(nameof(IsLoading));
        }
        private async Task EndLoadingAfterRender()
        {
            await Application.Current.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            //MessageBox.Show("EndLoadingAfterRender");
            EndLoading();
        }


        // ---------------- Public actions ----------------

        // Save current clipboard text (must run on UI thread because of Clipboard API)
        public void SaveContent(object parameter)
        {
            if (Clipboard.ContainsText())
            {
                BeginLoading();
                try
                {
                    string clipboardText = Clipboard.GetText();
                    var content = new TextClipboardContent(clipboardText);

                    // persist to disk (synchronous here; it's small text write)
                    content.SaveToFile();

                    // keep newest items at top
                    contents.Insert(0, content);

                    // refresh paging (keeps you on the current page if possible)
                    Paging(preserveCurrentPage: true);
                }
                finally
                {
                    EndLoadingAfterRender();
                }
            }
            else if (Clipboard.ContainsImage())
            {
                BitmapSource clipboardImage = Clipboard.GetImage();
                var content = new ImageClipboardContent(clipboardImage);
                content.SaveToFile();
                contents.Insert(0, content);
                Paging(preserveCurrentPage: true);
            }
            else
            {
                MessageBox.Show("Clipboard does not contain text or image data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            

            
        }

        private void CopyContent(object param)
        {
            if (param is TextClipboardContent textContent)
            {
                Clipboard.SetText(textContent.FullText);
            }
        }

        private void ViewContent(object param)
        {
            if (param is TextClipboardContent textContent)
            {
                OpenViewWindow(textContent);
            }else if (param is ImageClipboardContent imageContent)
            {
                imageContent.OpenImage();
            }
        }

        // ---------------- Async lifecycle / loading ----------------
        private async Task OnLoadedAsync(object parameter)
        {
            if (_isLoaded) return;
            _isLoaded = true;

            BeginLoading();
            try
            {
                contents = await Task.Run(() => FetchDataAsync());
                Paging();
            }
            finally
            {
                await EndLoadingAfterRender();
            }
        }

        private Task<List<ClipboardContent>> FetchDataAsync()
        {
            return Task.Run(() =>
            {
                string folderName = "ClipboardHistory";
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);

                if (!Directory.Exists(folderPath))
                {
                    try
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    catch
                    {
                        return new List<ClipboardContent>();
                    }
                }

                var textFiles = Directory.EnumerateFiles(folderPath, "*.txt");
                var pngFiles = Directory.EnumerateFiles(folderPath, "*.png");
                var allFiles = textFiles.Concat(pngFiles);
                var data = new List<ClipboardContent>();
                foreach (var file in allFiles.Reverse())
                {
                    string tmpFileName = Path.GetFileNameWithoutExtension(file);
                    
                    if (Path.GetExtension(file).ToLower() == ".txt")
                    {
                        string tmpStr = File.ReadAllText(file);
                        data.Add(new TextClipboardContent(tmpStr, tmpFileName));
                    }
                    else if (Path.GetExtension(file).ToLower() == ".png")
                    {
                        data.Add(new ImageClipboardContent(tmpFileName));
                    }
                }
                return data;
            });
        }

        // ---------------- Paging helpers ----------------
        private void Paging(bool preserveCurrentPage = false)
        {
            // create new PagedCollection from current contents
            PagedData = new PagedCollection<ClipboardContent>(contents, RowsPerPage);

            // choose page to show
            int pageToLoad = 0;
            if (preserveCurrentPage)
            {
                // try keep same page index if possible
                pageToLoad = Math.Min(CurrentPageNumber, Math.Max(0, PagedData.TotalPages - 1));
            }

            if (PagedData.TotalPages > 0)
                PagedData.LoadPage(pageToLoad);
            else
                PagedData.LoadPage(0);

            CurrentPageNumber = PagedData.CurrentPageIndex;

            // build page buttons
            var list = new ObservableCollection<ButtonModel>();
            if (PagedData.TotalPages == 0)
                list.Add(new ButtonModel { page = "1" });
            for (int i = 1; i <= PagedData.TotalPages; i++)
                list.Add(new ButtonModel { page = i.ToString() });

            Buttons = list;
        }

        private async Task LoadPageAsync(int index)
        {
            BeginLoading();

            try
            {
                // Give WPF a chance to update UI and show progress bar
                await Task.Yield();

                // Do the page load on UI thread
                PagedData.LoadPage(index);

                CurrentPageNumber = PagedData.CurrentPageIndex;
            }
            finally
            {
                await EndLoadingAfterRender();
            }
        }





        private async Task LoadNextPageAsync()
        {
            if (PagedData.CurrentPageIndex < PagedData.TotalPages - 1)
                await LoadPageAsync(PagedData.CurrentPageIndex + 1);
        }

        private async Task LoadPreviousPageAsync()
        {
            if (PagedData.CurrentPageIndex < PagedData.TotalPages - 1 && PagedData.CurrentPageIndex > 0)
                await LoadPageAsync(PagedData.CurrentPageIndex -1);
        }
        
        private async Task SelectPageAsync(object parameter)
        {
            if (parameter is ButtonModel item && int.TryParse(item.page, out var oneBased))
            {
                var index = Math.Max(0, oneBased - 1);
                await LoadPageAsync(index);
            }
        }

        // ---------------- Delete ----------------
        private async Task DeleteContentAsync(object param)
        {
            if (param is ClipboardContent content)
            {
                BeginLoading();
                try
                {
                    await Task.Run(() => content.DeleteFile());

                    // Update UI-bound list on UI thread
                    contents.Remove(content);
                    Paging(preserveCurrentPage: true);
                }
                finally
                {
                    await EndLoadingAfterRender();
                }
            }
        }


        // ---------------- Helper / View helpers ----------------
        private void OpenViewWindow(ClipboardContent parameter)
        {
            var viewContentViewModel = new ViewContentViewModel(parameter);
            var viewContentWindow = new ViewContentWindow
            {
                DataContext = viewContentViewModel
            };
            viewContentWindow.ShowDialog();
        }

        // ---------------- PropertyChanged wiring ----------------
        // If your ObservableObject already provides this, you can remove this block.
        public new event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        // ButtonModel nested class kept for compatibility with your XAML
        public class ButtonModel
        {
            public string page { get; set; }
        }
    }
}
