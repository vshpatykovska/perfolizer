using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Perfolizer.Collections;
using Perfolizer.Common;
using Perfolizer.Mathematics.Histograms;

namespace Perfolizer.Mathematics.Multimodality
{
    public class LowlandModalityDetector : IModalityDetector
    {
        [NotNull] public static readonly LowlandModalityDetector Instance = new LowlandModalityDetector();

        public ModalityData DetectModes(IReadOnlyList<double> values, [CanBeNull] IReadOnlyList<double> weights = null) =>
            DetectModes(values, weights, EmpiricalDensityHistogramBuilder.Instance);

        public ModalityData DetectModes(IReadOnlyList<double> values, [CanBeNull] IReadOnlyList<double> weights,
            [CanBeNull] IDensityHistogramBuilder densityHistogramBuilder)
        {
            Assertion.NotNullOrEmpty(nameof(values), values);
            Assertion.MoreThan($"{nameof(values)}.Count", values.Count, 0);
            if (values.Max() - values.Min() < 1e-9)
                throw new ArgumentException($"{nameof(values)} should contain at least two different elements", nameof(values));

            densityHistogramBuilder ??= EmpiricalDensityHistogramBuilder.Instance;
            var histogram = densityHistogramBuilder.Build(values, weights);
            var bins = histogram.Bins;
            int binCount = bins.Count;
            double binArea = 1.0 / bins.Count;
            var binHeights = bins.Select(bin => bin.Height).ToList();

            var peaks = new List<int>();
            for (int i = 1; i < binCount - 1; i++)
                if (binHeights[i] > binHeights[i - 1] && binHeights[i] >= binHeights[i + 1])
                    peaks.Add(i);

            RangedMode GlobalMode(double location) => new RangedMode(location, histogram.GlobalLower, histogram.GlobalUpper);

            switch (peaks.Count)
            {
                case 0:
                    return new ModalityData(new[] {GlobalMode(bins[binHeights.WhichMax()].Middle)}, histogram);
                case 1:
                    return new ModalityData(new[] {GlobalMode(bins[peaks.First()].Middle)}, histogram);
                default:
                {
                    var modeLocations = new List<double>();
                    var cutPoints = new List<double>();

                    bool CheckForSplit(int peak1, int peak2)
                    {
                        int left = peak1, right = peak2;
                        double height = Math.Min(binHeights[peak1], binHeights[peak2]);
                        while (left < right && binHeights[left] > height)
                            left++;
                        while (left < right && binHeights[right] > height)
                            right--;

                        double width = bins[right].Upper - bins[left].Lower;
                        double totalArea = width * height;
                        int totalBinCount = right - left + 1;
                        double totalBinArea = totalBinCount * binArea;
                        if (totalBinArea < totalArea - totalBinArea)
                        {
                            modeLocations.Add(bins[peak1].Middle);
                            cutPoints.Add(bins[binHeights.WhichMin(peak1, peak2 - peak1)].Middle);
                            return true;
                        }

                        return false;
                    }

                    var previousPeaks = new List<int> {peaks[0]};
                    for (int i = 1; i < peaks.Count; i++)
                    {
                        int currentPeak = peaks[i];
                        if (binHeights[previousPeaks.Last()] > binHeights[currentPeak])
                        {
                            if (CheckForSplit(previousPeaks.Last(), currentPeak))
                            {
                                previousPeaks.Clear();
                                previousPeaks.Add(currentPeak);
                            }
                        }
                        else
                        {
                            while (previousPeaks.Any() && binHeights[previousPeaks.Last()] < binHeights[currentPeak])
                            {
                                if (CheckForSplit(previousPeaks.Last(), currentPeak))
                                {
                                    previousPeaks.Clear();
                                    break;
                                }

                                previousPeaks.RemoveAt(previousPeaks.Count - 1);
                            }

                            previousPeaks.Add(currentPeak);
                        }
                    }

                    modeLocations.Add(bins[previousPeaks.First()].Middle);

                    var modes = new List<RangedMode>();
                    switch (modeLocations.Count)
                    {
                        case 0:
                            modes.Add(GlobalMode(bins[binHeights.WhichMax()].Middle));
                            break;
                        case 1:
                            modes.Add(GlobalMode(modeLocations.First()));
                            break;
                        default:
                        {
                            modes.Add(new RangedMode(modeLocations.First(), histogram.GlobalLower, cutPoints.First()));
                            for (int i = 1; i < modeLocations.Count - 1; i++)
                                modes.Add(new RangedMode(modeLocations[i], cutPoints[i - 1], cutPoints[i]));
                            modes.Add(new RangedMode(modeLocations.Last(), cutPoints.Last(), histogram.GlobalUpper));
                        }
                            break;
                    }

                    return new ModalityData(modes, histogram);
                }
            }
        }
    }
}