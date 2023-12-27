using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pressure_t.Model
{

    public class DataStorageList : INotifyPropertyChanged
    {
        public ObservableCollection<DataStorage> DataItems { get; set; }

        public DataStorageList()
        {
            DataItems = new ObservableCollection<DataStorage>
            {
                new DataStorage { Voltage = 3.3, ValueOfADC = 1234, Pressure= 50},
                new DataStorage { Voltage = 5.3, ValueOfADC = 232, Pressure= 52},
                new DataStorage { Voltage = 5.3, ValueOfADC = 232, Pressure= 76 },
                new DataStorage { Voltage = 5.3, ValueOfADC = 232, Pressure= 32 },
                new DataStorage { Voltage = 5.31, ValueOfADC = 232, Pressure= 98 },


                // Add more items here
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        // Implement PropertyChanged as needed to support UI updates
    }
    public class DataStorage
    {
        public double Voltage { get; set; }

        public double ValueOfADC {  get; set; }

        public double Pressure { get; set; }
    }

}

