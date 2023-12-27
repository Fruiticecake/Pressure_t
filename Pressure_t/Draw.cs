using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
public class DataPoint
{
    public float X { get; set; }
    public float Y { get; set; }
}
namespace Pressure_t
{
    internal class Draw
    {
        private class LineChartDrawable : IDrawable
        {
            public List<DataPoint> DataPoints { get; set; }

            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                // 计算边界和缩放因子
                float padding = 40;
                float maxX = DataPoints.Max(p => p.X);
                float maxY = DataPoints.Max(p => p.Y);
                float scaleX = (dirtyRect.Width - padding) / maxX;
                float scaleY = (dirtyRect.Height - padding) / maxY;

                // 绘制坐标轴
                canvas.StrokeSize = 2;
                canvas.DrawLine(padding, dirtyRect.Height - padding, dirtyRect.Width, dirtyRect.Height - padding);
                canvas.DrawLine(padding, dirtyRect.Height - padding, padding, 0);

                // 绘制轴标签
                canvas.FontSize = 12;
                for (int i = 0; i <= maxX; i += (int)(maxX / 10))
                {
                    var x = padding + i * scaleX;
                    canvas.DrawString($"{i}", x, dirtyRect.Height - padding + 20, HorizontalAlignment.Center);
                }
                for (int i = 0; i <= maxY; i += (int)(maxY / 10))
                {
                    var y = dirtyRect.Height - padding - i * scaleY;
                    canvas.DrawString($"{i}", padding - 20, y, HorizontalAlignment.Right);
                }

                // 绘制数据点和线条
                if (DataPoints.Count > 1)
                {
                    for (int i = 0; i < DataPoints.Count - 1; i++)
                    {
                        var p1 = DataPoints[i];
                        var p2 = DataPoints[i + 1];
                        canvas.StrokeSize = 2;
                        canvas.StrokeColor = Colors.Blue;
                        canvas.DrawLine(
                            padding + p1.X * scaleX, dirtyRect.Height - padding - p1.Y * scaleY,
                            padding + p2.X * scaleX, dirtyRect.Height - padding - p2.Y * scaleY);
                    }
                }

                // 绘制点
                foreach (var point in DataPoints)
                {
                    canvas.FillColor = Colors.Red;
                    canvas.FillCircle(padding + point.X * scaleX, dirtyRect.Height - padding - point.Y * scaleY, 5);
                }
            }
        }
    }
}
