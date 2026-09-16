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
namespace Tests.Rms.Risk.Mango;

[TestFixture]
public class RstToMarkdownConverterTests
{
    private RstToMarkdownConverter _converter = null!;

    [SetUp]
    public void SetUp()
    {
        _converter = new();
    }

    [Test]
    public void Convert_EmptyInput_ReturnsEmptyString()
    {
        // Arrange  
        var lines = new string[0];

        // Act  
        var result = _converter.Convert(lines);

        // Assert  
        Assert.AreEqual(string.Empty, result);
    }

    [Test]
    public void Convert_BoldText_ReturnsMarkdownBold()
    {
        // Arrange  
        var lines = new[] { "**bold text**" };

        // Act  
        var result = _converter.Convert(lines);

        // Assert  
        Assert.AreEqual("**bold text**", result);
    }

    [Test]
    public void Convert_ItalicText_ReturnsMarkdownItalic()
    {
        // Arrange  
        var lines = new[] { "*italic text*" };

        // Act  
        var result = _converter.Convert(lines);

        // Assert  
        Assert.AreEqual("_italic text_", result);
    }

    [Test]
    public void Convert_Heading_ReturnsMarkdownHeading()
    {
        // Arrange  
        var lines = new[] { "Heading", "======" };

        // Act  
        var result = _converter.Convert(lines);

        // Assert  
        Assert.AreEqual("# Heading", result);
    }

    [Test]
    public void Convert_CodeBlock_ReturnsMarkdownCodeBlock()
    {
        // Arrange  
        var lines = new[] { ".. code-block:: csharp", "  Console.WriteLine(\"Hello World\");" };

        // Act  
        var result = _converter.Convert(lines).Replace("\r", "");

        // Assert  
        Assert.AreEqual("```csharp\n  Console.WriteLine(\"Hello World\");\n```", result);
    }

    [Test]
    public void Convert_UnknownTags_SkipsUnknownTags()
    {
        // Arrange  
        var lines = new[] { ".. include:: file.rst" };

        // Act  
        var result = _converter.Convert(lines);

        // Assert  
        Assert.AreEqual(string.Empty, result);
    }

    [Test]
    public void Convert_MixedContent_ConvertsCorrectly()
    {
        // Arrange  
        var lines = new[]
        {
            "Heading",
            "======",
            "**bold text**",
            "*italic text*",
            ".. code-block:: csharp",
            "  Console.WriteLine(\"Hello World\");",
            ".. include:: file.rst"
        };

        // Act  
        var result = _converter.Convert(lines).Replace("\r", "");

        // Assert  
        var expected = "# Heading\n**bold text**\n_italic text_\n```csharp\n  Console.WriteLine(\"Hello World\");\n```\n";
        Assert.AreEqual(expected, result);
    }
}