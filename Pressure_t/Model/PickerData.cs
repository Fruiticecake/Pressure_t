using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pressure_t.Model
{
    public class PickerCOM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<string> _items;
        public ObservableCollection<string> availableCOM
        {
            get => _items;
            set
            {
                _items = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(availableCOM)));
            }
        }

        public PickerCOM()
        {
            // 初始化数据源
            availableCOM = new ObservableCollection<string>
                {
                    "COM6:通用串行输出口1",
                    "COM16:通用串行输出口2",
                    "COM26:通用串行输出口3"
                };
        }
    }
}
