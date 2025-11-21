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
﻿using Rms.Risk.Mango.Pivot.Core.Models;

namespace Tests.Rms.Risk.Mango
{
    [TestFixture]
    public class FilterExpressionTreeTests
    {
        [Test]
        public void ParseJson_EmptyString_ReturnsEmptyExpressionGroup()
        {
            // Arrange
            string filterText = "";

            // Act
            var result = FilterExpressionTree.ParseJson(filterText);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsEmpty);
        }

        [Test]
        public void ParseJson_ValidJson_ReturnsExpressionGroup()
        {
            // Arrange
            string filterText = "{ \"$and\": [ { \"Field1\": { \"$eq\": \"Value1\" } }, { \"Field2\": { \"$ne\": \"Value2\" } } ] }";

            // Act
            var result = FilterExpressionTree.ParseJson(filterText);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsEmpty);
            Assert.AreEqual(2, result.Children.Count);
        }

        [Test]
        public void ToJson_ValidExpressionGroup_ReturnsJsonString()
        {
            // Arrange
            var fieldTypes = new Dictionary<string, Type>
            {
                { "Field1", typeof(string) },
                { "Field2", typeof(int) }
            };

            var expressionGroup = new FilterExpressionTree.ExpressionGroup
            {
                Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And
            };

            expressionGroup.Children.Add(new FilterExpressionTree.FieldExpression
            {
                Field = "Field1",
                Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                Argument = "Value1"
            });

            expressionGroup.Children.Add(new FilterExpressionTree.FieldExpression
            {
                Field = "Field2",
                Condition = FilterExpressionTree.FieldConditionType.GreaterThan,
                Argument = "10"
            });

            // Act
            var result = FilterExpressionTree.ToJson(expressionGroup, fieldTypes);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("\"Field1\""));
            Assert.IsTrue(result.Contains("\"$eq\""));
            Assert.IsTrue(result.Contains("\"Value1\""));
        }

        [Test]
        public void ParseJson_DateTimeField_ReturnsExpressionGroup()
        {
            // Arrange
            string filterText = "{ \"COB\": { \"$eq\": \"2023-10-01T00:00:00Z\" } }";

            // Act
            var result = FilterExpressionTree.ParseJson(filterText);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsEmpty);
            Assert.AreEqual(1, result.Children.Count);

            var fieldExpression = result.Children[0] as FilterExpressionTree.FieldExpression;
            Assert.IsNotNull(fieldExpression);
            Assert.AreEqual("COB", fieldExpression!.Field);
            Assert.AreEqual(FilterExpressionTree.FieldConditionType.EqualTo, fieldExpression.Condition);
            Assert.AreEqual("2023-10-01T00:00:00Z", fieldExpression.Argument);
        }

        [Test]
        public void ToSQL_DateTimeField_ISO8601Format_ReturnsSQLString()
        {
            // Arrange
            var fieldTypes = new Dictionary<string, Type>
            {
                { "COB", typeof(DateTime) }
            };

            var expressionGroup = new FilterExpressionTree.ExpressionGroup
            {
                Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And
            };

            expressionGroup.Children.Add(new FilterExpressionTree.FieldExpression
            {
                Field = "COB",
                Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                Argument = "2023-10-01T00:00:00Z"
            });

            // Act
            var result = FilterExpressionTree.ToSQL(expressionGroup, fieldTypes);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("COB = timestamp( '2023-10-01 00:00:00' )"));
        }

        [Test]
        public void ToSQL_DateTimeField_LocalTimeFormat_ReturnsSQLString()
        {
            // Arrange
            var fieldTypes = new Dictionary<string, Type>
            {
                { "COB", typeof(DateTime) }
            };

            var expressionGroup = new FilterExpressionTree.ExpressionGroup
            {
                Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And
            };

            expressionGroup.Children.Add(new FilterExpressionTree.FieldExpression
            {
                Field = "COB",
                Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                Argument = "2023-10-01 08:00:00"
            });

            // Act
            var result = FilterExpressionTree.ToSQL(expressionGroup, fieldTypes);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("COB = timestamp( '2023-10-01 08:00:00' )"));
        }

        [Test]
        public void ToSQL_DateTimeField_CustomFormat_ReturnsSQLString()
        {
            // Arrange
            var fieldTypes = new Dictionary<string, Type>
            {
                { "COB", typeof(DateTime) }
            };

            var expressionGroup = new FilterExpressionTree.ExpressionGroup
            {
                Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And
            };

            expressionGroup.Children.Add(new FilterExpressionTree.FieldExpression
            {
                Field = "COB",
                Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                Argument = "01-Oct-2023 00:00:00"
            });

            // Act
            var result = FilterExpressionTree.ToSQL(expressionGroup, fieldTypes);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("COB = timestamp( '2023-10-01 00:00:00' )"));
        }

        [Test]
        public void ToSQL_DateTimeField_DD_MM_YYYY_ReturnsSQLString()
        {
            // Arrange
            var fieldTypes = new Dictionary<string, Type>
            {
                { "COB", typeof(DateTime) }
            };

            var expressionGroup = new FilterExpressionTree.ExpressionGroup
            {
                Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And
            };

            expressionGroup.Children.Add(new FilterExpressionTree.FieldExpression
            {
                Field = "COB",
                Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                Argument = "31-12-2025" // Date for 31-12-2025
            });

            // Act
            var result = FilterExpressionTree.ToSQL(expressionGroup, fieldTypes);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("COB = timestamp( '2025-12-31 00:00:00' )"));
        }

        [Test]
        public void ToSQL_DateTimeField_YYYY_MM_DD_ReturnsSQLString()
        {
            // Arrange
            var fieldTypes = new Dictionary<string, Type>
            {
                { "COB", typeof(DateTime) }
            };

            var expressionGroup = new FilterExpressionTree.ExpressionGroup
            {
                Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And
            };

            expressionGroup.Children.Add(new FilterExpressionTree.FieldExpression
            {
                Field     = "COB",
                Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                Argument  = "2025-12-31" // Date for 31-12-2025
            });

            // Act
            var result = FilterExpressionTree.ToSQL(expressionGroup, fieldTypes);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("COB = timestamp( '2025-12-31 00:00:00' )"));
        }

        [Test]
        public void ToSQL_DateTimeField_LocalTime_ReturnsSQLString()
        {
            // Arrange
            var fieldTypes = new Dictionary<string, Type>
            {
                { "COB", typeof(DateTime) }
            };

            var expressionGroup = new FilterExpressionTree.ExpressionGroup
            {
                Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And
            };

            expressionGroup.Children.Add(new FilterExpressionTree.FieldExpression
            {
                Field = "COB",
                Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                Argument = "2023-10-01T08:00:00"
            });

            // Act
            var result = FilterExpressionTree.ToSQL(expressionGroup, fieldTypes);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("COB = timestamp( '2023-10-01 08:00:00' )"));
        }

        [Test]
        public void FieldExpression_ToString_ReturnsReadableString()
        {
            // Arrange
            var fieldExpression = new FilterExpressionTree.FieldExpression
            {
                Field = "Field1",
                Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                Argument = "Value1"
            };

            // Act
            var result = fieldExpression.ToString();

            // Assert
            Assert.AreEqual("Field1 == Value1", result);
        }

        [Test]
        public void ExpressionGroup_ToString_ReturnsReadableString()
        {
            // Arrange
            var expressionGroup = new FilterExpressionTree.ExpressionGroup
            {
                Condition = FilterExpressionTree.ExpressionGroup.ConditionType.And
            };

            expressionGroup.Children.Add(new FilterExpressionTree.FieldExpression
            {
                Field = "Field1",
                Condition = FilterExpressionTree.FieldConditionType.EqualTo,
                Argument = "Value1"
            });

            expressionGroup.Children.Add(new FilterExpressionTree.FieldExpression
            {
                Field = "Field2",
                Condition = FilterExpressionTree.FieldConditionType.NotEqualTo,
                Argument = "Value2"
            });

            // Act
            var result = expressionGroup.ToString();

            // Assert
            Assert.AreEqual("( Field1 == Value1 AND Field2 != Value2 )", result);
        }
    }
}