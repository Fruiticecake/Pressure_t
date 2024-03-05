using System.ComponentModel;
using Pressure_t.Model;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace Pressure_t;
public class CombinedViewModel : INotifyPropertyChanged
{
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

    public MainPage(DataStorageListModel DataStorageList)
    {
        InitializeComponent();
        BindingContext = DataStorageList;

    }

    public class ChartDrawable : IDrawable
    {


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
            DisplayAlert("选择", $"选择了: {selectedItem}", "");
        }
    }



}


