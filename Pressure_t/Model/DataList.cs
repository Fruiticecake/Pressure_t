using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Text;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Drawing;
using CommunityToolkit.Mvvm.Input;

namespace Pressure_t.Model
{

    public interface IDialogService
    {
        Task<bool> ShowAlertAsync(string title, string message, string accept, string cancel);
    }

    public class DialogService : IDialogService
    {
        public async Task<bool> ShowAlertAsync(string title, string message, string accept, string cancel)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }
    }

    public partial class DataStorageListModel : INotifyPropertyChanged
    {
        const double MIN_ADC_VALUE = 0;

        public PickerCOM PickerCom { get; set; }
        public class DataStorage
        {
            public double Voltage { get; set; }
            public double ValueOfADC { get; set; }
            public double Pressure { get; set; }
        }


        public ObservableCollection<DataStorage> DataItems { get; set; }

        private int _comSelectedIndex;
        public int ComSelectedIndex
        {
            get => _comSelectedIndex;
            set
            {
                _comSelectedIndex = value;
                // 触发属性更改通知
            }
        }

        private int _baudRateSelectedIndex;
        public int BaudRateSelectedIndex
        {
            get => _baudRateSelectedIndex;
            set
            {
                _baudRateSelectedIndex = value;
                // 触发属性更改通知
            }
        }

        private int _sampleRateSelectedIndex;
        public int SampleRateSelectedIndex
        {
            get => _sampleRateSelectedIndex;
            set
            {
                _sampleRateSelectedIndex = value;
                // 触发属性更改通知
            }
        }

        private int _refSelectedIndex;
        public int RefSelectedIndex
        {
            get => _refSelectedIndex;
            set
            {
                _refSelectedIndex = value;
                // 触发属性更改通知
            }
        }

        private DataStorage _dataStorageListSelectedIndex;
        public DataStorage DataStorageListSelectedIndex
        {
            get => _dataStorageListSelectedIndex;
            set
            {
                _dataStorageListSelectedIndex = value;
                // 触发属性更改通知
            }
        }

        private ObservableCollection<ISeries> _series;
        public ObservableCollection<ISeries> Series
        {
            get => _series;
            set
            {
                _series = value;
                OnPropertyChanged(nameof(Series));
            }
        }

        private ObservableCollection<ISeries> _scrollbarSeries;
        public ObservableCollection<ISeries> ScrollbarSeries
        {
            get => _scrollbarSeries;
            set
            {
                _scrollbarSeries = value;
                OnPropertyChanged(nameof(ScrollbarSeries));
            }
        }

        public Axis[] ScrollableAxes { get; set; }
        public Axis[] InvisibleX { get; set; }
        public Axis[] InvisibleY { get; set; }
        public LiveChartsCore.Measure.Margin Margin { get; set; }
        public RectangularSection[] Thumbs { get; set; }

        private SerialPort _serialPort;
        // 命令属性
        public ICommand ConnectCommand { get; private set; }
        public ICommand DataSaveCommand { get; private set; }
        public ICommand DataClearCommand { get; private set; }
        public ICommand DataClearAllCommand { get; private set; }
        public ICommand ChangeModeCommand { get; private set; }

        private IDialogService _dialogService;


        private ObservableCollection<string> _availableCOM;
        public ObservableCollection<string> AvailableCOM
        {
            get => _availableCOM;
            set
            {
                _availableCOM = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableCOM)));
            }
        }

        private ObservableCollection<int> _baudRate;
        public ObservableCollection<int> BaudRate
        {
            get => _baudRate;
            set
            {
                _baudRate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BaudRate)));
            }
        }

        private ObservableCollection<int> _sampleRate;
        public ObservableCollection<int> SampleRate
        {
            get => _sampleRate;
            set
            {
                _sampleRate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SampleRate)));
            }
        }

        private ObservableCollection<double> _refVoltage;
        public ObservableCollection<double> RefVoltage
        {
            get => _refVoltage;
            set
            {
                _refVoltage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RefVoltage)));
            }
        }

        private string _buttonText = "ON";
        private bool _isSerialPortOpen = false;
        private Color _buttonTextColor = Color.FromRgba("#000000");
        private Color _textColor = Color.FromRgba("#000000");

        public string ButtonText
        {
            get => _buttonText;
            set
            {
                if (_buttonText != value)
                {
                    _buttonText = value;
                    OnPropertyChanged(nameof(ButtonText));
                }
            }
        }

        public Color ButtonTextColor
        {
            get => _buttonTextColor;
            set
            {
                if (_buttonTextColor != value)
                {
                    _buttonTextColor = value;
                    OnPropertyChanged(nameof(ButtonTextColor));
                }
            }
        }

        public Color TextColor
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    OnPropertyChanged(nameof(TextColor));
                }
            }
        }
        private string _rtA;
        public string RTA
        {
            get => _rtA;
            set
            {
                if (_rtA != value)
                {
                    _rtA = value;
                    OnPropertyChanged(nameof(RTA));
                }
            }
        }

        private double _rtANumeric;
        public double RTANumeric
        {
            get => _rtANumeric;
            set
            {
                if (_rtANumeric != value)
                {
                    _rtANumeric = value;
                    OnPropertyChanged(nameof(RTANumeric));
                }
            }
        }

        private double _rtVNumeric;
        public double RTVNumeric
        {
            get => _rtVNumeric;
            set
            {
                if (_rtVNumeric != value)
                {
                    _rtVNumeric = value;
                    OnPropertyChanged(nameof(RTVNumeric));
                }
            }
        }

        private double _pressureNumeric;
        public double PressureNumeric
        {
            get => _pressureNumeric;
            set
            {
                if (_pressureNumeric != value)
                {
                    _pressureNumeric = value;
                    OnPropertyChanged(nameof(PressureNumeric));
                }
            }
        }

        private string _modeText = "SingleMode";
        public string ModeText
        {
            get => _modeText;
            set
            {
                _modeText = value;
                OnPropertyChanged(nameof(ModeText));
                
            }
        }
        private bool _isSingleMode = true;
        //private bool _isMartixMode = false;

        public bool IsMartixMode => !_isSingleMode;
        public bool IsSingleMode
        {
            get => _isSingleMode;
            set
            {
                _isSingleMode = value;
                OnPropertyChanged(nameof(IsSingleMode));
                OnPropertyChanged(nameof(IsMartixMode));
            }
        }
        private bool _isDown = false;
        private bool _isDataSave = false;
        public bool IsDataSave
        {
            get => _isDataSave;
            set
            {
                _isDataSave = value;
                OnPropertyChanged(nameof(IsDataSave));
            }
        }


        public DataStorageListModel() { }

        public DataStorageListModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            PickerCOMInit();

            Series = new ObservableCollection<ISeries>
                {
                    new LineSeries<ObservablePoint>
                    {
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1)
                    },
                    new LineSeries<double>
                    {
                        Values = new ObservableCollection<double>(),
                        Fill = new SolidColorPaint(SKColors.CornflowerBlue)
                    }
                };

            ScrollbarSeries = new ObservableCollection<ISeries>
                {
                    new LineSeries<ObservablePoint>
                    {
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1)
                    }
                };

            ScrollableAxes = new[] { new Axis() };

            Thumbs = new[]
            {
                    new RectangularSection
                    {
                        Fill = new SolidColorPaint(new SKColor(255, 205, 210, 100))
                    }
                };

            InvisibleX = new[] { new Axis { IsVisible = false } };
            InvisibleY = new[] { new Axis { IsVisible = false } };

            var auto = LiveChartsCore.Measure.Margin.Auto;
            Margin = new(100, auto, 50, auto);
            DataItems = new ObservableCollection<DataStorage> { };

            DataSaveCommand = new Command(OnDataSaveClicked);
            DataClearCommand = new Command(OnDataClearClicked);
            DataClearAllCommand = new Command(OnDataClearAllClicked);
            ChangeModeCommand = new Command(OnModeButtonClicked);
        }

        public void PickerCOMInit()
        {
            // 初始化数据源
            AvailableCOM = new ObservableCollection<string> { };
            BaudRate = new ObservableCollection<int>
            {
                9600,   // 常见的低速连接，许多基础项目默认速率
                14400,  // 早期调制解调器使用的速度
                19200,  // 中速连接，常用于工业设备
                38400,  // 中等速度连接，更多的设备支持
                57600,  // 较高速度，用于快速数据传输
                115200, // 高速连接，常见于现代设备
                230400  // 高速连接，用于高速数据传输需求
            };

            SampleRate = new ObservableCollection<int>
            {
                8000,    // 电话质量，足以传递语音但缺乏细节
                11025,   // 低质量音频，早期电脑游戏常用
                16000,   // 较好的语音质量，适用于VoIP等
                22050,   // AM广播质量，也用于一些低质量音频处理
                32000,   // FM广播的近似质量，用于某些视频的音频轨
                44100,   // CD质量，成为数字音频处理的标准采样率
                48000,   // 专业音频和视频的标准采样率，广泛用于电影和DVD
                88200   // 高分辨率音频，用于专业音频编辑和处理
            };

            RefVoltage = new ObservableCollection<double>
            {
                1.0,    // 1.0V，用于低压应用
                1.8,    // 1.8V，常见于某些低功耗设备
                2.5,    // 2.5V，适用于中等范围的应用
                3.0,    // 3.0V，常用于电池供电的设备
                3.3,    // 3.3V，广泛用于逻辑电路和数字系统
                5.0,    // 5.0V，经典的电压值，用于许多传统和现代电子设备
                9.0    // 9.0V，常见于电池供电和一些模拟电路
            };

            ConnectCommand = new Command(ConnectToSerialPort);

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
                    AvailableCOM.Add(portName);
                }
                catch (COMException ex)
                {
                    // 可以记录异常信息，或者通知用户某个串口无法访问
                    Debug.WriteLine($"Error accessing COM port {portName}: {ex.Message}");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        // Implement PropertyChanged as needed to support UI updates
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private async void ConnectToSerialPort()
        {
            // 这里需要检查选中的串口是否有效
            string selectedPort = AvailableCOM[ComSelectedIndex];
            //Console.WriteLine("as"+selectedPort);
            if (!string.IsNullOrEmpty(selectedPort))
            {
                if (false == _isSerialPortOpen)
                {
                    _serialPort = new SerialPort(selectedPort, BaudRate[BaudRateSelectedIndex]);
                    _serialPort.DataReceived += SerialPort_DataReceived;

                    try
                    {
                        _serialPort.Open();
                        ButtonText = "OFF";
                        _isSerialPortOpen = true;
                        
                        ButtonTextColor = Color.FromRgb(0, 255, 0);
                        TextColor = Color.FromRgb(0, 255, 0);
                        await _dialogService.ShowAlertAsync("Connection", $"{selectedPort} 打开成功", "确认", "关闭");
                        // 可以在这里添加更多的设置，比如串口的参数等

                    }
                    catch (Exception ex)
                    {
                        // 处理串口打开异常
                        await _dialogService.ShowAlertAsync("Connection", $"{selectedPort} 打开失败: {ex.Message}", "确认", "关闭");
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {

                    try
                    {
                        _serialPort.Close();
                        ButtonText = "ON";
                        _isSerialPortOpen = false;
                        ButtonTextColor = Color.FromRgba("#000000");
                        TextColor = Color.FromRgba("#000000");
                        await _dialogService.ShowAlertAsync("Disconnection", $"{selectedPort} 关闭成功", "确认", "关闭");
                        // 可以在这里添加更多的设置，比如串口的参数等

                    }
                    catch (Exception ex)
                    {
                        // 处理串口打开异常
                        await _dialogService.ShowAlertAsync("Disconnection", $"{selectedPort} 关闭失败: {ex.Message}", "确认", "关闭");
                        Console.WriteLine(ex.Message);
                    }
                }

            }
        }

        private double _maxValue = MIN_ADC_VALUE;
        private readonly TimeSpan _resetInterval = TimeSpan.FromSeconds(1); // 设置重置间隔
        private DateTime _lastResetTime = DateTime.Now;
        private StringBuilder _serialBuffer = new StringBuilder();


        private void UpdateMaxValue(double newValue)
        {
            if (newValue > _maxValue)
            {
                _maxValue = newValue;
            }

            // 检查是否应该重置最大值
            if ((DateTime.Now - _lastResetTime) > _resetInterval)
            {
                // 这里可以做一些操作，例如记录或显示最大值
                Console.WriteLine($"Max value in the last interval: {_maxValue}");

                // 重置最大值
                _maxValue = MIN_ADC_VALUE;
                _lastResetTime = DateTime.Now;
            }
        }
        private Queue<double> _movingAverageQueue = new Queue<double>();
        private const int MovingAveragePeriod = 3; // 移动平均的周期，可以根据需要调整
        private double _movingAverageSum = 0;

        private void MovingAverageFilter(string value)
        {
            if (double.TryParse(value, out double numericValue))
            {
                // 将新值添加到队列中
                _movingAverageQueue.Enqueue(numericValue);
                _movingAverageSum += numericValue;

                // 如果队列中的元素数量超过了设定的周期，则移除最旧的元素
                if (_movingAverageQueue.Count > MovingAveragePeriod)
                {
                    _movingAverageSum -= _movingAverageQueue.Dequeue();
                }

                // 计算移动平均
                double movingAverage = _movingAverageSum / _movingAverageQueue.Count;

                // 更新移动平均值
                UpdateRTAValue(movingAverage.ToString());
            }
        }


        private void ProcessData(string data)
        {
            if (true == IsSingleMode)
            {
                //每个值都是以换行符分隔的
                string[] values = data.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var value in values)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // UpdateMaxValue(numericValue); // 更新最大值
                        // UpdateRTAValue(_maxValue.ToString()); // 更新 RTA 值
                        MovingAverageFilter(value);

                    });

                }
            }
            else
            {
                UpdateMartixValue(data);
            }

        }

        private void UpdateMartixValue(string data)
        {
            if(true == IsDataSave)
            {
                string[] values = data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                // 使用 LINQ 将字符串数组转换为整数列表
                var intValues = values.Select(s => {
                    bool success = int.TryParse(s, out int result);
                    return new { success, result };
                })
                .Where(x => x.success)
                .Select(x => x.result);
                Debug.WriteLine(string.Join(", ", values));
                Debug.WriteLine(string.Join(", ", intValues));
                if (Series.FirstOrDefault() is PolarLineSeries<int> polarLineSeries)
                {
                    polarLineSeries.Values = new ObservableCollection<int>();
                    if (polarLineSeries.Values is ObservableCollection<int> polarvalues)
                    {
                        foreach (var intValue in intValues)
                        {
                            polarvalues.Add(intValue);
                        }
                        Debug.WriteLine(string.Join(", ", polarvalues));
                    }
                }
                // 通知视图更新
                OnPropertyChanged(nameof(Series));
            }

        }

        int numCount = 0;
        private void UpdateRTAValue(string strValue)
        {
            if (double.TryParse(strValue, out double numericValue))
            {
                RTANumeric = numericValue;
                RTVNumeric = RTANumeric * (RefVoltage[RefSelectedIndex] / 4095);
                PressureNumeric = (10 * RTVNumeric) / RefVoltage[RefSelectedIndex];
            }
            else
            {
                //默认值或者处理错误
                RTANumeric = 0;
                RTVNumeric = 0;
                PressureNumeric = 0;
            }
            if (IsDataSave)
            {
                //if (Series.FirstOrDefault() is LineSeries<double> lineSeries)
                //{
                //    if (lineSeries.Values is ObservableCollection<double> values)
                //    {
                //        values.Add(PressureNumeric);
                //    }
                //}
                ObservablePoint _values = new ObservablePoint(++numCount, PressureNumeric);
                if (Series.FirstOrDefault() is LineSeries<ObservablePoint> lineSeries)
                {
                    if (lineSeries.Values is ObservableCollection<ObservablePoint> values)
                    {
                        values.Add(_values);
                    }
                }
                if (Series.FirstOrDefault() is LineSeries<double> lineDoubleSeries)
                {
                    if (lineDoubleSeries.Values is ObservableCollection<double> values)
                    {
                        values.Add(RTVNumeric);
                    }
                }


                if (ScrollbarSeries.FirstOrDefault() is LineSeries<ObservablePoint> scrollbarSeries)
                {
                    if (scrollbarSeries.Values is ObservableCollection<ObservablePoint> values)
                    {
                        values.Add(_values);
                    }
                }


            }

        }
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 处理接收到的数据
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            _serialBuffer.Append(indata); // 将接收到的数据追加到缓冲区
            // 如果数据包含了结束符（例如换行符），则处理数据
            if (_serialBuffer.ToString().IndexOf('\n') >= 0)
            {
                // 在这里处理完整的数据包
                Debug.WriteLine("这里是调试信息");
                string dataToProcess = _serialBuffer.ToString();
                _serialBuffer.Clear(); // 清除缓冲区，准备下一次数据接收
                
                // 将数据处理逻辑移动到另一个方法中，以便可以异步执行
                ProcessData(dataToProcess);
            }
        }

        private async void OnDataSaveClicked()
        {
            //DataItems.Add(new DataStorage { Voltage = RTVNumeric, ValueOfADC = RTANumeric, Pressure = PressureNumeric });

            //if (Series.FirstOrDefault() is LineSeries<double> lineSeries)
            //{
            //    if (lineSeries.Values is ObservableCollection<double> values)
            //    {
            //        values.Add(PressureNumeric);
            //    }
            //}
            bool replay;
            // 通知视图更新
            if(false == IsDataSave)
            {
                replay = await _dialogService.ShowAlertAsync("DataSave", "是否开始保存数据", "确认", "取消");
                if (replay)
                {
                    IsDataSave = true;
                }
            }
            else
            {
                replay = await _dialogService.ShowAlertAsync("DataSave", "是否停止保存数据", "确认", "取消");
                if (replay)
                {
                    IsDataSave = false;
                }
            }
            
            OnPropertyChanged(nameof(Series));
        }

        private async void OnDataClearClicked()
        {
            if (DataStorageListSelectedIndex != null && DataItems.Contains(DataStorageListSelectedIndex))
            {
                // 找到要删除的数据点在折线图中的索引
                var itemToRemove = DataStorageListSelectedIndex as DataStorage;
                int indexToRemove = DataItems.IndexOf(itemToRemove);

                // 删除列表中的数据点
                DataItems.Remove(itemToRemove);

                // 同时删除折线图中对应的数据点
                if (Series.FirstOrDefault() is LineSeries<double> lineSeries)
                {
                    if (lineSeries.Values is ObservableCollection<double> values)
                    {
                        // 确保索引有效
                        if (indexToRemove >= 0 && indexToRemove < values.Count)
                        {
                            values.RemoveAt(indexToRemove);
                        }
                    }
                }

                // 清除选中状态
                DataStorageListSelectedIndex = null;

                // 通知视图更新
                OnPropertyChanged(nameof(Series));
            }
            else
            {
                await _dialogService.ShowAlertAsync("", "请先选中需要删除的数据", "确认", "关闭");
            }
        }

        private async void OnDataClearAllClicked()
        {
            bool reply;
            reply = await _dialogService.ShowAlertAsync("清空数据", "是否要清空数据", "是", "否");

            if (reply)
            {
                DataItems.Clear();
                DataStorageListSelectedIndex = null;
                PressureNumeric = 0;
                RTANumeric = 0;
                RTVNumeric = 0;
                numCount = 0;
                if (Series.FirstOrDefault() is LineSeries<ObservablePoint> lineSeries)
                {
                    if (lineSeries.Values is ObservableCollection<ObservablePoint> values)
                    {
                        values.Clear();
                    }
                }

                if (ScrollbarSeries.FirstOrDefault() is LineSeries<ObservablePoint> scrollbarSeries)
                {
                    if (scrollbarSeries.Values is ObservableCollection<ObservablePoint> values)
                    {
                        values.Clear();
                    }
                }

                if (ScrollbarSeries.FirstOrDefault() is PolarLineSeries<int> polarLineSeries)
                {
                    if (polarLineSeries.Values is ObservableCollection<int> values)
                    {
                        values.Clear();
                    }
                }

                if (Series.FirstOrDefault() is LineSeries<double> lineDoubleSeries)
                {
                    if (lineDoubleSeries.Values is ObservableCollection<double> values)
                    {
                        values.Clear();
                    }
                }

            }



            // 通知视图更新
            OnPropertyChanged(nameof(Series));
        }

        public void OnModeButtonClicked()
        {
            if ("SingleMode" == ModeText)
            {
                ModeText = "MartixMode";
                IsSingleMode = false;
                Series = new ObservableCollection<ISeries>
                {
                    new PolarLineSeries<int>
                    {
                        //Values = new[] { 2, 7, 5, 9, 7 },
                        Values = new ObservableCollection<int>(),
                        LineSmoothness = 1,
                        GeometrySize= 0,
                        Fill = new SolidColorPaint(SKColors.Blue.WithAlpha(90))
                    },
                };
            }
            else
            {
                ModeText = "SingleMode";
                IsSingleMode = true;
                //Series = new ObservableCollection<ISeries>
                //{
                //    new LineSeries<double>
                //    {
                //        Values = new ObservableCollection<double>(),
                //        Fill = new SolidColorPaint(SKColors.CornflowerBlue)
                //    }
                //};

                Series = new ObservableCollection<ISeries>
                {
                    new LineSeries<ObservablePoint>
                    {
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1)
                    },
                    new LineSeries<double>
                    {
                        Values = new ObservableCollection<double>(),
                        Fill = new SolidColorPaint(SKColors.CornflowerBlue)
                    }
                };

                ScrollbarSeries = new ObservableCollection<ISeries>
                {
                    new LineSeries<ObservablePoint>
                    {
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1)
                    }
                };

                ScrollableAxes = new[] { new Axis() };

                Thumbs = new[]
                {
                    new RectangularSection
                    {
                        Fill = new SolidColorPaint(new SKColor(255, 205, 210, 100))
                    }
                };

                InvisibleX = new[] { new Axis { IsVisible = false } };
                InvisibleY = new[] { new Axis { IsVisible = false } };

                var auto = LiveChartsCore.Measure.Margin.Auto;
                Margin = new(100, auto, 50, auto);
            }
        }
        [RelayCommand]
        public void ChartUpdated(ChartCommandArgs args)
        {
            var cartesianChart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;

            var x = cartesianChart.XAxes.First();

            // update the scroll bar thumb when the chart is updated (zoom/pan)
            // this will let the user know the current visible range
            var thumb = Thumbs[0];

            thumb.Xi = x.MinLimit;
            thumb.Xj = x.MaxLimit;
        }

        [RelayCommand]
        public void PointerDown(PointerCommandArgs args)
        {
            _isDown = true;
        }

        [RelayCommand]
        public void PointerMove(PointerCommandArgs args)
        {
            if (!_isDown) return;

            var chart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;
            var positionInData = chart.ScalePixelsToData(args.PointerPosition);

            var thumb = Thumbs[0];
            var currentRange = thumb.Xj - thumb.Xi;

            // update the scroll bar thumb when the user is dragging the chart
            thumb.Xi = positionInData.X - currentRange / 2;
            thumb.Xj = positionInData.X + currentRange / 2;

            // update the chart visible range
            ScrollableAxes[0].MinLimit = thumb.Xi;
            ScrollableAxes[0].MaxLimit = thumb.Xj;
        }

        [RelayCommand]
        public void PointerUp(PointerCommandArgs args)
        {
            _isDown = false;
        }
    }


}

