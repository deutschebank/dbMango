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
﻿using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson.Serialization.Attributes;
using Rms.Risk.Mango.Pivot.Core.Models;

namespace Rms.Risk.Mango.Pivot.UI.Pivot;

public class ExtraFilterDefinition
{
    public const string ControlTypeDropDown        = "DropDown";
    public const string ControlTypeDatePicker      = "DatePicker";
    public const string CurrentCollectionSignature = "[COLLECTION]";
    public const string Any                        = "<Any>";

    [SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    public class FilterControl
    {
        public string ControlType         { get; set; } = string.Empty;
        public bool   AllowMultiselect    { get; set; }
        public string DisplayName         { get; set; } = string.Empty;
        public string FieldName           { get; set; } = string.Empty;
        public string? DefaultValue       { get; set; }
        public string Format              { get; set; } = string.Empty;
        [BsonIgnoreIfNull]
        public string? SelectorCollection { get; set; } = string.Empty;
        [BsonIgnoreIfNull]
        public string? SelectorQuery      { get; set; } = string.Empty;
        [BsonIgnoreIfNull]
        public List<string> Values        { get; set; } = [];
    }

    public class FilterValue
    {
        public bool             MultipleSelected { get; set; }
        public HashSet<string>? Value            { get; set; }
        public string?          RangeStart       { get; set; }
        public string?          RangeEnd         { get; set; }
    }


    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public List<FilterControl> Filter { get; set; } = new ();

    public List<FilterValue> ParseQueryParameters(Dictionary<string, string> queryParameters)
    {
        List<FilterValue> values = new(new FilterValue[Filter.Count]);
        foreach (var filterDef in Filter.Select((x, i) => (Index: i, Filter: x)))
        {
            if (!queryParameters.TryGetValue(filterDef.Filter.FieldName, out var value) 
                || (filterDef.Filter.ControlType != ControlTypeDatePicker && filterDef.Filter.ControlType != ControlTypeDropDown)
                )
            {
                values[filterDef.Index] = new()
                {
                    MultipleSelected = false,
                };
                continue;
            }

            if (filterDef.Filter.ControlType == ControlTypeDatePicker)
            {
                var vv = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (vv.Length > 1)
                {
                    values[filterDef.Index] = new()
                    {
                        MultipleSelected = true,
                        RangeStart       = vv[0],
                        RangeEnd         = vv[1]
                    };
                }
                else
                {
                    values[filterDef.Index] = new()
                    {
                        MultipleSelected = false,
                        RangeStart       = vv[0],
                        RangeEnd         = vv[0]
                    };
                }
            }
            else if (filterDef.Filter.ControlType == ControlTypeDropDown)
            {
                var vv = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                values[filterDef.Index] = new()
                {
                    MultipleSelected = vv.Length > 1,
                    Value            = [..vv]
                };
            }
        }

        return values;
    }

    public Dictionary<string, string> CreateQueryParameters(List<FilterValue> values)
    {
        var queryParameters = new Dictionary<string, string>();

        foreach (var filterDef in Filter.Select((x, i) => (Index: i, Filter: x)))
        {
            var filterValue = values[filterDef.Index];
            if (filterDef.Filter.ControlType == ControlTypeDatePicker)
            {
                if (!string.IsNullOrWhiteSpace(filterValue.RangeStart) && !string.IsNullOrWhiteSpace(filterValue.RangeEnd))
                {
                    queryParameters[filterDef.Filter.FieldName] = filterValue.RangeStart != filterValue.RangeEnd && !string.IsNullOrWhiteSpace(filterValue.RangeEnd) && !string.IsNullOrWhiteSpace(filterValue.RangeStart)
                        ? $"{filterValue.RangeStart},{filterValue.RangeEnd}"
                        : filterValue.RangeStart ?? filterValue.RangeEnd;
                }
            }
            else if (filterDef.Filter.ControlType == ControlTypeDropDown)
            {
                if (filterValue.Value != null && filterValue.Value.Any() && !(filterValue.Value.Count == 1 && filterValue.Value.Contains(Any)))
                {
                    queryParameters[filterDef.Filter.FieldName] = string.Join(',', filterValue.Value);
                }
            } 
        }

        return queryParameters;
    }


    public List<FilterValue> ParseExtraFilter(FilterExpressionTree.ExpressionGroup extraFilter)
    {
        // Clear the current _values list
        List<FilterValue> values = new(new FilterValue[Filter.Count]);

        if (!extraFilter.Children.Any())
        {
            for( var i = 0; i < Filter.Count; i++ )
            {
                var fd = Filter[i];
                values[i] = new ()
                {
                    MultipleSelected = false,
                    Value = fd.ControlType == ControlTypeDropDown && !string.IsNullOrWhiteSpace(fd.DefaultValue)
                        ? [fd.DefaultValue]
                        : fd.AllowMultiselect
                            ? [ Any ]
                            : [],
                    RangeStart = fd.ControlType == ControlTypeDatePicker ? fd.DefaultValue : null,
                    RangeEnd   = null
                };
            }

            return values;
        }

        foreach (var filterDef in Filter.Select((x, i) => (Index: i, Filter: x)))
        {
            var filterValue = new FilterValue();
            var matchingGroup = extraFilter.Children
                .OfType<FilterExpressionTree.ExpressionGroup>()
                .FirstOrDefault(group => group.Children.Any(child =>
                    child is FilterExpressionTree.FieldExpression fieldExpr &&
                    fieldExpr.Field == filterDef.Filter.FieldName));

            if (filterDef.Filter.ControlType == ControlTypeDatePicker)
            {
                if (matchingGroup != null)
                {
                    filterValue.MultipleSelected = matchingGroup.Children.Count > 1;

                    if (filterValue.MultipleSelected)
                    {
                        foreach (var child in matchingGroup.Children.OfType<FilterExpressionTree.FieldExpression>())
                        {
                            switch (child.Condition)
                            {
                                case FilterExpressionTree.FieldConditionType.GreaterThanOrEqualTo:
                                    filterValue.RangeStart = child.Argument;
                                    break;
                                case FilterExpressionTree.FieldConditionType.LessThanOrEqualTo:
                                    filterValue.RangeEnd = child.Argument;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        var startExpr = matchingGroup.Children
                            .OfType<FilterExpressionTree.FieldExpression>()
                            .FirstOrDefault(child => child.Condition == FilterExpressionTree.FieldConditionType.EqualTo);
                        filterValue.RangeStart = startExpr?.Argument;
                        filterValue.RangeEnd   = startExpr?.Argument;
                    }
                }
            }
            else if (filterDef.Filter.ControlType == ControlTypeDropDown)
            {
                if (matchingGroup != null)
                {
                    filterValue.MultipleSelected = matchingGroup is { Condition: FilterExpressionTree.ExpressionGroup.ConditionType.Or, Children.Count: > 1 };

                    var selectedValues = matchingGroup.Children
                        .OfType<FilterExpressionTree.FieldExpression>()
                        .Select(child => child.Argument)
                        .ToHashSet();

                    filterValue.Value = selectedValues.Any() ? selectedValues : [Any];
                }
            }

            values[filterDef.Index] = filterValue;
        }
        return values;
    }

    public FilterExpressionTree.ExpressionGroup CreateExtraFilter(List<FilterValue> values)
    {
        // Convert _values into ExtraFilter using FilterDef as a guide.
        // If filter type is ControlTypeDatePicker it should use RangeStart and RangeEnd (if MultipleSelected).
        // If filter type is ControlTypeDropDown it should use Value and MultipleSelected.
        // ExtraFilter starts with an AND group. If multiple filters are set, they should be added to this group.
        // If individual filter has MultipleSelected set to true, it should create an OR group for the values of that filter.

        // Initialize the root AND group for the ExtraFilter
        var rootGroup = new FilterExpressionTree.ExpressionGroup
        {
            Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And
        };

        for (var i = 0; i < Filter.Count; i++)
        {
            var filterDef = Filter[i];
            var filterValue = values[i];

            if (filterDef.ControlType == ControlTypeDatePicker)
            {
                // Handle DatePicker filter
                var start = string.IsNullOrWhiteSpace(filterValue.RangeStart) ? filterValue.RangeEnd : filterValue.RangeStart;
                var end   = string.IsNullOrWhiteSpace(filterValue.RangeEnd)   ? filterValue.RangeStart : filterValue.RangeEnd;

                if (start == null || end == null) 
                    continue;

                var dateGroup = new FilterExpressionTree.ExpressionGroup
                {
                    Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And
                };

                if ( start == end )
                {
                    dateGroup.Children.Add(new FilterExpressionTree.FieldExpression
                    {
                        Field     = filterDef.FieldName,
                        Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                        Argument  = start
                    });
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(start))
                    {
                        dateGroup.Children.Add(new FilterExpressionTree.FieldExpression
                        {
                            Field     = filterDef.FieldName,
                            Condition = FilterExpressionTree.FieldConditionType.GreaterThanOrEqualTo,
                            Argument  = start
                        });
                    }
                    if (!string.IsNullOrWhiteSpace(end))
                    {
                        dateGroup.Children.Add(new FilterExpressionTree.FieldExpression
                        {
                            Field     = filterDef.FieldName,
                            Condition = FilterExpressionTree.FieldConditionType.LessThanOrEqualTo,
                            Argument  = end
                        });
                    }
                }

                if ( dateGroup.Children.Any() )
                    rootGroup.Children.Add(dateGroup);
            }
            else if (filterDef.ControlType == ControlTypeDropDown)
            {
                // Handle DropDown filter
                if (filterValue.Value != null && filterValue.Value.Any())
                {
                    if (filterValue.Value?.Count > 1)
                    {
                        var orGroup = new FilterExpressionTree.ExpressionGroup
                        {
                            Condition = FilterExpressionTree.ExpressionGroup.ConditionType.Or
                        };

                        foreach (var value in filterValue.Value)
                        {
                            orGroup.Children.Add(new FilterExpressionTree.FieldExpression
                            {
                                Field     = filterDef.FieldName,
                                Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                                Argument  = value
                            });
                        }

                        rootGroup.Children.Add(orGroup);
                    }
                    else if (filterValue.Value?.Count == 1 && filterValue.Value.First() != Any)
                    {
                        var andGroup = new FilterExpressionTree.ExpressionGroup
                        {
                            Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And,
                            Children =
                            [
                                new FilterExpressionTree.FieldExpression
                                {
                                    Field     = filterDef.FieldName,
                                    Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                                    Argument  = filterValue.Value.First()
                                }
                            ]
                        };

                        rootGroup.Children.Add(andGroup);
                    }
                }
            }
        }

        return rootGroup;
    }

}
