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
﻿namespace Tests.Rms.Risk.Mango;

[TestFixture]
public class RstToMarkdownTableTests
{
    private RstToMarkdownConverter _converter = null!;

    [SetUp]
    public void SetUp()
    {
        _converter = new();
    }

    [Test]
    public void Convert_TableToMarkdownList_SimpleTable()
    {
        // Arrange
        var rstContent = new[]
        {
            "table starts here",
            ".. list-table::",
            "   * - Item 1",
            "     - SubItem 2",
            "   * - Item 3",
            "     - SubItem 4",
            "table ends here"
        };

        const string expectedMarkdown = """
            table starts here
            - Item 1
              - SubItem 2
            - Item 3
              - SubItem 4
            table ends here
            """;

        // Act
        var result = _converter.Convert(rstContent);

        // Assert
        Assert.AreEqual(expectedMarkdown, result);
    }

    [Test]
    public void Convert_TableToMarkdownList_NestedTable()
    {
        // Arrange
        var rstContent = new[]
        {
            "table starts here",
            ".. list-table::",
            "   * - Parent Item 1",
            "     - Child Item 1.1",
            "       - Child Item 1.1.1",
            "   * - Parent Item 2",
            "     - Child Item 2.1",
            "table ends here"
        };

        const string expectedMarkdown = """
            table starts here
            - Parent Item 1
              - Child Item 1.1
                - Child Item 1.1.1
            - Parent Item 2
              - Child Item 2.1
            table ends here
            """;

        // Act
        var result = _converter.Convert(rstContent);

        // Assert
        Assert.AreEqual(expectedMarkdown, result);
    }

    [Test]
    public void Convert_TableToMarkdownList_EmptyTable()
    {
        // Arrange
        var rstContent = new[]
        {
            ".. list-table::"
        };

        var expectedMarkdown = string.Empty;

        // Act
        var result = _converter.Convert(rstContent);

        // Assert
        Assert.AreEqual(expectedMarkdown, result);
    }

    [Test]
    public void LinesBeforeTheFirstRow()
    {
        var rstContent = """
            table starts here
            .. list-table::
               :header-rows: 1
               :widths: 20 80
            
               * - Item 1
                 - Item 1.1
            table end here
            """.Replace("\r", "").Split('\n');

        var expectedMarkdown = """
            table starts here
            - Item 1
              - Item 1.1
            table end here
            """;

        // Act
        var result = _converter.Convert(rstContent);

        // Assert
        Assert.AreEqual(expectedMarkdown, result);

    }

    [Test]
    public void LineContinuation()
    {
        var rstContent = """
                         table starts here
                         .. list-table::
                            * - Item 1
                              - Item 1.1
                                continues
                         table end here
                         """.Replace("\r", "").Split('\n');

        var expectedMarkdown = """
                               table starts here
                               - Item 1
                                 - Item 1.1
                                   continues
                               table end here
                               """;

        // Act
        var result = _converter.Convert(rstContent);

        // Assert
        Assert.AreEqual(expectedMarkdown, result);

    }
}