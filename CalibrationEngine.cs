using System;
using System.Collections.Generic;
using System.Linq;

namespace HLL_ATassistant
{
    /// <summary>
    /// 校准引擎：负责多点拟合、参数计算、数据导入导出。
    /// </summary>
    public class CalibrationEngine
    {
        public double Alpha { get; private set; }
        public double V2G { get; private set; }
        public double Velocity => V2G > 0 && G > 0 ? Math.Sqrt(V2G * G) : 0;
        public double G { get; set; } = 9.8;
        public double MaxDeltaLimit { get; private set; }
        public List<CalibrationPoint> Points { get; } = new List<CalibrationPoint>();

        public event Action? CalibrationChanged;

        // 核心拟合逻辑（私有）
        private (double bestAlpha, double bestV2G) FitInternal(
            List<CalibrationPoint> points,
            bool useFixedV2G,
            double fixedV2G,
            out double bestError)
        {
            var validPoints = points.Where(p => p.Delta > 0).ToList();
            if (validPoints.Count == 0)
                throw new InvalidOperationException("没有有效的标定点（Delta > 0）");

            if (!useFixedV2G && validPoints.Count < 2)
                throw new InvalidOperationException("自动拟合模式下至少需要两个有效标定点");

            double maxDelta = validPoints.Max(p => p.Delta);
            double alphaMax = Math.PI / (2.0 * maxDelta);
            const int steps = 2000;
            double bestAlpha = 0, bestV2G = 0;
            bestError = double.MaxValue;

            for (int i = 1; i <= steps; i++)
            {
                double alpha = alphaMax * i / steps;
                bool valid = true;
                double error = 0;

                if (useFixedV2G)
                {
                    foreach (var p in validPoints)
                    {
                        double arg = 2.0 * alpha * p.Delta;
                        if (arg <= 0 || arg >= Math.PI / 2.0) { valid = false; break; }
                        double pred = fixedV2G * Math.Sin(arg);
                        error += (pred - p.Distance) * (pred - p.Distance);
                    }
                    if (!valid) continue;
                    if (error < bestError)
                    {
                        bestError = error;
                        bestAlpha = alpha;
                        bestV2G = fixedV2G;
                    }
                }
                else
                {
                    double sumDSin = 0, sumSin2 = 0;
                    foreach (var p in validPoints)
                    {
                        double arg = 2.0 * alpha * p.Delta;
                        if (arg <= 0 || arg >= Math.PI / 2.0) { valid = false; break; }
                        double sinVal = Math.Sin(arg);
                        sumDSin += p.Distance * sinVal;
                        sumSin2 += sinVal * sinVal;
                    }
                    if (!valid || sumSin2 == 0) continue;
                    double v2g = sumDSin / sumSin2;
                    foreach (var p in validPoints)
                    {
                        double arg = 2.0 * alpha * p.Delta;
                        double pred = v2g * Math.Sin(arg);
                        error += (pred - p.Distance) * (pred - p.Distance);
                    }
                    if (error < bestError)
                    {
                        bestError = error;
                        bestAlpha = alpha;
                        bestV2G = v2g;
                    }
                }
            }

            if (bestAlpha == 0)
                throw new InvalidOperationException("未能找到合适的拟合参数");

            return (bestAlpha, bestV2G);
        }

        /// <summary>
        /// 多点拟合（使用当前 Points 数据）
        /// </summary>
        public void FitMultiPoints(bool useFixedMaxRange, double fixedMaxRange)
        {
            var (alpha, v2g) = FitInternal(Points, useFixedMaxRange, fixedMaxRange, out _);
            Alpha = alpha;
            V2G = v2g;

            if (useFixedMaxRange)
                MaxDeltaLimit = Math.Round(Math.PI / (4 * Alpha), 1);
            else
                MaxDeltaLimit = Points.Where(p => p.Delta > 0).Max(p => p.Delta);

            CalibrationChanged?.Invoke();
        }

        /// <summary>
        /// 使用固定最大射程重新拟合
        /// </summary>
        public void RecalculateWithFixedRange(double fixedMaxRange)
        {
            if (Points.Count == 0)
                throw new InvalidOperationException("没有标定点");

            var (alpha, _) = FitInternal(Points, true, fixedMaxRange, out _);
            Alpha = alpha;
            V2G = fixedMaxRange;
            MaxDeltaLimit = Math.Round(Math.PI / (4 * Alpha), 1);
            CalibrationChanged?.Invoke();
        }

        /// <summary>
        /// 从校准数据加载（反序列化）
        /// </summary>
        public void LoadFromData(CalibrationData data)
        {
            Points.Clear();
            Points.AddRange(data.Points.Select(p => new CalibrationPoint { Delta = p.Delta, Distance = p.Distance }));
            Alpha = data.Alpha;
            V2G = data.V2G;
            G = data.G != 0 ? data.G : 9.8;
            MaxDeltaLimit = data.MaxDeltaLimit;
            if (MaxDeltaLimit == 0 && Points.Count > 0)
                MaxDeltaLimit = Points.Max(p => p.Delta);
            else if (MaxDeltaLimit == 0 && Alpha != 0)
                MaxDeltaLimit = Math.PI / (4 * Alpha);
            CalibrationChanged?.Invoke();
        }

        /// <summary>
        /// 导出校准数据（用于序列化）
        /// </summary>
        public CalibrationData ExportData() => new CalibrationData
        {
            Points = Points.Select(p => new CalibrationPointData { Delta = p.Delta, Distance = p.Distance }).ToList(),
            Alpha = Alpha,
            V2G = V2G,
            V = Velocity,
            G = G,
            MaxDeltaLimit = MaxDeltaLimit
        };

        /// <summary>
        /// 重置所有数据
        /// </summary>
        public void Reset()
        {
            Points.Clear();
            Alpha = 0;
            V2G = 0;
            MaxDeltaLimit = 0;
            CalibrationChanged?.Invoke();
        }
    }

    // 辅助数据类
    public class CalibrationPoint
    {
        public double Distance { get; set; }
        public double Delta { get; set; }
    }

    [Serializable]
    public class CalibrationPointData
    {
        public double Delta { get; set; }
        public double Distance { get; set; }
    }

    [Serializable]
    public class CalibrationData
    {
        public List<CalibrationPointData> Points { get; set; } = new List<CalibrationPointData>();
        public double Alpha { get; set; }
        public double V2G { get; set; }
        public double V { get; set; }
        public double G { get; set; }
        public double MaxDeltaLimit { get; set; }
    }
}
