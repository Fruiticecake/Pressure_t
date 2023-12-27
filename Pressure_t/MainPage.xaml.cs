using System.Collections.ObjectModel;
using System.ComponentModel;
using Microcharts;
using SkiaSharp;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Pressure_t.Model;
namespace Pressure_t;
public class CombinedViewModel : INotifyPropertyChanged
{
    public DataStorageList DataList;
    public PickerCOM PickerCOM;

    // ...其他属性和方法...

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        this.BindingContext = new CombinedViewModel
        {
            PickerCOM = new PickerCOM(),// 设置绑定上下文
            DataList = new DataStorageList()
        };

        PickerCOM.SelectedIndex = 0;
        //chartView.Drawable = new ChartDrawable(chart);
    }

    public class ChartDrawable : IDrawable
    {
        private readonly Chart chart;

        public ChartDrawable(Chart chart)
        {
            this.chart = chart;
        }


        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            //chart.Draw(canvas, dirtyRect.ToSKRect());
        }
    }
    private void OnPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        if (selectedIndex != -1)
        {
            string selectedItem = (string)picker.ItemsSource[selectedIndex];
            // 使用所选项执行操作
            // 例如：显示所选项或根据选择更新UI
            DisplayAlert("选择", $"您选择了: {selectedItem}", "OK");
        }
    }

    public void OnSerialCOM(object sender, EventArgs e)
    {

    }


}


