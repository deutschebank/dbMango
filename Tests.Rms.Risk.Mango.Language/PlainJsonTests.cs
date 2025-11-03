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
﻿namespace Tests.Rms.Risk.Mango.Language;

[TestFixture]
public class PlainJsonTests
{
    [Test]
    public void JsonQuotes()
    {
        const string text = 
    """
    {
        "$bucket": "list"
    }
    """;

        LanguageParser.ParseJsonForFun(text);
    }

    [Test]
    public void JsonManyValues()
    {
        const string text = 
    """
    {
          "bucket": "list",
          "never" : "gonna",
          "happen" : "again"
    }
    """;

        LanguageParser.ParseJsonForFun(text);
    }

    [Test]
    public void JsonValueBool()
    {
        const string text = 
    """
    {
        "bucket": true
    }
    """;

        LanguageParser.ParseJsonForFun(text);
    }

    [Test]
    public void JsonValueNull()
    {
        const string text = 
    """
    {
          "bucket": null
    }
    """;

        LanguageParser.ParseJsonForFun(text);
    }

    [Test]
    public void JsonValueEmptyObject()
    {
        const string text = 
    """
    {
          "bucket": {}
    }
    """;

        LanguageParser.ParseJsonForFun(text);
    }


    [Test]
    public void JsonValueTypes()
    {
        const string text = 
    """
    {
          "bucket": "list",
          "never" : 1,
          "happen" : 1.234,
          "again" : true,
          "array": [1,2,3],
          "blah": null,
          "object" : {
             "a": 1,
             "b": "2",
             "c": {}
          }
    }
    """;

        LanguageParser.ParseJsonForFun(text);
    }

    [Test]
    public void JsonNoQuotes()
    {
        const string text = 
    """
    {
          bucket: "list"
    }
    """;

        LanguageParser.ParseJsonForFun(text);
    }

    [Test]
    public void JsonDollarId()
    {
        const string text = 
    """
    {
          $bucket: "list"
    }
    """;

        LanguageParser.ParseJsonForFun(text);
    }
}
