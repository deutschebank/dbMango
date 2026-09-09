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
using System.Text.Json.Serialization.Metadata;

namespace Tests.Rms.Risk.Mango.Language;

[TestFixture]
public class AstToJsonTests
{
    private static readonly JsonSerializerOptions _prettyPrint = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

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
[
  {
    "$addFields": {
      "FieldName": "$field1",
      "field2": "$field2"
    }
  }
]
""";
        var result = ast.AsJson()?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
    }

    [Test]
    public void AddFieldsFunctionCall()
    {
        var ast = new AstAggregation("CollectionName");

        var expr = new AstExpressionFunctionCall(
            "concat",
            [
                new(null, new AstExpressionString("many")),
                new(null, new AstExpressionString(" miles away"))
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
[
  {
    "$addFields": {
      "Moon": {
        "$concat": [
          "many",
          " miles away"
        ]
      }
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
    }

    [Test]
    public void AddFieldsOperation()
    {
        var ast = new AstAggregation("CollectionName");

        var expr = new AstExpressionOperation(
            AstExpressionOperation.OperationType.PLUS,
            new AstExpressionVariable("someField"),
            new AstExpressionNumber(123)
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
[
  {
    "$addFields": {
      "Moon": {
        "$add": [
          "$someField",
          123
        ]
      }
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
    }


    [Test]
    public void AddFieldsComplexExpression()
    {
        var ast = new AstAggregation("CollectionName");

        var expr = new AstExpressionFunctionCall(
            "concat",
            [
                new( null, new AstExpressionFunctionCall("str",
                [
                    new( null, new AstExpressionOperation(
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
                            new(null,new AstExpressionNumber(-4))
                        ]
                        )
                    ))
                ])),
                new( null, new AstExpressionString(" miles away"))
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
[
  {
    "$addFields": {
      "Moon": {
        "$concat": [
          {
            "$str": 
              {
                "$subtract": [
                  {
                    "$add": [
                      {
                        "$divide": [
                          "$field1",
                          1.234
                        ]
                      },
                      5
                    ]
                  },
                  {
                    "$abs": 
                      -4
                    
                  }
                ]
              }
            
          },
          " miles away"
        ]
      }
    }
  }
]
""";
        var json = ast.AsJson()?.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void AddFieldsFuncNamedArgs()
    {
        var ast = new AstAggregation("CollectionName");

        var expr = new AstExpressionFunctionCall(
            "dateToString",
            [
              new("format", new AstExpressionString("%Y-%m-%d")),
              new("date",   new AstExpressionVariable("field1"))
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
[
  {
    "$addFields": {
      "Moon": {
        "$dateToString": {
          "format": "%Y-%m-%d",
          "date": "$field1"
        }
      }
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
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
[
  {
    "$project": {
      "_id": 1,
      "FieldName": "$field1"
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
    }

    [Test]
    public void ProjectWithComplexVarNamed()
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
            [
              {
                "$project": {
                  "_id": 1,
                  "FieldName": "$Very.Complex.Var"
                }
              }
            ]
            """;
        var json   = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
    }

    [Test]
    public void ProjectWithComplexVarUnnamed()
    {
        var ast = new AstAggregation("CollectionName");

        ast.Add(new AstPipeline([
            new AstStageProject(
                [],
                [
                    new AstLetExpression( new AstExpressionVariable("_id")),
                    new AstLetExpression( new AstExpressionVariable("Very.Complex.Var"))
                ]
            )
        ]));

        const string expected = 
            """
            [
              {
                "$project": {
                  "_id": 1,
                  "Very.Complex.Var": 1
                }
              }
            ]
            """;
        var json   = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
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
[
  {
    "$project": {
      "field1": 0
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
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
[
  {
    "$match": {
      "$and": [
        {
          "field1": {
            "$ne": "Mars"
          }
        },
        {
          "field1": "Moon"
        }
      ]
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
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
[
  {
    "$group": {
      "_id": {
        "idxField1": "$idxField1",
        "idxField2": "$idxField2"
      },
      "IsMoon": {
        "$and": [
          {
            "field1": {
              "$ne": "Mars"
            }
          },
          {
            "field1": "Moon"
          }
        ]
      }
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
    }

    [Test]
    public void GroupByWithAddToSet()
    {
        const string source =
            """
            [{
              "$group": {
                "_id": {
                  "CurvePrefix": "$CurvePrefix",
                  "CcyPair": "$CcyPair",
                  "Tenor": "$Tenor"
                },
                "Order": {
                  "$max": "$Order"
                },
                "Items": {
                  "$addToSet": {
                    "Name": {
                      "$concat": [
                        "$Delta",
                        " ",
                        "$Type"
                      ]
                    },
                    "Value": "$Value"
                  }
                }
              }
            }]
            """;

        var ast = LanguageParser.ParseAggregationJsonToAST("", source);
        
        var json   = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        var expected = JsonNode.Parse(source)?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
    }

    [Test]
    public void GroupBySingleField()
    {
        var ast = new AstAggregation("CollectionName");
        var groupByStage = new AstStageGroupBy(
            [
                new AstLetExpression(new AstExpressionVariable("idxField1")),
            ],
            [
            ]
        );

        ast.Add(new AstPipeline([
            groupByStage
        ]));

        const string expected = 
"""
[
  {
    "$group": {
      "_id": "$idxField1"
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
    }

    [Test]
    public void GroupByExpression()
    {
        var ast = new AstAggregation("CollectionName");
        var groupByStage = new AstStageGroupBy(
            [
                new AstLetExpression(new AstExpressionNull()),
            ],
            [
            ]
        );

        ast.Add(new AstPipeline([
            groupByStage
        ]));

        const string expected = 
"""
[
  {
    "$group": {
      "_id": null
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
    }

    [Test]
    public void JoinTest()
    {
        var ast = new AstAggregation("CollectionName");
        
        var expr1 = new AstExpressionOperation(
            AstExpressionOperation.OperationType.LTE,
            new AstExpressionVariable("distance"),
            new AstExpressionNumber(384400.123)
        );

        var expr2 = new AstExpressionOperation(
            AstExpressionOperation.OperationType.NEQ,
            new AstExpressionVariable("planet"),
            new AstExpressionString("Mars")
        );

        var joinStage = new AstStageJoin(
            "OtherCollection",
            "Data",
            [
            new(new("myField1"), new("otherField1")),
            new(new("myField2"), new("otherField2"))
            ],
            [new AstLetExpression(expr1, "IsMoon")],
            new(
                [
                    new AstStageWhere(expr2)
                ]
            )
        );

        ast.Add(new AstPipeline([
            joinStage
        ]));

        const string expected = 
"""
[
  {
    "$lookup": {
      "from": "OtherCollection",
      "localField": "myField1",
      "foreignField": "otherField1",
      "as": "Data",
      "let": {
        "IsMoon": {
          "distance": {
            "$lte": 384400.123
          }
        }
      },
      "pipeline": [
        {
          "$match": {
            "planet": {
              "$ne": "Mars"
            }
          }
        }
      ]
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
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
[
  {
    "$bucket": {
      "groupBy": "$year_born"
    }
  }
]
""";
        var json = ast.AsJson();
        var result = json?.ToJsonString(_prettyPrint);
        Assert.That( result, Is.EqualTo(expected));
    }
}
