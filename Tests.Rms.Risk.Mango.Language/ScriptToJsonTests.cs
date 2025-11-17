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
﻿using System.Text.Json.Serialization.Metadata;
using Tests.Rms.Risk.Mango.Language.Data;

namespace Tests.Rms.Risk.Mango.Language;

[TestFixture]
public class ScriptToJsonTests
{
    private static readonly JsonSerializerOptions _prettyPrint = new()
    {
        WriteIndented    = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };


    [SetUp]
    public void Setup()
    {
    }

    const string Pipeline = 
    """
    FROM "CollectionName"
    PIPELINE {
        WHERE // special syntax
            ( cob == "2025-04-22" && Department == "FX Options" )
            && Book NOT IN ("CHFX", "JPYPJPY")
        ADD // special syntax
            pv + premiumPV AS TotalPV,
            abs( pv + pvMove ) AS MyPnl,                                    // function call
            dateToString( format: "%Y-%m-%d", date: field1 ) AS "TodayStr", // named args
            {
                "data" AS Key,
                "value" AS Value
            } AS Nested,                                                     // nested object
            [{
                "data1" AS Key,
                "v1" AS Value
            },
            {
                "data2" AS Key,
                "v2" AS Value
            }] AS NestedArray                                                // nested array
        PROJECT
            _id,
            TotalPV / 2 AS TotalPV
        PROJECT EXCLUDE TotalPV, MyPnl
        JOIN  "MarketCollection" AS "Data" ON srcCCY == CCY // source data => TO
            LET
                spot * DF AS todayRate
        GROUP BY ccy, tenor
            LET
                sum(pv) AS TotalPV,
                sum(delta) AS TotalDelta
        DO {
          "$bucket": // plain JSON for unknown stages
          {
              "groupBy": "$year_born",                        // Field to group by
              boundaries: [ 1840, 1850, 1860, 1870, 1880 ], // Boundaries for the buckets
              default: "Other",                             // Bucket ID for documents which do not fall into a bucket
              output: {                                     // Output for each bucket
                  "count": { "$sum": 1 },
                  "artists" :
                  {
                      "$push": {
                          "name": { "$concat": [ "$first_name", " ", "$last_name"] },
                          "year_born": $year_born
                      }
                  }
              }
          }
        }
        SORT BY _id
    }
    """;


    [Test]
    public void ParseTest()
    {

        var ast = LanguageParser.ParseScriptToAST(Pipeline);
        Assert.Multiple( () =>
        {
            Assert.That( ast, Is.Not.Null );
            Assert.That( ast.Pipeline, Is.Not.Null );
            Assert.That( ast.Pipeline!.Stages.Count, Is.EqualTo(8) );
            var count = 0;
            Assert.That( ast.Pipeline!.Stages[count++], Is.InstanceOf<AstStageWhere>() );
            Assert.That( ast.Pipeline!.Stages[count++], Is.InstanceOf<AstStageAddFields>() );
            Assert.That( ast.Pipeline!.Stages[count++], Is.InstanceOf<AstStageProject>() );
            Assert.That( ast.Pipeline!.Stages[count++], Is.InstanceOf<AstStageProject>() );
            Assert.That( ast.Pipeline!.Stages[count++], Is.InstanceOf<AstStageJoin>() );
            Assert.That( ast.Pipeline!.Stages[count++], Is.InstanceOf<AstStageGroupBy>() );
            Assert.That( ast.Pipeline!.Stages[count++], Is.InstanceOf<AstStageDo>() );
        });
    }

    [Test]
    public void ReParseTest()
    {

        var ast = LanguageParser.ParseScriptToAST(Pipeline);
        Assert.That( ast, Is.Not.Null );

        var newPipeline = ast!.AsText();
        var newAst = LanguageParser.ParseScriptToAST(newPipeline);
        Assert.That( newAst, Is.Not.Null );
        var newPipeline2 = newAst.AsText();
        Assert.That( newPipeline2, Is.EqualTo(newPipeline) );
    }

    [Test]
    public void ProjectWithComplexVarNamed()
    {
        const string pipeline = 
            """
            FROM "CollectionName" PIPELINE {
              PROJECT
                _id,
                "$Very.Complex.Var" AS FieldName
            }

            """;

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

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void FloatNumbers()
    {
        const string pipeline = 
            """
            FROM "CollectionName" PIPELINE {
            WHERE
              (RhoImpact > 1
                OR RhoImpact < -1
                OR Rho2NdOrderImpact > 1.234e-8
                OR Rho2NdOrderImpact < -1E-08)
            }

            """;

        const string expected =
            """
            [
              {
                "$match": {
                  "$or": [
                    {
                      "RhoImpact": {
                        "$gt": 1
                      }
                    },
                    {
                      "RhoImpact": {
                        "$lt": -1
                      }
                    },
                    {
                      "Rho2NdOrderImpact": {
                        "$gt": 1.234E-08
                      }
                    },
                    {
                      "Rho2NdOrderImpact": {
                        "$lt": -1E-08
                      }
                    }
                  ]
                }
              }
            ]
            """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void InvertedCondition()
    {
        const string pipeline = 
            """
            FROM "CollectionName" PIPELINE {
            WHERE
              1 == Field
            }

            """;

        const string expected =
            """
            [
              {
                "$match": {
                  "Field": 1
                }
              }
            ]
            """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void ProjectWithComplexVarUnnamed()
    {
        const string pipeline = 
            """
            FROM "CollectionName" PIPELINE {
              PROJECT
                "$Very Complex Var"
            }

            """;

        const string expected =
            """
            [
              {
                "$project": {
                  "Very Complex Var": 1
                }
              }
            ]
            """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void ProjectWithSimpleVarUnnamed()
    {
        const string pipeline = 
            """
            FROM "CollectionName" PIPELINE {
              PROJECT
                SimpleVar
            }

            """;

        const string expected =
            """
            [
              {
                "$project": {
                  "SimpleVar": 1
                }
              }
            ]
            """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void ProjectWithDollarVarUnnamed()
    {
        const string pipeline = 
            """
            FROM "CollectionName" PIPELINE {
              PROJECT
                $Simple.Var
            }

            """;

        const string expected =
            """
            [
              {
                "$project": {
                  "Simple.Var": 1
                }
              }
            ]
            """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void WhereSimpleAnd()
    {
        const string pipeline = 
            """
            FROM "CollectionName" PIPELINE {
              WHERE
                Field1 == 123
                AND Field2 == 456
            }

            """;

        const string expected =
            """
            [
              {
                "$match": {
                  "$and": [
                    {
                      "Field1": 123
                    },
                    {
                      "Field2": 456
                    }
                  ]
                }
              }
            ]
            """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void SortWithOptions()
    {
        const string pipeline = 
            """
            FROM "CollectionName" PIPELINE {
              SORT BY
                Field1
                OPTIONS { "Field1" : { "$meta" : "textScore" } }
            }

            """;

        const string expected =
            """
            [
              {
                "$sort": {
                  "Field1": {
                    "$meta": "textScore"
                  }
                }
              }
            ]
            """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void Bucket()
    {
        const string pipeline = 
            """
            FROM "CollectionName" PIPELINE {
              BUCKET
                Field1 / 100.0
                BOUNDARIES 1,10,100,1000
                DEFAULT "Ignored"
                LET
                    Field1 / 100.0 AS Gain 
            }

            """;

        const string expected =
            """
            [
              {
                "$bucket": {
                  "groupBy": {
                    "$divide": [
                      "$Field1",
                      100
                    ]
                  },
                  "boundaries": [
                    1,
                    10,
                    100,
                    1000
                  ],
                  "default": "Ignored",
                  "output": {
                    "Gain": {
                      "$divide": [
                        "$Field1",
                        100
                      ]
                    }
                  }
                }
              }
            ]
            """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void BucketAuto()
    {
        const string pipeline = 
            """
            FROM "CollectionName" PIPELINE {
              BUCKET AUTO
                Field1 / 100.0
                BUCKETS 5
                GRANULARITY "POWERSOF2"
                LET
                    Field1 / 100.0 AS Gain 
            }

            """;

        const string expected =
            """
            [
              {
                "$bucketAuto": {
                  "groupBy": {
                    "$divide": [
                      "$Field1",
                      100
                    ]
                  },
                  "buckets": 5,
                  "granularity": "POWERSOF2",
                  "output": {
                    "Gain": {
                      "$divide": [
                        "$Field1",
                        100
                      ]
                    }
                  }
                }
              }
            ]
            """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void Subtract()
    {
        const string pipeline = 
        """
        FROM "TestCollection" PIPELINE {
          PROJECT
            ClosingRho - OpeningRho AS RhoMove
        }
        
        """;

        const string expected =
        """
        [{
          "$project" : {
            "RhoMove" : {
              "$subtract" : ["$ClosingRho", "$OpeningRho"]
            }
          }
        }]
        """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test]
    public void Facet()
    {
        const string pipeline = 
            """
            FROM "TestCollection" PIPELINE {
              FACET
                RhoBranch PIPELINE {
                  PROJECT
                    ClosingRho - OpeningRho AS RhoMove
                },
                VegaBranch PIPELINE {
                  PROJECT
                    ClosingVega - OpeningVega AS VegaMove
                }
            }

            """;

        const string expected =
            """
            [
              {
                "$facet": {
                  "RhoBranch": [
                    {
                      "$project": {
                        "RhoMove": {
                          "$subtract": [
                            "$ClosingRho",
                            "$OpeningRho"
                          ]
                        }
                      }
                    }
                  ],
                  "VegaBranch": [
                    {
                      "$project": {
                        "VegaMove": {
                          "$subtract": [
                            "$ClosingVega",
                            "$OpeningVega"
                          ]
                        }
                      }
                    }
                  ]
                }
              }
            ]
            """;

        var ast = LanguageParser.ParseScriptToAST(pipeline);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(expected)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }


    [Test, Ignore("Equality expected to be in a different format")]
    public void FxVegaBreakdownByTenorTest()
    {

        var ast = LanguageParser.ParseScriptToAST(FxVegaBreakdownByTenor.Script);
        Assert.That( ast, Is.Not.Null );

        var json = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(FxVegaBreakdownByTenor.Pipeline)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

    [Test, Ignore("Equality expected to be in a different format")]
    public void RhoBreakdownByTenorTest()
    {

        var ast = LanguageParser.ParseScriptToAST(RhoBreakdownByTenor.Script);
        Assert.That( ast, Is.Not.Null );

        var json              = ast!.AsJson()!.ToJsonString(_prettyPrint);
        var formattedPipeline = JsonNode.Parse(RhoBreakdownByTenor.Pipeline)!.ToJsonString(_prettyPrint);
        Assert.That( json, Is.EqualTo(formattedPipeline) );
    }

}
