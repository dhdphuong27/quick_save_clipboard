using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quicksave_Clipboard.MVVM.Model
{
    public class PagedCollection<T> : ObservableCollection<T>
    {
        private List<T> _allItems;
        private int _itemsPerPage;
        private int _currentPageIndex;


        public PagedCollection(List<T> items, int itemsPerPage)
        {
            _allItems = items;
            _itemsPerPage = itemsPerPage;
            _currentPageIndex = 0;
            LoadPage(0);
        }
        public void AddItem(T item)
        {
            _allItems.Insert(0, item);
            LoadPage(0);
        }

        public void LoadPage(int pageIndex)
        {
            _currentPageIndex = pageIndex;
            ClearItems();
            foreach (var item in _allItems.Skip(pageIndex * _itemsPerPage).Take(_itemsPerPage))
            {
                Add(item);
            }
        }
        public int GetPage()
        {
            return _itemsPerPage;
        }

        public int TotalPages => (int)Math.Ceiling((double)_allItems.Count / _itemsPerPage);

        public int CurrentPageIndex => _currentPageIndex;
    }

}
