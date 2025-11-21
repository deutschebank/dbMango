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
namespace Tests.Rms.Risk.Mango.Language;

[TestFixture]
public class AstToScriptTests
{
    [Test]
    public void AddFields()
    {
        var ast = new AstAggregation("CollectionName");

        ast.Add(new AstPipeline([
            new AstStageAddFields(
            [
                new AstLetExpression( new AstExpressionVariable("field1"), "FieldName"),
                new AstLetExpression( new AstExpressionVariable("field2")),
            ]
            )
        ]));

        const string expected = 
"""
FROM "CollectionName" PIPELINE {
  ADD
    field1 AS FieldName,
    field2
}

""";
        Assert.That( ast.AsText(), Is.EqualTo(expected));
    }

    [Test]
    public void Project()
    {
        var ast = new AstAggregation("CollectionName");

        ast.Add(new AstPipeline([
            new AstStageProject(
                [],
            [
                new AstLetExpression( new AstExpressionVariable("_id")),
                new AstLetExpression( new AstExpressionVariable("field1"), "FieldName")
            ]
            )
        ]));

        const string expected = 
"""
FROM "CollectionName" PIPELINE {
  PROJECT
    _id,
    field1 AS FieldName
}

""";
        Assert.That( ast.AsText(), Is.EqualTo(expected));
    }

    [Test]
    public void ProjectWithComplexVar()
    {
        var ast = new AstAggregation("CollectionName");

        ast.Add(new AstPipeline([
            new AstStageProject(
                [],
                [
                    new AstLetExpression( new AstExpressionVariable("_id")),
                    new AstLetExpression( new AstExpressionVariable("Very.Complex.Var"), "FieldName")
                ]
            )
        ]));

        const string expected = 
            """
            FROM "CollectionName" PIPELINE {
              PROJECT
                _id,
                $Very.Complex.Var AS FieldName
            }

            """;
        Assert.That( ast.AsText(), Is.EqualTo(expected));
    }

    [Test]
    public void ProjectExclude()
    {
        var ast = new AstAggregation("CollectionName");

        ast.Add(new AstPipeline([
            new AstStageProject(
                [],
            [
                new AstLetExpression( new AstExpressionVariable("field1"))
            ]
            , true
            )
        ]));

        const string expected = 
"""
FROM "CollectionName" PIPELINE {
  PROJECT EXCLUDE
    field1
}

""";
        Assert.That( ast.AsText(), Is.EqualTo(expected));
    }

    [Test]
    public void Expression()
    {
        var ast = new AstAggregation("CollectionName");

        var expr = new AstExpressionOperation(
            AstExpressionOperation.OperationType.PLUS,
            new AstExpressionFunctionCall("str",
            [
                new(null, new AstExpressionOperation(
                    AstExpressionOperation.OperationType.MINUS,
                    new AstExpressionOperation(
                        AstExpressionOperation.OperationType.PLUS,
                        new AstExpressionOperation(
                            AstExpressionOperation.OperationType.DIVIDE,
                            new AstExpressionVariable("field1"),
                            new AstExpressionNumber(1.234)
                        ),
                        new AstExpressionNumber(5)

                    ),
                    new AstExpressionFunctionCall("abs",
                    [
                        new (null, new AstExpressionNumber(-4))
                    ]
                    )
                ))
            ]
            ),
            new AstExpressionString(" miles away")
        );

        ast.Add(new AstPipeline([
            new AstStageAddFields(
            [
                new AstLetExpression(expr, "Moon")
            ]
            )
        ]));

        const string expected = 
"""
FROM "CollectionName" PIPELINE {
  ADD
    str( field1 / 1.234 + 5 - abs( -4 ) ) + " miles away" AS Moon
}

""";
        Assert.That( ast.AsText(), Is.EqualTo(expected));
    }

    [Test]
    public void FuncWithNamedArgs()
    {
        var ast = new AstAggregation("CollectionName");

        var expr = new AstExpressionFunctionCall(
            "dateToString",
            [
              new("format", new AstExpressionString("%Y-%m-%d")),
              new("date", new AstExpressionVariable("field1"))
            ]
        );

        ast.Add(new AstPipeline([
            new AstStageAddFields(
            [
                new AstLetExpression(expr, "Moon")
            ]
            )
        ]));

        const string expected = 
"""
FROM "CollectionName" PIPELINE {
  ADD
    dateToString( 
      format: "%Y-%m-%d", 
      date: field1
    ) AS Moon
}

""";
        Assert.That( ast.AsText(), Is.EqualTo(expected));
    }

    [Test]
    public void BoolExpression()
    {
        var ast = new AstAggregation("CollectionName");

        var expr = new AstExpressionOperation(
            AstExpressionOperation.OperationType.AND,
            new AstExpressionOperation(
                AstExpressionOperation.OperationType.NEQ,
                new AstExpressionVariable("field1"),
                new AstExpressionString("Mars")
            ),
            new AstExpressionOperation(
                AstExpressionOperation.OperationType.EQ,
                new AstExpressionVariable("field1"),
                new AstExpressionString("Moon")
            )
        );

        ast.Add(new AstPipeline([
            new AstStageAddFields(
            [
                new AstLetExpression(expr, "IsMoon")
            ]
            )
        ]));

        const string expected = 
"""
FROM "CollectionName" PIPELINE {
  ADD
    field1 != "Mars"
    AND field1 == "Moon" AS IsMoon
}

""";
        Assert.That( ast.AsText(), Is.EqualTo(expected));
    }

    [Test]
    public void WhereTest()
    {
        var ast = new AstAggregation("CollectionName");
        
        var expr = new AstExpressionOperation(
            AstExpressionOperation.OperationType.AND,
            new AstExpressionOperation(
                AstExpressionOperation.OperationType.NEQ,
                new AstExpressionVariable("field1"),
                new AstExpressionString("Mars")
            ),
            new AstExpressionOperation(
                AstExpressionOperation.OperationType.EQ,
                new AstExpressionVariable("field1"),
                new AstExpressionString("Moon")
            )
        );


        var whereStage = new AstStageWhere();
        whereStage.Add(expr);

        ast.Add(new AstPipeline([
            whereStage
        ]));

        const string expected = 
"""
FROM "CollectionName" PIPELINE {
  WHERE
    field1 != "Mars"
    AND field1 == "Moon"
}

""";
        Assert.That( ast.AsText(), Is.EqualTo(expected));

    }

    [Test]
    public void GroupByTest()
    {
        var ast = new AstAggregation("CollectionName");
        
        var expr = new AstExpressionOperation(
            AstExpressionOperation.OperationType.AND,
            new AstExpressionOperation(
                AstExpressionOperation.OperationType.NEQ,
                new AstExpressionVariable("field1"),
                new AstExpressionString("Mars")
            ),
            new AstExpressionOperation(
                AstExpressionOperation.OperationType.EQ,
                new AstExpressionVariable("field1"),
                new AstExpressionString("Moon")
            )
        );


        var groupByStage = new AstStageGroupBy(
            [
                new AstLetExpression(new AstExpressionVariable("idxField1")),
                new AstLetExpression(new AstExpressionVariable("idxField2"))
            ],
            [
                new AstLetExpression(expr, "IsMoon")
            ]
        );
        groupByStage.Add(expr);

        ast.Add(new AstPipeline([
            groupByStage
        ]));

        const string expected = 
"""
FROM "CollectionName" PIPELINE {
  GROUP BY
    idxField1,
    idxField2
    LET
      field1 != "Mars"
      AND field1 == "Moon" AS IsMoon
}

""";
        Assert.That( ast.AsText(), Is.EqualTo(expected));

    }

    [Test]
    public void JoinTest()
    {
        var ast = new AstAggregation("CollectionName");
        
        var expr = new AstExpressionOperation(
            AstExpressionOperation.OperationType.AND,
            new AstExpressionOperation(
                AstExpressionOperation.OperationType.NEQ,
                new AstExpressionVariable("field1"),
                new AstExpressionString("Mars")
            ),
            new AstExpressionOperation(
                AstExpressionOperation.OperationType.EQ,
                new AstExpressionVariable("field1"),
                new AstExpressionString("Moon")
            )
        );


        var joinStage = new AstStageJoin(
            "OtherCollection",
            "Data",
            [
            new(new("myField1"), new("otherField1")),
            new(new("myField2"), new("otherField2"))
            ],
            [new AstLetExpression(expr, "IsMoon")],
            new(
                [
                    new AstStageWhere(expr)
                ]
            )
        );

        ast.Add(new AstPipeline([
            joinStage
        ]));

        const string expected = 
"""
FROM "CollectionName" PIPELINE {
  JOIN "OtherCollection" AS Data ON
    myField1 == otherField1,
    myField2 == otherField2
    LET
      field1 != "Mars"
      AND field1 == "Moon" AS IsMoon
    PIPELINE {
      WHERE
        field1 != "Mars"
        AND field1 == "Moon"
    }
}

""";
        Assert.That( ast.AsText(), Is.EqualTo(expected));

    }

    [Test]
    public void DoTest()
    {
        var ast = new AstAggregation("CollectionName");
        
        var doStage = new AstStageDo();
        doStage.Json = JsonNode.Parse(
            """
            {
              "$bucket":
              {
                "groupBy": "$year_born"
              }
            }
            """
        );

        ast.Add(new AstPipeline([
            doStage
        ]));

        const string expected = 
"""
FROM "CollectionName" PIPELINE {
  DO 
  {
    "$bucket": {
      "groupBy": "$year_born"
    }
  }
}

""";
        Assert.That( ast.AsText().Replace("\r", ""), Is.EqualTo(expected.Replace("\r", "")));

    }

}
