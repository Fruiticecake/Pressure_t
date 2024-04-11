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
using System.Runtime.CompilerServices;
using System;
using System.Reflection.Metadata;
using Microsoft.Maui.Graphics;
using ClosedXML.Excel;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using Timer = System.Threading.Timer;

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
    public class PressurePoint : INotifyPropertyChanged
    {
        public Color GetColorFromPressure(double pressure)
        {
            // 将压力值从0-10的范围转换为0-1的范围
            pressure = Math.Clamp(pressure, 0, 4095) / 4000.0;

            // 白色的RGB分量
            float whiteR = 1f;
            float whiteG = 1f;
            float whiteB = 1f;

            // 深红色的RGB分量（例如：RGB为139, 0, 0）
            float darkRedR = 139f / 255f;
            float darkRedG = 0f;
            float darkRedB = 0f;

            // 计算新颜色的RGB值
            float red = whiteR + (darkRedR - whiteR) * (float)pressure;
            float green = whiteG + (darkRedG - whiteG) * (float)pressure;
            float blue = whiteB + (darkRedB - whiteB) * (float)pressure;

            // 返回新颜色
            return new Color(red, green, blue);
        }
        public Color PressureColor => GetColorFromPressure(MartixValueItem);
        private double _martixValueitem;
        public double MartixValueItem
        {
            get => _martixValueitem;
            set
            {
                _martixValueitem = value;
                OnPropertyChanged(nameof(MartixValueItem));
                OnPropertyChanged(nameof(PressureColor));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        // Implement PropertyChanged as needed to support UI updates
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class DataStorageListModel : INotifyPropertyChanged
    {
        const double MIN_ADC_VALUE = 0;
        public Color OnButtonActiveColor = Color.FromRgb(0, 255, 0);
        public Color OnButtonNormalColor = Color.FromRgb(139, 0, 0);
        public Color OnMartixActiveColor = Color.FromRgb(30, 144, 255);
        public Color OnMartixNormalColor = Color.FromRgb(211, 211, 211);
        private static readonly SKColor s_blue = new(25, 118, 210);
        private static readonly SKColor s_red = new(229, 57, 53);

        private ConcurrentQueue<SinglePointStorage> dataQueue = new ConcurrentQueue<SinglePointStorage>();
        private ConcurrentQueue<MartixPointStorage> dataMartixQueue = new ConcurrentQueue<MartixPointStorage>();

        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private Timer FileSaveTimer;
        private Timer FileMartixSaveTimer;

        // public ObservableCollection<PressurePoint> PressurePoints { get; set; }

        private ObservableCollection<PressurePoint> _pressurePoints;
        public ObservableCollection<PressurePoint> PressurePoints
        {
            get => _pressurePoints;
            set
            {
                _pressurePoints = value;
                OnPropertyChanged(nameof(PressurePoints));
            }
        }

        public PickerCOM PickerCom { get; set; }
        
        //public class DataStorage
        //{
        //    public double Voltage { get; set; }
        //    public double ValueOfADC { get; set; }
        //    public double Pressure { get; set; }
        //}

        // public ObservableCollection<DataStorage> DataItems { get; set; }

        public class DataExcelPath
        {
            public string ExcelPath { get; set; }
        }

        public ObservableCollection<DataExcelPath> DataItems { get; set; }

        public DataStorageListModel() { }

        public DataStorageListModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            PickerCOMInit();
            FileSaveTimer = new Timer(ProcessDataQueue, null, 0, 1000);
            FileMartixSaveTimer = new Timer(ProcessMartixDataQueue, null, 0, 1000);

            Series = new ObservableCollection<ISeries>
                {
                    new LineSeries<ObservablePoint>
                    {
                        Name = "Pressure",
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1),
                        ScalesYAt = 0
                    },

                    new LineSeries<ObservablePoint>
                    {
                        Name = "Voltage",
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1),
                        ScalesYAt = 1
                    }
                };

            ScrollbarSeries = new ObservableCollection<ISeries>
                {
                    new LineSeries<ObservablePoint>
                    {
                        Name = "Pressure",
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1)
                    },
                    new LineSeries<ObservablePoint>
                    {
                        Name = "Voltage",
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1)
                    }
                };

            YAxes = new ICartesianAxis[]
{
                    new Axis // the "units" and "tens" series will be scaled on this axis
                    {
                        Name = "Pressure/g",
                        NameTextSize = 14,
                        NamePaint = new SolidColorPaint(s_blue),
                        NamePadding = new LiveChartsCore.Drawing.Padding(0, 20),
                        Padding =  new LiveChartsCore.Drawing.Padding(0, 0, 20, 0),
                        TextSize = 12,
                        LabelsPaint = new SolidColorPaint(s_blue),
                        TicksPaint = new SolidColorPaint(s_blue),
                        SubticksPaint = new SolidColorPaint(s_blue),
                        DrawTicksPath = true
                    },
                    new Axis // the "hundreds" series will be scaled on this axis
                    {
                        Name = "Voltage/V",
                        NameTextSize = 14,
                        MinLimit = 0,
                        MaxLimit = RefVoltage[RefSelectedIndex],
                        NamePaint = new SolidColorPaint(s_red),
                        NamePadding = new LiveChartsCore.Drawing.Padding(0, 5),
                        Padding =  new LiveChartsCore.Drawing.Padding(5, 0, 0, 0),
                        TextSize = 12,
                        LabelsPaint = new SolidColorPaint(s_red),
                        TicksPaint = new SolidColorPaint(s_red),
                        SubticksPaint = new SolidColorPaint(s_red),
                        DrawTicksPath = true,
                        ShowSeparatorLines = false,
                        Position = LiveChartsCore.Measure.AxisPosition.End
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
            // DataItems = new ObservableCollection<DataStorage> { };
            DataItems = new ObservableCollection<DataExcelPath> { };

            PressurePoints = new ObservableCollection<PressurePoint>();
            for (int i = 0; i < 18; i++)
            {
                PressurePoints.Add(new PressurePoint());
            }
            MartixSettingCommand = new Command<string>(OnMartixSettingCommand);
            DataSaveCommand = new Command(OnDataSaveClicked);
            DataClearCommand = new Command(OnDataClearClicked);
            DataClearAllCommand = new Command(OnDataClearAllClicked);
            ChangeModeCommand = new Command(OnModeButtonClicked);
        }
        public SolidColorPaint LegendTextPaint { get; set; } =
        new SolidColorPaint
        {
            Color = new SKColor(50, 50, 50),
            SKTypeface = SKFontManager.Default.MatchCharacter('汉')
        };


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

        private int _baudRateSelectedIndex = 5;
        public int BaudRateSelectedIndex
        {
            get => _baudRateSelectedIndex;
            set
            {
                _baudRateSelectedIndex = value;
                // 触发属性更改通知
            }
        }

        private int _sampleRateSelectedIndex = 2;
        public int SampleRateSelectedIndex
        {
            get => _sampleRateSelectedIndex;
            set
            {
                _sampleRateSelectedIndex = value;
                // 触发属性更改通知
            }
        }

        private int _refSelectedIndex = 4;
        public int RefSelectedIndex
        {
            get => _refSelectedIndex;
            set
            {
                _refSelectedIndex = value;
                // 触发属性更改通知
            }
        }

        //private DataStorage _dataStorageListSelectedIndex;
        //public DataStorage DataStorageListSelectedIndex
        //{
        //    get => _dataStorageListSelectedIndex;
        //    set
        //    {
        //        _dataStorageListSelectedIndex = value;
        //        // 触发属性更改通知
        //    }
        //}

        private DataExcelPath _dataExcelPathListSelectedIndex;
        public DataExcelPath DataExcelPathListSelectedIndex
        {
            get => _dataExcelPathListSelectedIndex;
            set
            {
                _dataExcelPathListSelectedIndex = value;
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
        public ICartesianAxis[] YAxes { get; set; }

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
        public ICommand MartixSettingCommand { get; private set; }


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

        private ObservableCollection<string> _sampleRate;
        public ObservableCollection<string> SampleRate
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
        private Color _buttonTextColor_MartixLeft = Color.FromRgb(139, 0, 0);
        private Color _buttonTextColor_MartixRight = Color.FromRgb(139, 0, 0);
        private Color _martixLeftBgColor = Color.FromRgba("#bdc3c7");
        private Color _martixRightBgColor = Color.FromRgba("#bdc3c7");

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

        public Color ButtonTextColor_MartixRight
        {
            get => _buttonTextColor_MartixRight;
            set
            {
                if (_buttonTextColor_MartixRight != value)
                {
                    _buttonTextColor_MartixRight = value;
                    OnPropertyChanged(nameof(ButtonTextColor_MartixRight));
                }
            }
        }

        public Color ButtonTextColor_MartixLeft
        {
            get => _buttonTextColor_MartixLeft;
            set
            {
                if (_buttonTextColor_MartixLeft != value)
                {
                    _buttonTextColor_MartixLeft = value;
                    OnPropertyChanged(nameof(ButtonTextColor_MartixLeft));
                }
            }
        }

        public Color MartixLeftBgColor
        {
            get => _martixLeftBgColor;
            set
            {
                if (_martixLeftBgColor != value)
                {
                    _martixLeftBgColor = value;
                    OnPropertyChanged(nameof(MartixLeftBgColor));
                }
            }
        }

        public Color MartixRightBgColor
        {
            get => _martixRightBgColor;
            set
            {
                if (_martixRightBgColor != value)
                {
                    _martixRightBgColor = value;
                    OnPropertyChanged(nameof(MartixRightBgColor));
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

        private bool _isRightFoot = false;
        public bool IsRightFoot
        {
            get => _isRightFoot;
            set
            {
                _isRightFoot = value;
                OnPropertyChanged(nameof(IsRightFoot));
            }
        }

        private bool _isLeftFoot = false;
        public bool IsLeftFoot
        {
            get => _isLeftFoot;
            set
            {
                _isLeftFoot = value;
                OnPropertyChanged(nameof(IsLeftFoot));
            }
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

            SampleRate = new ObservableCollection<string>
            {
                // 高采样率
                "50 KSPS",  // 每秒5万次
                "100 KSPS", // 每秒10万次
                "500 KSPS", // 每秒50万次
    
                // 非常高的采样率（取决于具体的STM32型号和配置）
                "1 MSPS", // 每秒100万次
                "2.4 MSPS", // stm32F401
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
                        
                        ButtonTextColor = OnButtonActiveColor;
                        TextColor = OnButtonActiveColor;
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
                //string[] values = data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                // 使用 LINQ 将字符串数组转换为整数列表
                //var intValues = values.Select(s => {
                //    bool success = int.TryParse(s, out int result);
                //    return new { success, result };
                //})
                //.Where(x => x.success)
                //.Select(x => x.result);
                //Debug.WriteLine(string.Join(", ", values));
                //Debug.WriteLine(string.Join(", ", intValues));
                //if (Series.FirstOrDefault() is PolarLineSeries<int> polarLineSeries)
                //{
                //    polarLineSeries.Values = new ObservableCollection<int>();
                //    if (polarLineSeries.Values is ObservableCollection<int> polarvalues)
                //    {
                //        foreach (var intValue in intValues)
                //        {
                //            polarvalues.Add(intValue);
                //        }
                //        Debug.WriteLine(string.Join(", ", polarvalues));
                //    }
                //}
                // 通知视图更新
                // OnPropertyChanged(nameof(Series));

                string[] pressureValues = data.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                string filePath = CreateFile("PressureMartixData");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Debug.WriteLine(string.Join(", ", pressureValues));
                    if (pressureValues.Length == 18)
                    {
                        for (int i = 0; i < 18; i++)
                        {
                            // 更新压力值，并通知UI
                            // UpdatePressurePoint(i, double.Parse(pressureValues[i]));
                            PressurePoints[i].MartixValueItem = int.Parse(pressureValues[i]);
                            // Debug.WriteLine(PressurePoints[i].MartixValueItem);
                            
                        }
                        ReciveMartixData(pressureValues);
                        // SaveMartixData(filePath);
                        OnPropertyChanged(nameof(PressurePoints));
                    }
                });

            }

        }
        // 优化后的异步执行保存操作
        public async Task SaveDataAsync(SinglePointStorage data)
        {
            try
            {
                string filePath = CreateFile("PressureSingleData"); 

                // 异步执行保存操作，以减少对UI线程的影响
                await Task.Run(() => SaveSingleData(filePath, data)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 处理可能发生的异常，例如记录日志或通知用户
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        // 优化后的异步执行保存操作
        public async Task SaveMartixDataAsync(MartixPointStorage data)
        {
            try
            {
                string filePath = CreateFile("PressureMartixData");

                // 异步执行保存操作，以减少对UI线程的影响
                await Task.Run(() => SaveMartixData(filePath, data)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 处理可能发生的异常，例如记录日志或通知用户
                Console.WriteLine($"An error occurred: {ex.Message}");
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
                //数据加入队列中
                ReciveSingleData();


                // 添加图表数据
                AddChartData(++numCount, PressureNumeric, RTVNumeric);

            }

        }
        private void AddToSeries(LineSeries<ObservablePoint> series, double x, double y)
        {
            if (series.Values is ObservableCollection<ObservablePoint> values)
            {
                values.Add(new ObservablePoint(x, y));

                // 如果数据点过多，考虑移除最旧的数据点
                //if (values.Count > MAX_DATA_POINTS)
                //{
                //    values.RemoveAt(0);
                //}
            }
        }
        private void AddChartData(double xValue, double primaryYValue, double secondaryYValue)
        {
            // 主线程中执行UI更新
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddToSeries((LineSeries<ObservablePoint>)Series[0], xValue, primaryYValue);
                AddToSeries((LineSeries<ObservablePoint>)Series[1], xValue, secondaryYValue);
                AddToSeries((LineSeries<ObservablePoint>)ScrollbarSeries[0], xValue, primaryYValue);
                AddToSeries((LineSeries<ObservablePoint>)ScrollbarSeries[1], xValue, secondaryYValue);
            });
        }

        public class SinglePointStorage
        {
            public string DataTime { get; set; }
            public double RTANumeric { get; set; }
            public double RTVNumeric { get; set; }
            public double PressureNumeric { get; set; }
        }

        private void ReciveSingleData()
        {
            SinglePointStorage data = new SinglePointStorage()
            {
                DataTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                PressureNumeric = PressureNumeric,
                RTANumeric = RTANumeric,
                RTVNumeric = RTVNumeric,
            };
            // 将接收到的数据放入队列
            dataQueue.Enqueue(data);
        }

        public class MartixPointStorage
        {
            public string DataTime { get; set; }
            public bool IsRight { get; set; }
            public bool IsLeft { get; set; }
            public int[] MartixPointArray { get; set; } = new int[18];
        }

        private void ReciveMartixData(string[] pressureValues)
        {
            // 将字符串数组转换为整数数组
            int[] _martixPointArray = Array.ConvertAll(pressureValues, int.Parse);

            MartixPointStorage data = new MartixPointStorage()
            {
                DataTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                IsRight = IsRightFoot,
                IsLeft = IsLeftFoot,
                MartixPointArray = _martixPointArray,

            };
            // 将接收到的数据放入队列
            dataMartixQueue.Enqueue(data);
        }

        private async void ProcessDataQueue(object state)
        {
            // 批量处理队列中的数据
            // 注意：这里的实现需要确保线程安全
            while (!dataQueue.IsEmpty)
            {
                if (dataQueue.TryDequeue(out SinglePointStorage data))
                {
                    await semaphoreSlim.WaitAsync(); // 异步等待获取锁
                    try
                    {
                        // 异步保存数据，SaveDataAsync 是一个异步方法
                        await SaveDataAsync(data);
                    }
                    finally
                    {
                        semaphoreSlim.Release(); // 释放锁
                    }
                }
            }
        }

        private async void ProcessMartixDataQueue(object state)
        {
            // 批量处理队列中的数据
            // 注意：这里的实现需要确保线程安全
            while (!dataMartixQueue.IsEmpty)
            {
                if (dataMartixQueue.TryDequeue(out MartixPointStorage data))
                {
                    await semaphoreSlim.WaitAsync(); // 异步等待获取锁
                    try
                    {
                        // 异步保存数据
                        await SaveMartixDataAsync(data);
                    }
                    finally
                    {
                        semaphoreSlim.Release(); // 释放锁
                    }
                }
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (IsDataSave)
            {
                // 处理接收到的数据
                SerialPort sp = (SerialPort)sender;
                string indata = sp.ReadExisting();
                Debug.WriteLine(indata);
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

        }
        private string CreateFile(string fileTitle)
        {
            // 获取当前日期并将其转换为字符串格式，例如 "2024-03-25"
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string fileName = $"{fileTitle}-{date}.xlsx";  // 创建基于日期的文件名

            // 指定文件路径
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = Path.Combine(documentsPath, fileName);

            // 检查文件是否存在，如果不存在则创建一个新的工作簿
            if (!File.Exists(filePath))
            {
                using (var workbook = new XLWorkbook())
                {
                    workbook.AddWorksheet("Pressure Sheet");
                    workbook.SaveAs(filePath);
                }
            }

            // 检查DataItems中是否包含该文件名
            bool fileExists = DataItems.Any(item => item.ExcelPath.Equals(filePath));
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (false == fileExists)
                {
                    DataItems.Add(new DataExcelPath { ExcelPath = filePath });
                    OnPropertyChanged(nameof(DataItems));
                }
            });
            return filePath;

        }

        private void SaveSingleData(string filePath, SinglePointStorage data)
        {

            // 打开现有的工作簿并添加数据
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet("Pressure Sheet");

                // 查找下一个空白行
                var lastRowCell = worksheet.LastRowUsed();
                int lastRowUsed = lastRowCell != null ? lastRowCell.RowNumber() : 0;
                int nextRow = lastRowUsed + 1;

                if (lastRowUsed == 0)
                {
                    worksheet.Cell("A" + nextRow).Value = "日期与时间";
                    worksheet.Cell("B" + nextRow).Value = "ADC数值";
                    worksheet.Cell("C" + nextRow).Value = "电压/V";
                    worksheet.Cell("D" + nextRow).Value = "压力/N";
                }
                else
                {
                    // 要添加的数据
                    //worksheet.Cell("A" + nextRow).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); 
                    worksheet.Cell("A" + nextRow).Value = data.DataTime;
                    worksheet.Cell("B" + nextRow).Value = data.RTANumeric;
                    worksheet.Cell("C" + nextRow).Value = data.RTVNumeric;
                    worksheet.Cell("D" + nextRow).Value = data.PressureNumeric;
                }

                // 保存工作簿
                try
                {
                    workbook.Save();
                }
                catch (Exception ex)
                {
                    // 如果有异常发生，保存操作失败
                    Console.WriteLine("Failed to save the workbook: " + ex.Message);
                }

            }
        }

        private async void SaveMartixData(string filePath, MartixPointStorage data)
        {

            // 打开现有的工作簿并添加数据
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet("Pressure Sheet");

                    // 查找下一个空白行
                    var lastRowCell = worksheet.LastRowUsed();
                    int lastRowUsed = lastRowCell != null ? lastRowCell.RowNumber() : 0;
                    int nextRow = lastRowUsed + 1;

                    if (lastRowUsed == 0)
                    {
                        worksheet.Cell("A" + nextRow).Value = "日期与时间";
                        // 从"B"列开始填充L1到L18的质量标题
                        worksheet.Cell("B" + nextRow).Value = "L1(g)";
                        worksheet.Cell("C" + nextRow).Value = "L2(g)";
                        worksheet.Cell("D" + nextRow).Value = "L3(g)";
                        worksheet.Cell("E" + nextRow).Value = "L4(g)";
                        worksheet.Cell("F" + nextRow).Value = "L5(g)";
                        worksheet.Cell("G" + nextRow).Value = "L6(g)";
                        worksheet.Cell("H" + nextRow).Value = "L7(g)";
                        worksheet.Cell("I" + nextRow).Value = "L8(g)";
                        worksheet.Cell("J" + nextRow).Value = "L9(g)";
                        worksheet.Cell("K" + nextRow).Value = "L10(g)";
                        worksheet.Cell("L" + nextRow).Value = "L11(g)";
                        worksheet.Cell("M" + nextRow).Value = "L12(g)";
                        worksheet.Cell("N" + nextRow).Value = "L13(g)";
                        worksheet.Cell("O" + nextRow).Value = "L14(g)";
                        worksheet.Cell("P" + nextRow).Value = "L15(g)";
                        worksheet.Cell("Q" + nextRow).Value = "L16(g)";
                        worksheet.Cell("R" + nextRow).Value = "L17(g)";
                        worksheet.Cell("S" + nextRow).Value = "L18(g)";

                        // 继续添加R1到R18的质量标题，从"T"列开始
                        worksheet.Cell("T" + nextRow).Value = "R1(g)";
                        worksheet.Cell("U" + nextRow).Value = "R2(g)";
                        worksheet.Cell("V" + nextRow).Value = "R3(g)";
                        worksheet.Cell("W" + nextRow).Value = "R4(g)";
                        worksheet.Cell("X" + nextRow).Value = "R5(g)";
                        worksheet.Cell("Y" + nextRow).Value = "R6(g)";
                        worksheet.Cell("Z" + nextRow).Value = "R7(g)";
                        worksheet.Cell("AA" + nextRow).Value = "R8(g)";
                        worksheet.Cell("AB" + nextRow).Value = "R9(g)";
                        worksheet.Cell("AC" + nextRow).Value = "R10(g)";
                        worksheet.Cell("AD" + nextRow).Value = "R11(g)";
                        worksheet.Cell("AE" + nextRow).Value = "R12(g)";
                        worksheet.Cell("AF" + nextRow).Value = "R13(g)";
                        worksheet.Cell("AG" + nextRow).Value = "R14(g)";
                        worksheet.Cell("AH" + nextRow).Value = "R15(g)";
                        worksheet.Cell("AI" + nextRow).Value = "R16(g)";
                        worksheet.Cell("AJ" + nextRow).Value = "R17(g)";
                        worksheet.Cell("AK" + nextRow).Value = "R18(g)";
                    }
                    else
                    {
                        // 要添加的数据

                        if (data.IsLeft)
                        {
                            worksheet.Cell("A" + nextRow).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            worksheet.Cell("B" + nextRow).Value = data.MartixPointArray[0];
                            worksheet.Cell("C" + nextRow).Value = data.MartixPointArray[1];
                            worksheet.Cell("D" + nextRow).Value = data.MartixPointArray[2];
                            worksheet.Cell("E" + nextRow).Value = data.MartixPointArray[3];
                            worksheet.Cell("F" + nextRow).Value = data.MartixPointArray[4];
                            worksheet.Cell("G" + nextRow).Value = data.MartixPointArray[5];
                            worksheet.Cell("H" + nextRow).Value = data.MartixPointArray[6];
                            worksheet.Cell("I" + nextRow).Value = data.MartixPointArray[7];
                            worksheet.Cell("J" + nextRow).Value = data.MartixPointArray[8];
                            worksheet.Cell("K" + nextRow).Value = data.MartixPointArray[9];
                            worksheet.Cell("L" + nextRow).Value = data.MartixPointArray[10];
                            worksheet.Cell("M" + nextRow).Value = data.MartixPointArray[11];
                            worksheet.Cell("N" + nextRow).Value = data.MartixPointArray[12];
                            worksheet.Cell("O" + nextRow).Value = data.MartixPointArray[13];
                            worksheet.Cell("P" + nextRow).Value = data.MartixPointArray[14];
                            worksheet.Cell("Q" + nextRow).Value = data.MartixPointArray[15];
                            worksheet.Cell("R" + nextRow).Value = data.MartixPointArray[16];
                            worksheet.Cell("S" + nextRow).Value = data.MartixPointArray[17];
                        }
                        if (data.IsRight)
                        {
                            worksheet.Cell("A" + nextRow).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            worksheet.Cell("T" + nextRow).Value = data.MartixPointArray[0];
                            worksheet.Cell("U" + nextRow).Value = data.MartixPointArray[1];
                            worksheet.Cell("V" + nextRow).Value = data.MartixPointArray[2];
                            worksheet.Cell("W" + nextRow).Value = data.MartixPointArray[3];
                            worksheet.Cell("X" + nextRow).Value = data.MartixPointArray[4];
                            worksheet.Cell("Y" + nextRow).Value = data.MartixPointArray[5];
                            worksheet.Cell("Z" + nextRow).Value = data.MartixPointArray[6];
                            worksheet.Cell("AA" + nextRow).Value = data.MartixPointArray[7];
                            worksheet.Cell("AB" + nextRow).Value = data.MartixPointArray[8];
                            worksheet.Cell("AC" + nextRow).Value = data.MartixPointArray[9];
                            worksheet.Cell("AD" + nextRow).Value = data.MartixPointArray[10];
                            worksheet.Cell("AE" + nextRow).Value = data.MartixPointArray[11];
                            worksheet.Cell("AF" + nextRow).Value = data.MartixPointArray[12];
                            worksheet.Cell("AG" + nextRow).Value = data.MartixPointArray[13];
                            worksheet.Cell("AH" + nextRow).Value = data.MartixPointArray[14];
                            worksheet.Cell("AI" + nextRow).Value = data.MartixPointArray[15];
                            worksheet.Cell("AJ" + nextRow).Value = data.MartixPointArray[16];
                            worksheet.Cell("AK" + nextRow).Value = data.MartixPointArray[17];
                        }

                    }

                    // 保存工作簿
                    try
                    {
                        workbook.Save();

                    }
                    catch (Exception ex)
                    {
                        // 如果有异常发生，保存操作失败        
                        Console.WriteLine("Failed to save the workbook: " + ex.Message);
                    }
                }
            }
            catch (Exception)
            {
                await _dialogService.ShowAlertAsync("DataSave", "请先关闭已打开的相关文件", "确认", "取消");
                throw;
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
            if (DataExcelPathListSelectedIndex != null && DataItems.Contains(DataExcelPathListSelectedIndex))
            {
                // 找到要删除的数据点在折线图中的索引
                var itemToRemove = DataExcelPathListSelectedIndex as DataExcelPath;
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
                DataExcelPathListSelectedIndex = null;

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
                //DataItems.Clear();
                DataExcelPathListSelectedIndex = null;
                PressureNumeric = 0;
                RTANumeric = 0;
                RTVNumeric = 0;
                numCount = 0;
                if (Series[0] is LineSeries<ObservablePoint> lineSeries)
                {
                    if (lineSeries.Values is ObservableCollection<ObservablePoint> values)
                    {
                        values.Clear();
                    }
                }
                if (Series[1] is LineSeries<ObservablePoint> lineDoubleSeries)
                {
                    if (lineDoubleSeries.Values is ObservableCollection<ObservablePoint> values)
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
                if (ScrollbarSeries[0] is LineSeries<ObservablePoint> scrollbarSeries)
                {
                    if (scrollbarSeries.Values is ObservableCollection<ObservablePoint> values)
                    {
                        values.Clear();
                    }
                }

                if (ScrollbarSeries[1] is LineSeries<ObservablePoint> scrollbarDoubleSeries)
                {
                    if (scrollbarDoubleSeries.Values is ObservableCollection<ObservablePoint> values)
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
                        Name = "Pressure",
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1),
                        ScalesYAt = 0
                    },

                    new LineSeries<ObservablePoint>
                    {
                        Name = "Voltage",
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1),
                        ScalesYAt = 1
                    }
                };

                ScrollbarSeries = new ObservableCollection<ISeries>
                {
                    new LineSeries<ObservablePoint>
                    {
                        Name = "Pressure",
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1)
                    },
                    new LineSeries<ObservablePoint>
                    {
                        Name = "Voltage",
                        Values = new ObservableCollection<ObservablePoint>(),
                        GeometryStroke = null,
                        GeometryFill = null,
                        DataPadding = new(0, 1)
                    }
                };

                YAxes = new ICartesianAxis[]
    {
                    new Axis // the "units" and "tens" series will be scaled on this axis
                    {
                        Name = "Pressure/g",
                        NameTextSize = 14,
                        NamePaint = new SolidColorPaint(s_blue),
                        NamePadding = new LiveChartsCore.Drawing.Padding(0, 20),
                        Padding =  new LiveChartsCore.Drawing.Padding(0, 0, 20, 0),
                        TextSize = 12,
                        LabelsPaint = new SolidColorPaint(s_blue),
                        TicksPaint = new SolidColorPaint(s_blue),
                        SubticksPaint = new SolidColorPaint(s_blue),
                        DrawTicksPath = true
                    },
                    new Axis // the "hundreds" series will be scaled on this axis
                    {
                        Name = "Voltage/V",
                        NameTextSize = 14,
                        MinLimit = 0,
                        MaxLimit = RefVoltage[RefSelectedIndex],
                        NamePaint = new SolidColorPaint(s_red),
                        NamePadding = new LiveChartsCore.Drawing.Padding(0, 5),
                        Padding =  new LiveChartsCore.Drawing.Padding(5, 0, 0, 0),
                        TextSize = 12,
                        LabelsPaint = new SolidColorPaint(s_red),
                        TicksPaint = new SolidColorPaint(s_red),
                        SubticksPaint = new SolidColorPaint(s_red),
                        DrawTicksPath = true,
                        ShowSeparatorLines = false,
                        Position = LiveChartsCore.Measure.AxisPosition.End
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

        private void OnMartixSettingCommand(string parameter)
        {
            switch (parameter)
            {
                case "MartixSettingLeft":
                    if (true == IsLeftFoot)
                    {
                        IsLeftFoot = false;
                        ButtonTextColor_MartixLeft = OnButtonNormalColor;
                        MartixLeftBgColor = OnMartixNormalColor;
                    }
                    else
                    {
                        IsLeftFoot = true;
                        ButtonTextColor_MartixLeft = OnButtonActiveColor;
                        MartixLeftBgColor = OnMartixActiveColor;
                    }
                    
                    break;
                case "MartixSettingRight":
                    if (true == IsRightFoot)
                    {
                        IsRightFoot = false;
                        ButtonTextColor_MartixRight = OnButtonNormalColor;
                        MartixRightBgColor = OnMartixNormalColor;
                    }
                    else
                    {
                        IsRightFoot = true;
                        ButtonTextColor_MartixRight = OnButtonActiveColor;
                        MartixRightBgColor= OnMartixActiveColor;
                    }
                    break;
            }
        }


       

    }


}

