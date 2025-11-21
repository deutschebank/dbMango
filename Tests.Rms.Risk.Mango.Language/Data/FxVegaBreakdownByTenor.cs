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

internal static class FxVegaBreakdownByTenor
{
    public const string Pipeline = 
"""
[{
  "$match" : {
            "COB" : { "$date" : "2025-05-01T00:00:00Z" }
  }
}, {
  "$match" : {
    "VegaDetails.VegaImpact" : {
      "$exists" : true
    }
  }
}, {
  "$project" : {
    "HedgeLVSVInstruments" : 1,
    "VegaDetails.OpeningVega" : "$VegaDetails.OpeningVega",
    "VegaDetails.ClosingVega" : "$VegaDetails.ClosingVega",
    "VegaDetails.VegaImpact" : "$VegaDetails.VegaImpact",
    "VegaDetails.Vega2ndOrderImpact" : "$VegaDetails.Vega2ndOrderImpact",
    "CurvePrefix" : {
      "$concat" : [{
          "$dateToString" : {
            "format" : "%Y%m%d",
            "date" : "$COB"
          }
        }, "-", {
          "$toString" : "$OpeningVolRateSetId"
        }, "-", {
          "$toString" : "$ClosingVolRateSetId"
        }, "-PnL-Vol-"]
    }
  }
}, {
  "$project" : {
    "HedgeLVSVInstruments" : 1,
    "CurvePrefix" : 1,
    "VegaDetails" : {
      "$objectToArray" : "$VegaDetails"
    }
  }
}, {
  "$unwind" : {
    "path" : "$VegaDetails"
  }
}, {
  "$project" : {
    "HedgeLVSVInstruments" : 1,
    "CurvePrefix" : 1,
    "Type" : "$VegaDetails.k",
    "Data" : {
      "$objectToArray" : "$VegaDetails.v"
    }
  }
}, {
  "$unwind" : {
    "path" : "$Data"
  }
}, {
  "$match" : {
    "$or" : [{
        "Type" : "OpeningVega"
      }, {
        "Type" : "VegaImpact"
      }, {
        "Type" : "Vega2ndOrderImpact"
      }]
  }
}, {
  "$project" : {
    "CurvePrefix" : {
      "$concat" : ["$CurvePrefix", "$Data.v.CcyPair", "-", "$HedgeLVSVInstruments", "-"]
    },
    "Type" : 1,
    "CcyPair" : "$Data.v.CcyPair",
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
    "CurvePrefix" : 1,
    "CcyPair" : 1,
    "Tenor" : "$Data.k",
    "Order" : 1,
    "Type" : 1,
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
    "CurvePrefix" : 1,
    "CurveKey" : {
      "$concat" : ["$CurvePrefix", "$Tenor", "-", "$Data.k"]
    },
    "CcyPair" : 1,
    "Type" : 1,
    "Tenor" : 1,
    "Order" : 1,
    "Delta" : "$Data.k",
    "Value" : "$Data.v"
  }
}, {
  "$match" : {
    "$or" : [{
        "Delta" : "10RR"
      }, {
        "Delta" : "10FLY"
      }, {
        "Delta" : "25RR"
      }, {
        "Delta" : "25FLY"
      }, {
        "Delta" : "10C"
      }, {
        "Delta" : "10P"
      }, {
        "Delta" : "25C"
      }, {
        "Delta" : "25P"
      }, {
        "Delta" : "DN"
      }]
  }
}, {
  "$group" : {
    "_id" : {
      "CurvePrefix" : "$CurvePrefix",
      "CcyPair" : "$CcyPair",
      "Type" : "$Type",
      "Tenor" : "$Tenor",
      "Delta" : "$Delta",
      "CurveKey" : "$CurveKey"
    },
    "Order" : {
      "$max" : "$Order"
    },
    "Value" : {
      "$sum" : "$Value"
    }
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
}, {
  "$replaceRoot" : {
    "newRoot" : "$_id"
  }
}, {
  "$unwind" : {
    "path" : "$Data"
  }
}, {
  "$project" : {
    "CurvePrefix" : 1,
    "CcyPair" : 1,
    "Tenor" : 1,
    "Order" : 1,
    "Delta" : 1,
    "Type" : {
      "$cond" : {
        "if" : {
          "$or" : [{
              "$eq" : ["$Data.k", "OpeningCurve"]
            }, {
              "$eq" : ["$Data.k", "CurveMove"]
            }]
        },
        "then" : "$Data.k",
        "else" : "$Type"
      }
    },
    "Value" : "$Data.v"
  }
}, {
  "$group" : {
    "_id" : {
      "CurvePrefix" : "$CurvePrefix",
      "CcyPair" : "$CcyPair",
      "Tenor" : "$Tenor"
    },
    "Order" : {
      "$max" : "$Order"
    },
    "Items" : {
      "$addToSet" : {
        "Name" : {
          "$concat" : ["$Delta", " ", "$Type"]
        },
        "Value" : "$Value"
      }
    }
  }
}, {
  "$project" : {
    "_id" : 1,
    "Order" : 1,
    "tmp" : {
      "$arrayToObject" : {
        "$zip" : {
          "inputs" : ["$Items.Name", "$Items.Value"]
        }
      }
    }
  }
}, {
  "$addFields" : {
    "tmp._id" : "$_id",
    "tmp.Order" : "$Order"
  }
}, {
  "$replaceRoot" : {
    "newRoot" : "$tmp"
  }
}, {
  "$match" : {
    "$and" : [{
        "DN OpeningVega" : {
          "$ne" : null
        }
      }, {
        "DN OpeningVega" : {
          "$ne" : null
        }
      }]
  }
}, {
  "$replaceWith" : {
    "_id" : {
      "CcyPair" : "$_id.CcyPair",
      "Tenor" : "$_id.Tenor"
    },
    "Order" : "$Order",
    "DN OpeningCurve" : "$DN OpeningCurve",
    "10RR OpeningCurve" : "$10RR OpeningCurve",
    "25RR OpeningCurve" : "$25RR OpeningCurve",
    "10FLY OpeningCurve" : "$10FLY OpeningCurve",
    "25FLY OpeningCurve" : "$25FLY OpeningCurve",
    "25P OpeningCurve" : "$25P OpeningCurve",
    "10P OpeningCurve" : "$10P OpeningCurve",
    "10C OpeningCurve" : "$10C OpeningCurve",
    "25C OpeningCurve" : "$25C OpeningCurve",
    "DN CurveMove" : "$DN CurveMove",
    "10RR CurveMove" : "$10RR CurveMove",
    "25RR CurveMove" : "$25RR CurveMove",
    "10FLY CurveMove" : "$10FLY CurveMove",
    "25FLY CurveMove" : "$25FLY CurveMove",
    "25P CurveMove" : "$25P CurveMove",
    "10P CurveMove" : "$10P CurveMove",
    "10C CurveMove" : "$10C CurveMove",
    "25C CurveMove" : "$25C CurveMove",
    "DN OpeningVega" : "$DN OpeningVega",
    "10RR OpeningVega" : "$10RR OpeningVega",
    "25RR OpeningVega" : "$25RR OpeningVega",
    "10FLY OpeningVega" : "$10FLY OpeningVega",
    "25FLY OpeningVega" : "$25FLY OpeningVega",
    "25P OpeningVega" : "$25P OpeningVega",
    "10P OpeningVega" : "$10P OpeningVega",
    "10C OpeningVega" : "$10C OpeningVega",
    "25C OpeningVega" : "$25C OpeningVega",
    "DN VegaImpact" : "$DN VegaImpact",
    "10RR VegaImpact" : "$10RR VegaImpact",
    "25RR VegaImpact" : "$25RR VegaImpact",
    "10FLY VegaImpact" : "$10FLY VegaImpact",
    "25FLY VegaImpact" : "$25FLY VegaImpact",
    "25P VegaImpact" : "$25P VegaImpact",
    "10P VegaImpact" : "$10P VegaImpact",
    "10C VegaImpact" : "$10C VegaImpact",
    "25C VegaImpact" : "$25C VegaImpact",
    "DN Vega2ndOrderImpact" : "$DN Vega2ndOrderImpact",
    "10RR Vega2ndOrderImpact" : "$10RR Vega2ndOrderImpact",
    "25RR Vega2ndOrderImpact" : "$25RR Vega2ndOrderImpact",
    "10FLY Vega2ndOrderImpact" : "$10FLY Vega2ndOrderImpact",
    "25FLY Vega2ndOrderImpact" : "$25FLY Vega2ndOrderImpact",
    "25P Vega2ndOrderImpact" : "$25P Vega2ndOrderImpact",
    "10P Vega2ndOrderImpact" : "$10P Vega2ndOrderImpact",
    "10C Vega2ndOrderImpact" : "$10C Vega2ndOrderImpact",
    "25C Vega2ndOrderImpact" : "$25C Vega2ndOrderImpact"
  }
}, {
  "$sort" : {
    "_id.CcyPair" : 1,
    "Order" : 1
  }
}, {
  "$project" : {
    "Order" : 0
  }
}]
""";

    public const string Script = 
"""
FROM "TestCollection" PIPELINE {
  WHERE
    COB == date( "2025-05-01T00:00:00Z" )
  WHERE
    $VegaDetails.VegaImpact == exists( true )
  PROJECT
    HedgeLVSVInstruments,
    $VegaDetails.OpeningVega,
    $VegaDetails.ClosingVega,
    $VegaDetails.VegaImpact,
    $VegaDetails.Vega2ndOrderImpact,
    concat( dateToString( 
        format: "%Y%m%d", 
        date: COB
      ), "-", toString( OpeningVolRateSetId ), "-", toString( ClosingVolRateSetId ), "-PnL-Vol-" ) AS CurvePrefix
  PROJECT
    HedgeLVSVInstruments,
    CurvePrefix,
    objectToArray( VegaDetails ) AS VegaDetails
  UNWIND VegaDetails
  PROJECT
    HedgeLVSVInstruments,
    CurvePrefix,
    $VegaDetails.k AS Type,
    objectToArray( $VegaDetails.v ) AS Data
  UNWIND Data
  WHERE
    (Type == "OpeningVega"
      OR Type == "VegaImpact"
      OR Type == "Vega2ndOrderImpact")
  PROJECT
    concat( CurvePrefix, $Data.v.CcyPair, "-", HedgeLVSVInstruments, "-" ) AS CurvePrefix,
    Type,
    $Data.v.CcyPair AS CcyPair,
    objectToArray( $Data.v.Data ) AS Data
  UNWIND Data INDEX Order
  PROJECT
    CurvePrefix,
    CcyPair,
    $Data.k AS Tenor,
    Order,
    Type,
    objectToArray( $Data.v ) AS Data
  UNWIND Data
  PROJECT
    CurvePrefix,
    concat( CurvePrefix, Tenor, "-", $Data.k ) AS CurveKey,
    CcyPair,
    Type,
    Tenor,
    Order,
    $Data.k AS Delta,
    $Data.v AS Value
  WHERE
    (Delta == "10RR"
      OR Delta == "10FLY"
      OR Delta == "25RR"
      OR Delta == "25FLY"
      OR Delta == "10C"
      OR Delta == "10P"
      OR Delta == "25C"
      OR Delta == "25P"
      OR Delta == "DN")
  GROUP BY
    CurvePrefix,
    CcyPair,
    Type,
    Tenor,
    Delta,
    CurveKey
    LET
      max( Order ) AS Order,
      sum( Value ) AS Value
  JOIN "PnL-Market" AS Market ON
    $_id.CurveKey == _id
  UNWIND Market
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
  DO 
  {
    "$replaceRoot": {
      "newRoot": "$_id"
    }
  }
  UNWIND Data
  PROJECT
    CurvePrefix,
    CcyPair,
    Tenor,
    Order,
    Delta,
    cond( 
      if: $Data.k == "OpeningCurve"
        OR $Data.k == "CurveMove", 
      then: $Data.k, 
      else: Type
    ) AS Type,
    $Data.v AS Value
  GROUP BY
    CurvePrefix,
    CcyPair,
    Tenor
    LET
      max( Order ) AS Order,
      addToSet( 
        Name: concat( Delta, " ", Type ), 
        Value: Value
      ) AS Items
  PROJECT
    _id,
    Order,
    arrayToObject( zip( 
        inputs: [
            $Items.Name,
            $Items.Value
          ]
      ) ) AS tmp
  ADD
    _id AS "tmp._id",
    Order AS "tmp.Order"
  DO 
  {
    "$replaceRoot": {
      "newRoot": "$tmp"
    }
  }
  WHERE
    'DN OpeningVega' != NULL
    AND 'DN OpeningVega' != NULL
  REPLACE ID {
      $_id.CcyPair AS CcyPair,
      $_id.Tenor AS Tenor
    }
    Order,
    'DN OpeningCurve',
    '10RR OpeningCurve',
    '25RR OpeningCurve',
    '10FLY OpeningCurve',
    '25FLY OpeningCurve',
    '25P OpeningCurve',
    '10P OpeningCurve',
    '10C OpeningCurve',
    '25C OpeningCurve',
    'DN CurveMove',
    '10RR CurveMove',
    '25RR CurveMove',
    '10FLY CurveMove',
    '25FLY CurveMove',
    '25P CurveMove',
    '10P CurveMove',
    '10C CurveMove',
    '25C CurveMove',
    'DN OpeningVega',
    '10RR OpeningVega',
    '25RR OpeningVega',
    '10FLY OpeningVega',
    '25FLY OpeningVega',
    '25P OpeningVega',
    '10P OpeningVega',
    '10C OpeningVega',
    '25C OpeningVega',
    'DN VegaImpact',
    '10RR VegaImpact',
    '25RR VegaImpact',
    '10FLY VegaImpact',
    '25FLY VegaImpact',
    '25P VegaImpact',
    '10P VegaImpact',
    '10C VegaImpact',
    '25C VegaImpact',
    'DN Vega2ndOrderImpact',
    '10RR Vega2ndOrderImpact',
    '25RR Vega2ndOrderImpact',
    '10FLY Vega2ndOrderImpact',
    '25FLY Vega2ndOrderImpact',
    '25P Vega2ndOrderImpact',
    '10P Vega2ndOrderImpact',
    '10C Vega2ndOrderImpact',
    '25C Vega2ndOrderImpact'
  SORT BY
        $_id.CcyPair,
        Order
  PROJECT EXCLUDE
    Order
}

""";

}
