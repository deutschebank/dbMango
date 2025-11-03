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
namespace Tests.Rms.Risk.Mango.Language.Data;

public static class RhoBreakdownByTenor
{
    public const string Pipeline = 
 """
[{
    "$match" : {
      "$and" : [{
          "$and" : [{
              "COB" : { "$date" : "2025-04-25T00:00:00Z" }
            }]
        }]
    }
  }, {
    "$project" : {
      "RhoDetails.OpeningRho" : "$RhoDetails.OpeningRho",
      "RhoDetails.ClosingRho" : "$RhoDetails.ClosingRho",
      "RhoDetails.RhoImpact" : "$RhoDetails.RhoImpact",
      "RhoDetails.Rho2ndOrderImpact" : "$RhoDetails.Rho2ndOrderImpact",
      "CurvePrefix" : {
        "$concat" : [{
            "$dateToString" : {
              "format" : "%Y%m%d",
              "date" : "$COB"
            }
          }, "-", {
            "$toString" : "$OpeningYcRateSetId"
          }, "-", {
            "$toString" : "$ClosingYcRateSetId"
          }, "-PnL-YC", {
            "$ifNull" : ["$Ds2CurveType", ""]
          }, "-"]
      }
    }
  }, {
    "$project" : {
      "CurvePrefix" : 1,
      "Data" : {
        "$objectToArray" : "$RhoDetails"
      }
    }
  }, {
    "$unwind" : {
      "path" : "$Data"
    }
  }, {
    "$project" : {
      "CurvePrefix" : 1,
      "Type" : "$Data.k",
      "Data" : {
        "$objectToArray" : "$Data.v"
      }
    }
  }, {
    "$unwind" : {
      "path" : "$Data"
    }
  }, {
    "$project" : {
      "CurvePrefix" : {
        "$concat" : ["$CurvePrefix", "$Data.k", "-"]
      },
      "Type" : 1,
      "Ccy" : "$Data.v.Ccy",
      "Curve" : {
        "$ifNull" : ["$Data.v.Curve", "$Data.v.CurveName", "$Data.v.CurveType"]
      },
      "FundingName" : "$Data.v.FundingName",
      "Data" : {
        "$objectToArray" : "$Data.v.Data"
      }
    }
  }, {
    "$unwind" : {
      "path" : "$Data",
      "includeArrayIndex" : "Order"
    }
  }, {
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
  }, {
    "$project" : {
      "CurveKey" : 1,
      "Ccy" : 1,
      "Curve" : 1,
      "FundingName" : 1,
      "Tenor" : 1,
      "Order" : 1,
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
  }, {
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
      "OpeningRho" : {
        "$sum" : "$Value.OpeningRho"
      },
      "ClosingRho" : {
        "$sum" : "$Value.ClosingRho"
      },
      "RhoImpact" : {
        "$sum" : "$Value.RhoImpact"
      },
      "Rho2ndOrderImpact" : {
        "$sum" : "$Value.Rho2ndOrderImpact"
      }
    }
  }, {
    "$match" : {
      "$or" : [{
          "RhoImpact" : {
            "$gt" : 1E-08
          }
        }, {
          "RhoImpact" : {
            "$lt" : -1E-08
          }
        }, {
          "Rho2NdOrderImpact" : {
            "$gt" : 1E-08
          }
        }, {
          "Rho2NdOrderImpact" : {
            "$lt" : -1E-08
          }
        }]
    }
  }, {
    "$lookup" : {
      "from" : "PnL-Market",
      "localField" : "_id.CurveKey",
      "foreignField" : "_id",
      "as" : "Market"
    }
  }, {
    "$unwind" : {
      "path" : "$Market"
    }
  }, {
    "$project" : {
      "Order" : "$Order",
      "OpeningCurve" : "$Market.Opening",
      "ClosingCurve" : "$Market.Closing",
      "CurveMove" : "$Market.Move",
      "OpeningRho" : "$OpeningRho",
      "ClosingRho" : "$ClosingRho",
      "RhoMove" : {
        "$subtract" : ["$ClosingRho", "$OpeningRho"]
      },
      "RhoImpact" : "$RhoImpact",
      "Rho2ndOrderImpact" : "$Rho2ndOrderImpact"
    }
  }, {
    "$sort" : {
      "_id.Ccy" : 1,
      "_id.Curve" : 1,
      "_id.FundingName" : 1,
      "Tenor" : 1,
      "Order" : 1
    }
  }, {
    "$project" : {
      "_id" : {
        "Ccy" : 1,
        "Curve" : 1,
        "FundingName" : 1,
        "Tenor" : 1
      },
      "OpeningCurve" : "$OpeningCurve",
      "ClosingCurve" : "$ClosingCurve",
      "CurveMove" : "$CurveMove",
      "OpeningRho" : "$OpeningRho",
      "ClosingRho" : "$ClosingRho",
      "RhoMove" : "$RhoMove",
      "RhoImpact" : "$RhoImpact",
      "Rho2ndOrderImpact" : "$Rho2ndOrderImpact"
    }
  }]
""";

    public const string Script = 
"""
FROM "TestCollection" PIPELINE {
  WHERE
    COB == date( "2025-04-25T00:00:00Z" )
  PROJECT
    $RhoDetails.OpeningRho,
    $RhoDetails.ClosingRho,
    $RhoDetails.RhoImpact,
    $RhoDetails.Rho2ndOrderImpact,
    concat( dateToString( 
        format: "%Y%m%d", 
        date: COB
      ), "-", toString( OpeningYcRateSetId ), "-", toString( ClosingYcRateSetId ), "-PnL-YC", ifNull( Ds2CurveType, "" ), "-" ) AS CurvePrefix
  PROJECT
    CurvePrefix,
    objectToArray( RhoDetails ) AS Data
  UNWIND Data
  PROJECT
    CurvePrefix,
    $Data.k AS Type,
    objectToArray( $Data.v ) AS Data
  UNWIND Data
  PROJECT
    concat( CurvePrefix, $Data.k, "-" ) AS CurvePrefix,
    Type,
    $Data.v.Ccy AS Ccy,
    ifNull( $Data.v.Curve, $Data.v.CurveName, $Data.v.CurveType ) AS Curve,
    $Data.v.FundingName AS FundingName,
    objectToArray( $Data.v.Data ) AS Data
  UNWIND Data INDEX Order
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
  PROJECT
    CurveKey,
    Ccy,
    Curve,
    FundingName,
    Tenor,
    Order,
    arrayToObject( map( 
        input: Data, 
        as: "d", 
        in: [
            "$$d.Type",
            "$$d.Value"
          ]
      ) ) AS Value
  GROUP BY
    CurveKey,
    Ccy,
    Curve,
    FundingName,
    Tenor
    LET
      max( Order ) AS Order,
      sum( $Value.OpeningRho ) AS OpeningRho,
      sum( $Value.ClosingRho ) AS ClosingRho,
      sum( $Value.RhoImpact ) AS RhoImpact,
      sum( $Value.Rho2ndOrderImpact ) AS Rho2ndOrderImpact
  WHERE
    (RhoImpact > 1E-08
      OR RhoImpact < -1E-08
      OR Rho2NdOrderImpact > 1E-08
      OR Rho2NdOrderImpact < -1E-08)
  JOIN "PnL-Market" AS Market ON
    $_id.CurveKey == _id
  UNWIND Market
  PROJECT
    Order,
    $Market.Opening AS OpeningCurve,
    $Market.Closing AS ClosingCurve,
    $Market.Move AS CurveMove,
    OpeningRho,
    ClosingRho,
    ClosingRho - OpeningRho AS RhoMove,
    RhoImpact,
    Rho2ndOrderImpact
  SORT BY
        $_id.Ccy,
        $_id.Curve,
        $_id.FundingName,
        Tenor,
        Order
  PROJECT ID {
      Ccy,
      Curve,
      FundingName,
      Tenor
    }
    OpeningCurve,
    ClosingCurve,
    CurveMove,
    OpeningRho,
    ClosingRho,
    RhoMove,
    RhoImpact,
    Rho2ndOrderImpact
}

""";

}
