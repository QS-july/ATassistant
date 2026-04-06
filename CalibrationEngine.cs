using System;
using System.Collections.Generic;
using System.Linq;

namespace HLL_ATassistant
{
    public class CalibrationEngine
    {
        public double Alpha { get; private set; }
        public double V2G { get; private set; }
        public double Velocity => V2G > 0 && G > 0 ? Math.Sqrt(V2G * G) : 0;
        public double G { get; set; } = 9.8;
        public double MaxDeltaLimit { get; private set; }
        public List<CalibrationPoint> Points { get; } = new List<CalibrationPoint>();

        public event Action CalibrationChanged;

        public void FitMultiPoints(bool useFixedMaxRange, double fixedMaxRange)
        {
            var validPoints = Points.Where(p => p.Delta > 0).ToList();
            if (validPoints.Count < 2 && !useFixedMaxRange)
                throw new InvalidOperationException("至少需要两个有效标定点");

            double maxDelta = validPoints.Count > 0 ? validPoints.Max(p => p.Delta) : 1;
            double alphaMax = Math.PI / (2.0 * maxDelta) * 0.99;
            int steps = 2000;
            double bestAlpha = 0, bestV2G = 0, bestError = double.MaxValue;

            for (int i = 1; i <= steps; i++)
            {
                double alpha = alphaMax * i / steps;
                bool valid = true;
                double error = 0;

                if (useFixedMaxRange)
                {
                    if (validPoints.Count == 0) throw new InvalidOperationException("使用最大射程时仍需至少一个标定点");
                    foreach (var p in validPoints)
                    {
                        double arg = 2.0 * alpha * p.Delta;
                        if (arg <= 0 || arg >= Math.PI / 2.0) { valid = false; break; }
                        double pred = fixedMaxRange * Math.Sin(arg);
                        error += (pred - p.Distance) * (pred - p.Distance);
                    }
                    if (!valid) continue;
                    if (error < bestError)
                    {
                        bestError = error;
                        bestAlpha = alpha;
                        bestV2G = fixedMaxRange;
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

            if (bestAlpha == 0) throw new InvalidOperationException("未能找到合适的参数");

            Alpha = bestAlpha;
            V2G = bestV2G;
            MaxDeltaLimit = useFixedMaxRange ? Math.Round(Math.PI / (4 * Alpha), 1) : validPoints.Max(p => p.Delta);
            CalibrationChanged?.Invoke();
        }

        public void RecalculateWithFixedRange(double fixedMaxRange)
        {
            if (Points.Count == 0) throw new InvalidOperationException("没有标定点");
            double maxDelta = Points.Max(p => p.Delta);
            double alphaMax = Math.PI / (2.0 * maxDelta) * 0.99;
            int steps = 2000;
            double bestAlpha = 0, bestError = double.MaxValue;

            for (int i = 1; i <= steps; i++)
            {
                double alpha = alphaMax * i / steps;
                bool valid = true;
                double error = 0;
                foreach (var p in Points)
                {
                    double arg = 2.0 * alpha * p.Delta;
                    if (arg <= 0 || arg >= Math.PI / 2.0) { valid = false; break; }
                    double pred = fixedMaxRange * Math.Sin(arg);
                    error += (pred - p.Distance) * (pred - p.Distance);
                }
                if (!valid) continue;
                if (error < bestError)
                {
                    bestError = error;
                    bestAlpha = alpha;
                }
            }

            if (bestAlpha == 0) throw new InvalidOperationException("拟合失败");
            Alpha = bestAlpha;
            V2G = fixedMaxRange;
            MaxDeltaLimit = Math.Round(Math.PI / (4 * Alpha), 1);
            CalibrationChanged?.Invoke();
        }

        public void LoadFromData(CalibrationData data)
        {
            Points.Clear();
            Points.AddRange(data.Points.Select(p => new CalibrationPoint { Delta = p.Delta, Distance = p.Distance }));
            Alpha = data.Alpha;
            V2G = data.V2G;
            G = data.G != 0 ? data.G : 9.8;
            MaxDeltaLimit = data.MaxDeltaLimit;
            if (MaxDeltaLimit == 0 && Points.Count > 0) MaxDeltaLimit = Points.Max(p => p.Delta);
            else if (MaxDeltaLimit == 0 && Alpha != 0) MaxDeltaLimit = Math.PI / (4 * Alpha);
            CalibrationChanged?.Invoke();
        }

        public CalibrationData ExportData() => new CalibrationData
        {
            Points = Points.Select(p => new CalibrationPointData { Delta = p.Delta, Distance = p.Distance }).ToList(),
            Alpha = Alpha,
            V2G = V2G,
            V = Velocity,
            G = G,
            MaxDeltaLimit = MaxDeltaLimit
        };

        public void Reset()
        {
            Points.Clear();
            Alpha = 0;
            V2G = 0;
            MaxDeltaLimit = 0;
            CalibrationChanged?.Invoke();
        }
    }

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