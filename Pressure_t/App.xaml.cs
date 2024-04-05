using Pressure_t.Model;

namespace Pressure_t;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

        // 创建依赖注入容器
        var services = new ServiceCollection();

        // 注册弹窗服务
        services.AddSingleton<IDialogService, DialogService>();

        // 注册 ViewModel
        services.AddTransient<DataStorageListModel>();

        // 创建服务提供者
        var serviceProvider = services.BuildServiceProvider();

        // 设置主页面
        MainPage = new NavigationPage(new MainPage(serviceProvider.GetService<DataStorageListModel>()));
	}


    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);

        const int newWidth = 1130;
        const int newHeight = 680;

        window.Width = newWidth;
        window.Height = newHeight;
        // Get display size
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

        // Center the window
        window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
        window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;
        window.Title = "Kingsense RTPressure Evaluation Software";
        
        return window;
    }
}

