using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pressure_t
{
    internal class AvailableCOM
    {

        public class MainViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private ObservableCollection<string> _items;
            public ObservableCollection<string> Items
            {
                get => _items;
                set
                {
                    _items = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
                }
            }

            public MainViewModel()
            {
                // 初始化数据源
                Items = new ObservableCollection<string>
        {
            "选项1",
            "选项2",
            "选项3"
        };
            }
        }

    }
}
