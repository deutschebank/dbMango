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
using NUnit.Framework.Legacy;
using Rms.Risk.Mango.Pivot.Core.Models;
using Rms.Risk.Mango.Pivot.UI.Pivot;

namespace Tests.Rms.Risk.Mango;

[TestFixture]
public class ExtraFilterDefinitionTests
{
    private ExtraFilterDefinition _extraFilterDefinition = null!;

    [SetUp]
    public void SetUp()
    {
        _extraFilterDefinition = new()
        {
            Filter =
            [
                new()
                {
                    ControlType      = ExtraFilterDefinition.ControlTypeDropDown,
                    AllowMultiselect = true,
                    DisplayName      = "Test DropDown",
                    FieldName        = "DropDownField",
                    DefaultValue     = "DefaultValue"
                },

                new()
                {
                    ControlType      = ExtraFilterDefinition.ControlTypeDatePicker,
                    AllowMultiselect = false,
                    DisplayName      = "Test DatePicker",
                    FieldName        = "DatePickerField",
                    DefaultValue     = "2023-01-01"
                }
            ]
        };
    }

    [Test]
    public void ParseExtraFilter_ShouldReturnDefaultValues_WhenNoChildrenInExtraFilter()
    {
        // Arrange
        var extraFilter = new FilterExpressionTree.ExpressionGroup();

        // Act
        var result = _extraFilterDefinition.ParseExtraFilter(extraFilter);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(new HashSet<string> { "DefaultValue" }, result[0].Value);
        Assert.AreEqual("2023-01-01", result[1].RangeStart);
    }

    [Test]
    public void ParseExtraFilter_ShouldReturnNoValues_WhenNoDefaultsAndNoChildrenInExtraFilter()
    {
        // Arrange
        var extraFilter = new FilterExpressionTree.ExpressionGroup();
        var extraFilterDefinition = new ExtraFilterDefinition
        {
            Filter =
            [
                new()
                {
                    ControlType      = ExtraFilterDefinition.ControlTypeDropDown,
                    AllowMultiselect = false,
                    DisplayName      = "Test DropDown",
                    FieldName        = "DropDownField",
                },

                new()
                {
                    ControlType      = ExtraFilterDefinition.ControlTypeDatePicker,
                    AllowMultiselect = false,
                    DisplayName      = "Test DatePicker",
                    FieldName        = "DatePickerField",
                }
            ]
        };
        // Act
        var result = extraFilterDefinition.ParseExtraFilter(extraFilter);

        // Assert
        Assert.AreEqual(2,                     result.Count);
        Assert.AreEqual(new HashSet<string>(), result[0].Value);
        Assert.AreEqual(null,                  result[1].RangeStart);
    }


    [Test]
    public void ParseExtraFilter_ShouldReturnAny_ForMultiselectAndNoValueSelected()
    {
        // Arrange
        var extraFilter = new FilterExpressionTree.ExpressionGroup();
        var extraFilterDefinition = new ExtraFilterDefinition()
        {
            Filter =
            [
                new()
                {
                    ControlType      = ExtraFilterDefinition.ControlTypeDropDown,
                    AllowMultiselect = true,
                    DisplayName      = "Test DropDown",
                    FieldName        = "DropDownField"
                }
            ]
        };

        // Act
        var result = extraFilterDefinition.ParseExtraFilter(extraFilter);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new HashSet<string> { ExtraFilterDefinition.Any }, result[0].Value);
    }

    [Test]
    public void ParseExtraFilter_ShouldParseDropDownValuesCorrectly()
    {
        // Arrange
        var extraFilter = new FilterExpressionTree.ExpressionGroup
        {
            Children =
            [
                new FilterExpressionTree.ExpressionGroup
                {
                    Condition = FilterExpressionTree.ExpressionGroup.ConditionType.Or,
                    Children =
                    [
                        new FilterExpressionTree.FieldExpression
                        {
                            Field     = "DropDownField",
                            Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                            Argument  = "Value1"
                        },

                        new FilterExpressionTree.FieldExpression
                        {
                            Field     = "DropDownField",
                            Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                            Argument  = "Value2"
                        }
                    ]
                }
            ]
        };

        // Act
        var result = _extraFilterDefinition.ParseExtraFilter(extraFilter);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result[0].MultipleSelected);
        CollectionAssert.AreEquivalent(new[] { "Value1", "Value2" }, result[0].Value!);
    }

    [Test]
    public void ParseExtraFilter_ShouldParseMultiSelectDatePickerValuesCorrectly()
    {
        // Arrange
        var extraFilter = new FilterExpressionTree.ExpressionGroup
        {
            Children =
            [
                new FilterExpressionTree.ExpressionGroup
                {
                    Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And,
                    Children =
                    [
                        new FilterExpressionTree.FieldExpression
                        {
                            Field     = "DatePickerField",
                            Condition = FilterExpressionTree.FieldConditionType.GreaterThanOrEqualTo,
                            Argument  = "2023-01-01"
                        },

                        new FilterExpressionTree.FieldExpression
                        {
                            Field     = "DatePickerField",
                            Condition = FilterExpressionTree.FieldConditionType.LessThanOrEqualTo,
                            Argument  = "2023-12-31"
                        }
                    ]
                }
            ]
        };

        // Act
        var result = _extraFilterDefinition.ParseExtraFilter(extraFilter);

        // Assert
        Assert.AreEqual(2,            result.Count);
        Assert.IsTrue(result[1].MultipleSelected);
        Assert.AreEqual("2023-01-01", result[1].RangeStart);
        Assert.AreEqual("2023-12-31", result[1].RangeEnd);
    }

    [Test]
    public void ParseExtraFilter_ShouldParseSingleSelectDatePickerValuesCorrectly()
    {
        // Arrange
        var extraFilter = new FilterExpressionTree.ExpressionGroup
        {
            Children =
            [
                new FilterExpressionTree.ExpressionGroup
                {
                    Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And,
                    Children =
                    [
                        new FilterExpressionTree.FieldExpression
                        {
                            Field     = "DatePickerField",
                            Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                            Argument  = "2023-01-01"
                        }
                    ]
                }
            ]
        };

        // Act
        var result = _extraFilterDefinition.ParseExtraFilter(extraFilter);

        // Assert
        Assert.AreEqual(2,            result.Count);
        Assert.IsFalse(result[1].MultipleSelected);
        Assert.AreEqual("2023-01-01", result[1].RangeStart);
        Assert.AreEqual("2023-01-01", result[1].RangeEnd);
    }

    [Test]
    public void CreateExtraFilter_ShouldGenerateCorrectExpressionGroup_MultiSelect()
    {
        // Arrange
        var values = new List<ExtraFilterDefinition.FilterValue>
        {
            new()
            {
                MultipleSelected = true,
                Value = ["Value1", "Value2"]
            },
            new()
            {
                MultipleSelected = true,
                RangeStart       = "2023-01-01",
                RangeEnd         = "2023-12-31"
            }
        };

        // Act
        var result = _extraFilterDefinition.CreateExtraFilter(values);

        // Assert
        Assert.AreEqual(FilterExpressionTree.ExpressionGroup.ConditionType.And, result.Condition);
        Assert.AreEqual(2, result.Children.Count);

        var dropDownGroup = result.Children[0] as FilterExpressionTree.ExpressionGroup;
        Assert.IsNotNull(dropDownGroup);
        Assert.AreEqual(FilterExpressionTree.ExpressionGroup.ConditionType.Or, dropDownGroup!.Condition);
        Assert.AreEqual(2, dropDownGroup.Children.Count);
        Assert.AreEqual(FilterExpressionTree.FieldConditionType.EqualTo, ((FilterExpressionTree.FieldExpression)dropDownGroup.Children[0]).Condition);
        Assert.AreEqual(FilterExpressionTree.FieldConditionType.EqualTo, ((FilterExpressionTree.FieldExpression)dropDownGroup.Children[1]).Condition);

        var datePickerGroup = result.Children[1] as FilterExpressionTree.ExpressionGroup;
        Assert.IsNotNull(datePickerGroup);
        Assert.AreEqual(FilterExpressionTree.ExpressionGroup.ConditionType.And, datePickerGroup!.Condition);
        Assert.AreEqual(2, datePickerGroup.Children.Count);
        Assert.AreEqual(FilterExpressionTree.FieldConditionType.GreaterThanOrEqualTo, ((FilterExpressionTree.FieldExpression)datePickerGroup.Children[0]).Condition);
        Assert.AreEqual(FilterExpressionTree.FieldConditionType.LessThanOrEqualTo, ((FilterExpressionTree.FieldExpression)datePickerGroup.Children[1]).Condition);
    }

    [Test]
    public void CreateExtraFilter_ShouldGenerateCorrectExpressionGroup_SingleSelect()
    {
        // Arrange
        var values = new List<ExtraFilterDefinition.FilterValue>
        {
            new()
            {
                MultipleSelected = false,
                Value = ["Value1"]
            },
            new()
            {
                MultipleSelected = false,
                RangeStart       = "2023-01-01",
                RangeEnd         = "2023-01-01"
            }
        };

        // Act
        var result = _extraFilterDefinition.CreateExtraFilter(values);

        // Assert
        Assert.AreEqual(FilterExpressionTree.ExpressionGroup.ConditionType.And, result.Condition);
        Assert.AreEqual(2, result.Children.Count);

        var dropDownGroup = result.Children[0] as FilterExpressionTree.ExpressionGroup;
        Assert.IsNotNull(dropDownGroup);
        Assert.AreEqual(FilterExpressionTree.ExpressionGroup.ConditionType.And, dropDownGroup!.Condition);
        Assert.AreEqual(1, dropDownGroup.Children.Count);
        Assert.AreEqual(FilterExpressionTree.FieldConditionType.EqualTo, ((FilterExpressionTree.FieldExpression)dropDownGroup.Children[0]).Condition);

        var datePickerGroup = result.Children[1] as FilterExpressionTree.ExpressionGroup;
        Assert.IsNotNull(datePickerGroup);
        Assert.AreEqual(FilterExpressionTree.ExpressionGroup.ConditionType.And, datePickerGroup!.Condition);
        Assert.AreEqual(1, datePickerGroup.Children.Count);
        Assert.AreEqual(FilterExpressionTree.FieldConditionType.EqualTo, ((FilterExpressionTree.FieldExpression)datePickerGroup.Children[0]).Condition);
    }

    [Test]
    public void CreateExtraFilter_ShouldGenerateCorrectExpressionGroup_NoValues()
    {
        // Arrange
        var values = new List<ExtraFilterDefinition.FilterValue>
        {
            new()
            {
                MultipleSelected = false
            },
            new()
            {
                MultipleSelected = false
            }
        };

        // Act
        var result = _extraFilterDefinition.CreateExtraFilter(values);

        // Assert
        Assert.AreEqual(FilterExpressionTree.ExpressionGroup.ConditionType.And, result.Condition);
        Assert.AreEqual(0, result.Children.Count);
    }
    [Test]
    public void ParseQueryParameters_ShouldReturnDefaultValues_WhenQueryParametersAreEmpty()
    {
        // Arrange
        var queryParameters = new Dictionary<string, string>();

        // Act
        var result = _extraFilterDefinition.ParseQueryParameters(queryParameters);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsFalse(result[0].MultipleSelected);
        Assert.IsNull(result[0].Value);
        Assert.IsFalse(result[1].MultipleSelected);
        Assert.IsNull(result[1].RangeStart);
        Assert.IsNull(result[1].RangeEnd);
    }

    [Test]
    public void ParseQueryParameters_ShouldParseDropDownValuesCorrectly()
    {
        // Arrange
        var queryParameters = new Dictionary<string, string>
        {
            { "DropDownField", "Value1,Value2" }
        };

        // Act
        var result = _extraFilterDefinition.ParseQueryParameters(queryParameters);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result[0].MultipleSelected);
        CollectionAssert.AreEquivalent(new[] { "Value1", "Value2" }, result[0].Value!);
    }

    [Test]
    public void ParseQueryParameters_ShouldParseDatePickerValuesCorrectly()
    {
        // Arrange
        var queryParameters = new Dictionary<string, string>
        {
            { "DatePickerField", "2023-01-01,2023-12-31" }
        };

        // Act
        var result = _extraFilterDefinition.ParseQueryParameters(queryParameters);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result[1].MultipleSelected);
        Assert.AreEqual("2023-01-01", result[1].RangeStart);
        Assert.AreEqual("2023-12-31", result[1].RangeEnd);
    }

    [Test]
    public void ParseQueryParameters_ShouldHandleSingleDatePickerValue()
    {
        // Arrange
        var queryParameters = new Dictionary<string, string>
        {
            { "DatePickerField", "2023-01-01" }
        };

        // Act
        var result = _extraFilterDefinition.ParseQueryParameters(queryParameters);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsFalse(result[1].MultipleSelected);
        Assert.AreEqual("2023-01-01", result[1].RangeStart);
        Assert.AreEqual("2023-01-01", result[1].RangeEnd);
    }

    [Test]
    public void ParseQueryParameters_ShouldIgnoreUnknownFields()
    {
        // Arrange
        var queryParameters = new Dictionary<string, string>
        {
            { "UnknownField", "SomeValue" }
        };

        // Act
        var result = _extraFilterDefinition.ParseQueryParameters(queryParameters);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsFalse(result[0].MultipleSelected);
        Assert.IsNull(result[0].Value);
        Assert.IsFalse(result[1].MultipleSelected);
        Assert.IsNull(result[1].RangeStart);
        Assert.IsNull(result[1].RangeEnd);
    }
    [Test]
    public void CreateQueryParameters_ShouldReturnEmptyDictionary_WhenValuesAreEmpty()
    {
        // Arrange
        var values = new List<ExtraFilterDefinition.FilterValue>
        {
            new()
            {
                MultipleSelected = false
            },
            new()
            {
                MultipleSelected = false
            }
        };

        // Act
        var result = _extraFilterDefinition.CreateQueryParameters(values);

        // Assert
        Assert.IsEmpty(result);
    }

    [Test]
    public void CreateQueryParameters_ShouldGenerateCorrectQueryParameters_ForDropDown()
    {
        // Arrange
        var values = new List<ExtraFilterDefinition.FilterValue>
        {
            new()
            {
                MultipleSelected = true,
                Value = ["Value1", "Value2"]
            },
            new()
            {
                MultipleSelected = false
            }
        };

        // Act
        var result = _extraFilterDefinition.CreateQueryParameters(values);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.ContainsKey("DropDownField"));
        Assert.AreEqual("Value1,Value2", result["DropDownField"]);
    }

    [Test]
    public void CreateQueryParameters_ShouldGenerateCorrectQueryParameters_ForDatePicker()
    {
        // Arrange
        var values = new List<ExtraFilterDefinition.FilterValue>
        {
            new()
            {
                MultipleSelected = false
            },
            new()
            {
                MultipleSelected = true,
                RangeStart = "2023-01-01",
                RangeEnd = "2023-12-31"
            }
        };

        // Act
        var result = _extraFilterDefinition.CreateQueryParameters(values);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.ContainsKey("DatePickerField"));
        Assert.AreEqual("2023-01-01,2023-12-31", result["DatePickerField"]);
    }

    [Test]
    public void CreateQueryParameters_ShouldHandleSingleDatePickerValue()
    {
        // Arrange
        var values = new List<ExtraFilterDefinition.FilterValue>
        {
            new()
            {
                MultipleSelected = false
            },
            new()
            {
                MultipleSelected = false,
                RangeStart = "2023-01-01",
                RangeEnd = "2023-01-01"
            }
        };

        // Act
        var result = _extraFilterDefinition.CreateQueryParameters(values);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.ContainsKey("DatePickerField"));
        Assert.AreEqual("2023-01-01", result["DatePickerField"]);
    }

    [Test]
    public void CreateQueryParameters_ShouldIgnoreEmptyValues()
    {
        // Arrange
        var values = new List<ExtraFilterDefinition.FilterValue>
        {
            new()
            {
                MultipleSelected = false,
                Value = []
            },
            new()
            {
                MultipleSelected = false,
                RangeStart = null,
                RangeEnd = null
            }
        };

        // Act
        var result = _extraFilterDefinition.CreateQueryParameters(values);

        // Assert
        Assert.IsEmpty(result);
    }

}
