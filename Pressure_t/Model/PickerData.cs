using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Timers;

using Timer = System.Timers.Timer;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Pressure_t.Model
{
    public class PickerCOM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private Timer refreshTimer;

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
            availableCOM = new ObservableCollection<string> { };

/*            // 设置定时器以定期刷新串口列表
            refreshTimer = new Timer(5000); // 设置定时器的间隔时间，例如5000毫秒（5秒）
            refreshTimer.Elapsed += OnTimedEvent; // 每当指定的时间间隔完成时执行的事件
            refreshTimer.AutoReset = true; // 设置是否重复执行
            refreshTimer.Enabled = true; // 启动定时器*/
            

            GetAllPortNames(); // 初始调用以填充列表
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            GetAllPortNames(); // 定期调用以刷新串口列表
        }

        public void GetAllPortNames()
        {
            string[] portNames = SerialPort.GetPortNames();

            // 添加新的串口到列表
            foreach (var portName in portNames)
            {
                try
                {
                    // 尝试添加串口到列表
                    availableCOM.Add(portName);
                }
                catch (COMException ex)
                {
                    // 可以记录异常信息，或者通知用户某个串口无法访问
                    Debug.WriteLine($"Error accessing COM port {portName}: {ex.Message}");
                }
            }
        }



    }
}
