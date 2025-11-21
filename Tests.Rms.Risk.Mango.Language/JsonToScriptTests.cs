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
public class JsonToScriptTests
{
    private const string MatchStage = 
    """
[{
"$match" : {
  "$and" : [{
      "$and" : [{
          "COB" : { "$date" : "2025-04-25T00:00:00Z" }
        }, {
          "$or" : [{
              "Department" : "FX Forwards Controlling"
            }, {
              "Department" : "FX Metals Controlling"
            }, {
              "Department" : "FX Options Controlling"
            }]
        }]
    }]
  }
}]
""";
    private const string MatchScript = 
"""
FROM "TestCollection" PIPELINE {
  WHERE
    COB == date( "2025-04-25T00:00:00Z" )
    AND (Department == "FX Forwards Controlling"
      OR Department == "FX Metals Controlling"
      OR Department == "FX Options Controlling")
}

""";

    private const string ProjectStage = 
    """
[{
    "$project" : {
      "CurvePrefix" : 1,
      "Data" : {
        "$objectToArray" : "$RhoDetails"
      }
    }
}]
""";
    private const string ProjectScript = 
"""
FROM "TestCollection" PIPELINE {
  PROJECT
    CurvePrefix,
    objectToArray( RhoDetails ) AS Data
}

""";

    private const string ProjectWithComplexVarNamedStage = 
        """
        [{
            "$project" : {
              "Data" : "$Very.Complex.Var"
            }
        }]
        """;
    private const string ProjectWithComplexVarNamedScript = 
        """
        FROM "TestCollection" PIPELINE {
          PROJECT
            $Very.Complex.Var AS Data
        }

        """;

    private const string ProjectWithComplexVarUnnamedStage = 
        """
        [{
            "$project" : {
              "$Very.Complex.Var" : 1
            }
        }]
        """;
    private const string ProjectWithComplexVarUnnamedScript = 
        """
        FROM "TestCollection" PIPELINE {
          PROJECT
            $Very.Complex.Var
        }

        """;

    private const string ProjectExcludeStage = 
    """
[{
    "$project" : {
      "CurvePrefix" : 0,
      "Data" : 0
    }
}]
""";
    private const string ProjectExcludeScript = 
"""
FROM "TestCollection" PIPELINE {
  PROJECT EXCLUDE
    CurvePrefix,
    Data
}

""";

    // https://www.mongodb.com/docs/manual/reference/operator/query/exists/
    private const string MatchWithProjectionStage = 
"""
[{
  "$match" : {
    "$or" : [{
        "RhoDetails.RhoImpact" : {
          "$exists" : true
        }
      }, {
        "RhoDetails.Rho2NdOrderImpact" : {
          "$exists" : true
        }
      }]
  }
}]
""";

    private const string MatchWithProjectionScript = 
"""
FROM "TestCollection" PIPELINE {
  WHERE
    $RhoDetails.RhoImpact EXISTS
    OR $RhoDetails.Rho2NdOrderImpact EXISTS
}

""";

    private const string ProjectArrayStage = 
"""
[{
    "$project" : {
      "CurveKey" : {
        "$concat" : ["$CurvePrefix", "$Data.k"]
      },
      "Ccy" : 1,
      "Curve" : 1,
      "FundingName" : 1,
      "Tenor" : "$Data.k",
      "Order" : 1,
      "Data" : [{
          "Type" : "$Type",
          "Value" : "$Data.v"
        }]
    }
}]
""";

    private const string ProjectArrayScript = 
"""
FROM "TestCollection" PIPELINE {
  PROJECT
    concat( CurvePrefix, $Data.k ) AS CurveKey,
    Ccy,
    Curve,
    FundingName,
    $Data.k AS Tenor,
    Order,
    [
      {
        Type,
        $Data.v AS Value
      }
    ] AS Data
}

""";

    private const string ProjectArrayComplexStage = 
        """
        [{
          "$project" : {
            "Tenor" : 1,
            "Value" : {
              "$arrayToObject" : {
                "$map" : {
                  "input" : "$Data",
                  "as" : "d",
                  "in" : ["$$d.Type", "$$d.Value"]
                }
              }
            }
          }
        }]
        """;

    private const string ProjectArrayComplexScript = 
        """
        FROM "TestCollection" PIPELINE {
          PROJECT
            Tenor,
            arrayToObject( map( 
                input: Data, 
                as: "d", 
                in: [
                    "$$d.Type",
                    "$$d.Value"
                  ]
              ) ) AS Value
        }

        """;

    private const string ProjectWithNamedArgsStage =
"""
[{
    "$project" : {
      "Value" : {
        "$map" : {
          "input" : "$Data",
          "as" : "d",
          "in" : ["$$d.Type", "$$d.Value"]
        }
      }
    }
}]
""";    

    private const string ProjectWithNamedArgsScript =
"""
FROM "TestCollection" PIPELINE {
  PROJECT
    map( 
      input: Data, 
      as: "d", 
      in: [
          "$$d.Type",
          "$$d.Value"
        ]
    ) AS Value
}

""";

    private const string ProjectNestedFuncStage =
"""
[ {
    "$project" : {
      "Value" : {
        "$arrayToObject" : {
          "$map" : {
            "input" : "$Data",
            "as" : "d",
            "in" : ["$$d.Type", "$$d.Value"]
          }
        }
      }
    }
}]
""";    

    private const string ProjectNestedFuncScript =
"""
FROM "TestCollection" PIPELINE {
  PROJECT
    arrayToObject( map( 
        input: Data, 
        as: "d", 
        in: [
            "$$d.Type",
            "$$d.Value"
          ]
      ) ) AS Value
}

""";

    private const string GroupStage =
"""
[{
  "$group" : {
    "_id" : {
      "CurveKey" : "$CurveKey",
      "Ccy" : "$Ccy",
      "Curve" : "$Curve",
      "FundingName" : "$FundingName",
      "Tenor" : "$Tenor"
    },
    "Order" : {
      "$max" : "$Order"
    },
    "Rho2ndOrderImpact" : {
      "$sum" : "$Value.Rho2ndOrderImpact"
    }
  }
}]
""";    

    private const string GroupScript =
"""
FROM "TestCollection" PIPELINE {
  GROUP BY
    CurveKey,
    Ccy,
    Curve,
    FundingName,
    Tenor
    LET
      max( Order ) AS Order,
      sum( $Value.Rho2ndOrderImpact ) AS Rho2ndOrderImpact
}

""";

    private const string MatchUnaryExpressionsStage =
"""
[{
  "$match" : {
    "$or" : [{
        "RhoImpact" : {
          "$gt" : 1E-08
        }
      }]
  }
}]
""";

    private const string MatchUnaryExpressionsScript = 
"""
FROM "TestCollection" PIPELINE {
  WHERE
    RhoImpact > 1E-08
}

"""; 

  private const string LookupStage = 
"""
[{
  "$lookup" : {
    "from" : "PnL-Market",
    "localField" : "_id.CurveKey",
    "foreignField" : "_id",
    "as" : "Market"
  }
}]
""";
  private const string LookupScript = 
"""
FROM "TestCollection" PIPELINE {
  JOIN "PnL-Market" AS Market ON
    $_id.CurveKey == _id
}

""";

    private const string ProjectWithIdStage = 
"""
[{
  "$project" : {
    "_id" : {
      "Ccy" : 1,
      "Curve" : 1
    },
    "OpeningCurve" : "$OpeningCurve"
  }
}]
""";

    private const string ProjectWithIdScript = 
"""
FROM "TestCollection" PIPELINE {
  PROJECT ID {
      Ccy,
      Curve
    }
    OpeningCurve
}

""";

    private const string AddFieldsStage = 
"""
[{
  "$addFields" : {
    "_id.Order" : "$Order",
    "_id.Data" : [{
        "k" : "OpeningCurve",
        "v" : "$Market.Opening"
      }, {
        "k" : "CurveMove",
        "v" : "$Market.Move"
      }, {
        "k" : "Value",
        "v" : "$Value"
      }]
  }
}]
""";    

    private const string AddFieldsScript = 
"""
FROM "TestCollection" PIPELINE {
  ADD
    Order AS "_id.Order",
    [
      {
        "OpeningCurve" AS "k",
        $Market.Opening AS "v"
      },
      {
        "CurveMove" AS "k",
        $Market.Move AS "v"
      },
      {
        "Value" AS "k",
        Value AS "v"
      }
    ] AS "_id.Data"
}

""";

    private const string SubtractStage =
        """
        [{
          "$project" : {
            "RhoMove" : {
              "$subtract" : ["$ClosingRho", "$OpeningRho"]
            }
          }
          }]
        """;

    private const string SubtractScript = 
        """
        FROM "TestCollection" PIPELINE {
          PROJECT
            ClosingRho - OpeningRho AS RhoMove
        }

        """;

    private const string BucketStage =
        """
        [
          {
            "$bucket": {
              "groupBy": {
                "$divide": [
                  "$Field1",
                  100.1
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
                    100.1
                  ]
                }
              }
            }
          }
        ]
        """;

    private const string BucketScript = 
        """
        FROM "TestCollection" PIPELINE {
          BUCKET
            Field1 / 100.1
            BOUNDARIES 1, 10, 100, 1000
            DEFAULT "Ignored"
            LET
              Field1 / 100.1 AS Gain
        }
        
        """;

    private const string BucketAutoStage =
        """
        [
          {
            "$bucketAuto": {
              "groupBy": {
                "$divide": [
                  "$Field1",
                  100.1
                ]
              },
              "buckets": 5,
              "granularity": "POWERSOF2",
              "output": {
                "Gain": {
                  "$divide": [
                    "$Field1",
                    100.1
                  ]
                }
              }
            }
          }
        ]
        """;

    private const string BucketAutoScript = 
        """
        FROM "TestCollection" PIPELINE {
          BUCKET AUTO
            Field1 / 100.1
            BUCKETS 5
            GRANULARITY "POWERSOF2"
            LET
              Field1 / 100.1 AS Gain
        }

        """;

    private const string FacetStage =
        """
        [
          {
            "$facet": {
              "categorizedByTags": [
                { "$unwind": "$tags" },
                { "$sortByCount": "$tags" }
              ],
              "categorizedByPrice": [
                { "$match": { "price": { "$exists": 1 } } },
                {
                  "$bucket": {
                    "groupBy": "$price",
                    "boundaries": [  0, 150, 200, 300, 400 ],
                    "default": "Other",
                    "output": {
                      "count": { "$sum": 1 },
                      "titles": { "$push": "$title" }
                    }
                  }
                }
              ],
              "categorizedByYears(Auto)": [
                {
                  "$bucketAuto": {
                    "groupBy": "$year",
                    "buckets": 4
                  }
                }
              ]
            }
          }
        ]
        """;

    private const string FacetScript = 
        """
        FROM "TestCollection" PIPELINE {
          FACET
            categorizedByTags PIPELINE {
              UNWIND tags
              DO 
              {
                "$sortByCount": "$tags"
              }
            },
            categorizedByPrice PIPELINE {
              WHERE
                price == exists( 1 )
              BUCKET
                price
                BOUNDARIES 0, 150, 200, 300, 400
                DEFAULT "Other"
                LET
                  sum( 1 ) AS count,
                  push( title ) AS titles
            },
            'categorizedByYears(Auto)' PIPELINE {
              BUCKET AUTO
                year
                BUCKETS 4
            }
        }
        
        """;

    private const string MatchHackExistsStage = 
        """
        [{
          "$match": {
            "$or": [
              { "RhoDetails.RhoImpact": { "$gt": {} } },
              { "RhoDetails.Rho2NdOrderImpact": { "$gt": {} } }
            ]
          }
        }]
        """;
    private const string MatchHackExistsScript = 
        """
        FROM "TestCollection" PIPELINE {
          WHERE
            $RhoDetails.RhoImpact == exists( true )
            OR $RhoDetails.Rho2NdOrderImpact == exists( true )
        }
        
        """;

    private const string MatchExtraFilterStage = 
        """
        [{
          "$match": { 
            "$and" : [
                { "COB" : { "$eq" : { "$date" : "2025-06-02T00:00:00Z" } } }, 
                { "$or" : [
                    { "Department" : { "$eq" : "CPM" } }, 
                    { "Department" : { "$eq" : "FX Metals Controlling" } }
                ]}
            ]}
        }]
        """;
    private const string MatchExtraFilterScript = 
        """
        FROM "TestCollection" PIPELINE {
          WHERE
            COB == date( "2025-06-02T00:00:00Z" )
            AND (Department == "CPM"
              OR Department == "FX Metals Controlling")
        }

        """;

    private static void TestPipeline(string pipeline, string expectedScript)
    {
        var ast = LanguageParser.ParseAggregationJsonToAST("TestCollection", pipeline);
        var text = ast.AsText();

        Assert.Multiple( () =>
        {
            Assert.That( ast, Is.Not.Null );
            Assert.That( ast.Pipeline, Is.Not.Null );

            Assert.That( text.Replace("\r", ""), Is.EqualTo(expectedScript.Replace("\r", "")) );
        });

    }


    [Test] public void FxVegaBreakdownByTenor()       => TestPipeline(Data.FxVegaBreakdownByTenor.Pipeline, Data.FxVegaBreakdownByTenor.Script);
    [Test] public void RhoBreakdownByTenor()          => TestPipeline(Data.RhoBreakdownByTenor.Pipeline, Data.RhoBreakdownByTenor.Script);
    [Test] public void Match()                        => TestPipeline(MatchStage, MatchScript);
    [Test] public void MatchWithProjection()          => TestPipeline(MatchWithProjectionStage, MatchWithProjectionScript);
    [Test] public void Project()                      => TestPipeline(ProjectStage, ProjectScript);
    [Test] public void ProjectWithComplexVarNamed()   => TestPipeline(ProjectWithComplexVarNamedStage, ProjectWithComplexVarNamedScript);
    [Test] public void ProjectWithComplexVarUnnamed() => TestPipeline(ProjectWithComplexVarUnnamedStage, ProjectWithComplexVarUnnamedScript);
    [Test] public void ProjectExclude()               => TestPipeline(ProjectExcludeStage, ProjectExcludeScript);
    [Test] public void ProjectArray()                 => TestPipeline(ProjectArrayStage, ProjectArrayScript);
    [Test] public void ProjectArrayComplex()          => TestPipeline(ProjectArrayComplexStage, ProjectArrayComplexScript);
    [Test] public void ProjectWithNamedArgs()         => TestPipeline(ProjectWithNamedArgsStage, ProjectWithNamedArgsScript);
    [Test] public void ProjectNestedFunc()            => TestPipeline(ProjectNestedFuncStage, ProjectNestedFuncScript);
    [Test] public void Group()                        => TestPipeline(GroupStage, GroupScript);
    [Test] public void MatchUnaryExpressions()        => TestPipeline(MatchUnaryExpressionsStage, MatchUnaryExpressionsScript);
    [Test] public void Lookup()                       => TestPipeline(LookupStage, LookupScript);
    [Test] public void ProjectWithId()                => TestPipeline(ProjectWithIdStage, ProjectWithIdScript);
    [Test] public void AddFields()                    => TestPipeline(AddFieldsStage, AddFieldsScript);
    [Test] public void Subtract()                     => TestPipeline(SubtractStage, SubtractScript);
    [Test] public void Bucket()                       => TestPipeline(BucketStage, BucketScript);
    [Test] public void BucketAuto()                   => TestPipeline(BucketAutoStage, BucketAutoScript);
    [Test] public void Facet()                        => TestPipeline(FacetStage, FacetScript);
    [Test] public void MatchHackExists()              => TestPipeline(MatchHackExistsStage, MatchHackExistsScript);
    [Test] public void MatchExtraFilter()             => TestPipeline(MatchExtraFilterStage, MatchExtraFilterScript);

}
