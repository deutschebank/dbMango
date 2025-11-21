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
public class DoStageTests
{
    [Test]
    public void JsonQuotes()
    {
        const string text = 
    """
    FROM "CollectionName"
    PIPELINE {
        DO {
          "$bucket": "list"
        }
    }
    """;

        var ast = LanguageParser.ParseScriptToAST(text);
        Assert.That( ast, Is.Not.Null );
    }

    [Test]
    public void JsonManyValues()
    {
        const string text = 
    """
    FROM "CollectionName"
    PIPELINE {
        DO {
          "bucket": "list",
          "never" : "gonna",
          "happen" : "again"
        }
    }
    """;

        var ast = LanguageParser.ParseScriptToAST(text);
        Assert.That( ast, Is.Not.Null );
    }

    [Test]
    public void JsonValueBool()
    {
        const string text = 
    """
    FROM "CollectionName"
    PIPELINE {
        DO {
          "bucket": true
        }
    }
    """;

        var ast = LanguageParser.ParseScriptToAST(text);
        Assert.That( ast, Is.Not.Null );
    }

    [Test]
    public void JsonValueNull()
    {
        const string text = 
    """
    FROM "CollectionName"
    PIPELINE {
        DO {
          "bucket": null
        }
    }
    """;

        var ast = LanguageParser.ParseScriptToAST(text);
        Assert.That( ast, Is.Not.Null );
    }

    [Test]
    public void JsonValueEmptyObject()
    {
        const string text = 
    """
    FROM "CollectionName"
    PIPELINE {
        DO {
          "bucket": {}
        }
    }
    """;

        var ast = LanguageParser.ParseScriptToAST(text);
        Assert.That( ast, Is.Not.Null );
    }


    [Test]
    public void JsonValueTypes()
    {
        const string text = 
    """
    FROM "CollectionName"
    PIPELINE {
        DO {
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
    }
    """;

        var ast = LanguageParser.ParseScriptToAST(text);
        Assert.That( ast, Is.Not.Null );
    }

    [Test]
    public void JsonNoQuotes()
    {
        const string text = 
    """
    FROM "CollectionName"
    PIPELINE {
        DO {
          bucket: "list"
        }
    }
    """;

        var ast = LanguageParser.ParseScriptToAST(text);
        Assert.That( ast, Is.Not.Null );
    }

    [Test]
    public void JsonDollarId()
    {
        const string text = 
    """
    FROM "CollectionName"
    PIPELINE {
        DO {
          $bucket: "list"
        }
    }
    """;

        var ast = LanguageParser.ParseScriptToAST(text);
        Assert.That( ast, Is.Not.Null );
    }

    public static JsonNode ParseJson(string jsonString)
    {
        try
        {
            return JsonNode.Parse(jsonString)!;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON string: {ex.Message}", ex);
        }
    }

    public static string ToJson(JsonNode jsonNode)
    {
        return JsonSerializer.Serialize(jsonNode);
    }

    private const string Template = 
    """
    FROM "CollectionName"
    PIPELINE {
        DO [JSON]
    }
    """;

    private static void TestJson(string json)
    {
        var src = Template.Replace("[JSON]", json);
        var expected = ToJson(ParseJson(json)); // normalize json

        var ast = LanguageParser.ParseScriptToAST(src);
        Assert.Multiple( () =>
        {
            Assert.That( ast, Is.Not.Null );
            Assert.That( ast.Pipeline, Is.Not.Null );
            Assert.That( ast.Pipeline!.Children.OfType<AstStageDo>().Any(), Is.True );
            var stage = ast.Pipeline.Children.OfType<AstStageDo>().First();
            Assert.That( stage.Json, Is.Not.Null );
            var result = ToJson(stage.Json!);
            Assert.That( result, Is.EqualTo( expected ));
        });
    }

    [Test]
    public void Object()
    {
        TestJson("{ \"bucket\" : \"list\" }");
    }
    [Test]
    public void ComplexObject()
    {
        const string json =
        """
        {
          "bucket": "list",
          "never" : "gonna",
          "happen" : "again",
          "array": [1,2,3],
          "blah": null,
          "object" : {
             "a": 1,
             "b": "2",
             "c": {}
            }
        }
        """;    
        TestJson(json);
    }    
}
