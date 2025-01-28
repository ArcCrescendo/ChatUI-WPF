using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace WpfApp1.Controls
{
    public partial class AudioWaveform : UserControl
    {
        private const int POINT_COUNT = 100; // 增加点的数量使曲线更平滑
        private const double MAX_HEIGHT = 40;
        private const double MIN_HEIGHT = 1;
        private const double DEFAULT_HEIGHT = 35; // 默认高度（静音时）
        private PointCollection _points;

        public AudioWaveform()
        {
            InitializeComponent();
            InitializePoints();
            
            // 当控件大小改变时更新点的位置
            SizeChanged += (s, e) => UpdatePointsPosition();
        }

        private void InitializePoints()
        {
            _points = new PointCollection();
            for (int i = 0; i < POINT_COUNT; i++)
            {
                _points.Add(new Point(0, DEFAULT_HEIGHT));
            }
            WaveformLine.Points = _points;
            UpdatePointsPosition();
        }

        private void UpdatePointsPosition()
        {
            double width = ActualWidth;
            if (width <= 0) return;
            
            double step = width / (POINT_COUNT - 1);
            for (int i = 0; i < POINT_COUNT; i++)
            {
                var point = _points[i];
                _points[i] = new Point(i * step, point.Y);
            }
        }

        public void UpdateWaveform(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                int step = buffer.Length / POINT_COUNT;
                for (int i = 0; i < POINT_COUNT; i++)
                {
                    int start = i * step;
                    int sum = 0;
                    for (int j = 0; j < step && start + j < buffer.Length; j++)
                    {
                        sum += Math.Abs(buffer[start + j] - 128);
                    }
                    double average = sum / (double)step;
                    double volume = average / 128.0;
                    double height = DEFAULT_HEIGHT - (volume * (DEFAULT_HEIGHT - MIN_HEIGHT));
                    var point = _points[i];
                    _points[i] = new Point(point.X, Math.Max(MIN_HEIGHT, height));
                }
            });
        }

        public void Reset()
        {
            for (int i = 0; i < POINT_COUNT; i++)
            {
                var point = _points[i];
                _points[i] = new Point(point.X, DEFAULT_HEIGHT);
            }
        }
    }
} 