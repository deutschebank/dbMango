/* 
 *                                dbMango
 *
 * Copyright 2025 Deutsche Bank AG
 * SPDX-License-Identifier: Apache-2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Collections;
using pax.BlazorChartJs;
using Rms.Risk.Mango.Pivot.Core;
using Rms.Risk.Mango.Pivot.UI.Controls;
using Rms.Risk.Mango.Pivot.UI.Services;

namespace Rms.Risk.Mango.Pivot.UI.Pivot;

/// <summary>
/// Prepare LineChard config for the given Pivot definition and data.
/// </summary>
public class ChartHelperForPivot
{
    public bool IsLineChart(PivotDefinition pivotDef, IPivotedData pivotData)
        => GetLineChartColumns(pivotDef, pivotData, out _, out _, out _);

    public ChartJsConfig ChartConfig { get; } = new()
    {
        Type = ChartType.line,
        Data = new()
        {
            Labels = [],
            Datasets = []
        },
        Options = new()
        {
            Responsive = true,
            Plugins = new()
            {
                Legend = new()
                {
                    Display = false,
                    Position = "right"
                }
            },
            Scales = new()
            {
                Y = new LinearAxis
                {
                    BeginAtZero = false,
                    Title = new()
                    {
                        Display = true,
                        Text = "Amount"
                    }
                }
            }
        }
    };

    public void UpdateLineChart(
        string name,
        IReadOnlyCollection<string> labels,
        IReadOnlyCollection<double> data
    )
    {
        ChartConfig.Data.Labels = labels.ToList();

        var color = Night.RandomColorString();
        var currentDataSet = new LineDataset
        {
            Label = name,
            BackgroundColor = color,
            BorderColor = color,
            PointBackgroundColor = color,
            PointRadius = 3,
            PointBorderWidth = 1,
            Fill = false,
            PointHitRadius = 5,
            Stepped = false,
            Data = data.Select(x => (object)x).ToList()
        };

        ChartConfig.Data.Datasets = [currentDataSet];

        ChartConfig.Options ??= new();
        ChartConfig.Options.Plugins ??= new();
        ChartConfig.Options.Plugins.Legend ??= new();
        ChartConfig.Options.Plugins.Legend.Display = true;
        ChartConfig.Options.Plugins.Legend.Position = "bottom";

        if (data.Count > 0)
        {
            var dataMin = data.Min();
            var dataMax = data.Max();
            SetYAxisRange(dataMin, dataMax);
        }

        ChartConfig.ReinitializeChart();
    }

    public void UpdateLineChart(
        PivotDefinition pivotDef,
        IPivotedData pivotData,
        Func<string, string> getFormat
        )
    {
        if (!GetLineChartColumns(pivotDef, pivotData, out var xCol, out var yCol, out var dataSetColumns))
            return;

        var comparer = new RowComparer(pivotData, xCol, dataSetColumns!);

        // sort row indexes by data set key ( all yCol ), then by X-axis label (xCol)
        var orderedRows = Enumerable
            .Range(0, pivotData.Count)
            .OrderBy(x => x, comparer)
            .ToArray()
            ;

        var labelObjects = orderedRows
            .Select(x => pivotData.Get(xCol, x))
            .Where(x => x != null)
            .Distinct()
            .OrderBy(x => x)
            .ToArray()
            ;
        var labels = labelObjects
            .Select(x => TableControl.ConvertToString(x, getFormat(pivotDef.LineChartXAxis!)))
            .ToList()
            ;

        if (labels.Count == 0) // impossible
            return;

        var labelPos = labelObjects
                .Select((x, i) => new KeyValuePair<object, int>(x!, i))
                .ToDictionary(x => x.Key, x => x.Value)
            ;

        ChartConfig.Data.Labels = labels;
        ChartConfig.Data.Datasets.Clear();

        LineDataset[]? currentDataSet = null;
        var currentKey = new object[dataSetColumns?.Count ?? 0];
        var rowKey = new object?[dataSetColumns?.Count ?? 0];
        var data = new List<object?[]>(Enumerable.Range(0, yCol!.Count).Select(_ => new object?[labelObjects.Length]));

        var dataMin = double.MaxValue;
        var dataMax = double.MinValue;

        foreach (var row in orderedRows)
        {
            for (var i = 0; i < (dataSetColumns?.Count ?? 0); i++)
                rowKey[i] = pivotData.Get(dataSetColumns![i].Item2, row!);

            // start new data set if row label (all yCols) changed
            if (currentDataSet == null || !rowKey.SequenceEqual(currentKey))
            {
                if (currentDataSet != null)
                {
                    for (var dataSetNo = 0; dataSetNo < yCol.Count; dataSetNo += 1)
                    {
                        currentDataSet[dataSetNo].Data = data[dataSetNo]
                            .Select(x => x ?? double.NaN)
                            .ToList();
                        ChartConfig.Data.Datasets.Add(currentDataSet[dataSetNo]);
                    }
                    data = [.. Enumerable.Range(0, yCol.Count).Select(_ => new object?[labelObjects.Length])];
                }

                Array.Copy(rowKey, currentKey, currentKey.Length);

                currentDataSet = new LineDataset[yCol.Count];
                for (var dataSetNo = 0; dataSetNo < yCol.Count; dataSetNo += 1)
                {
                    var color = Night.RandomColorString();
                    currentDataSet[dataSetNo] = new()
                    {
                        Label = (yCol.Count > 1 ? $"{yCol[dataSetNo].Item1} - " : "") + string.Join(" - ", currentKey.Select(x => x.ToString())),
                        BackgroundColor = color,
                        BorderColor = color,
                        PointBackgroundColor = color,
                        PointRadius = 3,
                        PointBorderWidth = 1,
                        Fill = pivotDef.LineChartFill,
                        PointHitRadius = 5,
                        Stepped = pivotDef.LineChartSteppedLine,
                        Data = []
                    };
                }
            }

            // add null if label is missing
            var label = pivotData.Get(xCol, row);
            if (label == null)
                continue;

            var pos = labelPos[label];

            for (var dataSetNo = 0; dataSetNo < yCol.Count; dataSetNo += 1)
            {
                var val = pivotData.Get(yCol[dataSetNo].Item2, row);
                if (val is double d)
                {
                    if (d < dataMin)
                        dataMin = d;
                    if (d > dataMax)
                        dataMax = d;
                }

                data[dataSetNo][pos] = val;
            }
        }

        if (currentDataSet != null)
        {
            for (var dataSetNo = 0; dataSetNo < yCol.Count; dataSetNo += 1)
            {
                currentDataSet[dataSetNo].Data = data[dataSetNo]
                    .Select(x => x ?? double.NaN)
                    .ToList();
                ChartConfig.Data.Datasets.Add(currentDataSet[dataSetNo]);
            }
        }

        ChartConfig.Options ??= new();
        ChartConfig.Options.Plugins ??= new();
        ChartConfig.Options.Plugins.Legend ??= new();
        ChartConfig.Options.Plugins.Legend.Display = pivotDef.LineChartShowLegend;
        ChartConfig.Options.Plugins.Legend.Position = "right";

        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (dataMin == double.MaxValue || dataMax == double.MinValue)
        {
            ChartConfig.ReinitializeChart();
            return;
        }

        SetYAxisRange(dataMin, dataMax);
        // ReSharper restore CompareOfFloatsByEqualityOperator

        ChartConfig.ReinitializeChart();
    }

    private void SetYAxisRange(double dataMin, double dataMax)
    {
        ChartConfig.Options ??= new();
        ChartConfig.Options.Scales ??= new();
        ChartConfig.Options.Scales.Y ??= new LinearAxis();

        if (ChartConfig.Options.Scales.Y is not LinearAxis yAxis)
            return;

        yAxis.Min = dataMin - Math.Abs(dataMin) * 0.01; // -1%
        yAxis.Max = dataMax + Math.Abs(dataMax) * 0.01; // +1%
    }

    private bool GetLineChartColumns(
        PivotDefinition? pivotDef,
        IPivotedData pivotData,
        out int xColumn,
        out List<Tuple<string, int>>? yColumn,
        out List<Tuple<string, int>>? dataSetColumns
        )
    {
        xColumn = -1;
        yColumn = null;
        dataSetColumns = null;

        if ( pivotDef is not { MakeLineChart: true }
            || pivotData.Count == 0
            || pivotData.Headers.Count < 1
            || string.IsNullOrWhiteSpace(pivotDef.LineChartXAxis)
            || (pivotDef.LineChartYAxis?.Count ?? 0) <= 0
            )
        {
            return false;
        }

        var headersDict = pivotData.GetColumnPositions();

        if (!headersDict.TryGetValue(pivotDef.LineChartXAxis, out var xCol))
            return false;

        var yCol = (pivotDef.LineChartYAxis ?? [])
                .Select(x => headersDict.TryGetValue(x, out var col) ? new Tuple<string, int>(x, col) : null)
                .Where(x => x != null)
                .ToList()
            ;

        var dCol = (pivotDef.LineChartDataSetKeys ?? [])
            .Select(x => headersDict.TryGetValue(x, out var col) ? new Tuple<string, int>(x, col) : null)
            .Where(x => x != null)
            .ToList()
            ;

        xColumn = xCol;
        yColumn = yCol!;
        dataSetColumns = dCol!;
        return true;
    }

    /// <summary>
    /// Compare pivot rows by multiple columns
    /// </summary>
    private class RowComparer(IPivotedData pivot, int xCol, List<Tuple<string, int>> dataSetCols) : IComparer<int>
    {
        private readonly IPivotedData _pivot = pivot;
        private readonly int _xCol = xCol;
        private readonly List<Tuple<string, int>> _dataSetCols = dataSetCols ?? [];

        public int Compare(int row1, int row2)
        {
            int c;

            // sort by data set (if any)
            foreach (var (_, col) in _dataSetCols)
            {
                c = Comparer.Default.Compare(_pivot.Get(col, row1), _pivot.Get(col, row2));
                if (c != 0)
                    return c;
            }

            // sort by label
            c = Comparer.Default.Compare(_pivot.Get(_xCol, row1), _pivot.Get(_xCol, row2));
            return c;
        }
    }
}
