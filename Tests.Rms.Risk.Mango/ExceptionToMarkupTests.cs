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
﻿using Rms.Risk.Mango.Pivot.UI.Services;
using System.Text;

namespace Tests.Rms.Risk.Mango;

[TestFixture]
public class ExceptionToMarkupTests
{

    [Test] public void Real()
    {
        try
        {
            throw new("Test");
        }
        catch ( Exception ex) 
        {
            //### Test
            //#### System.Exception
            //* *Tests.Rms.Risk.Mango.ExceptionToMarkupTests*.`Simple`*()* `XXX\dbMango\Tests.Rms.Risk.Mango\ExceptionToMarkupTests.cs` 13
                
            var s = ex.ToMarkdown();
            Assert.Multiple(() =>
            {
                Assert.IsTrue(s.Contains("### Test"));
                Assert.IsTrue(s.Contains("#### System.Exception"));
                Assert.IsTrue(s.Contains("* *Tests.Rms.Risk.Mango.ExceptionToMarkupTests*.`Real`*()* `"));
                Assert.IsTrue(s.Contains("ExceptionToMarkupTests.cs` "));
            });
        }
    }

    [Test] public void WithSource()
    {
        const string test=@"   at Tests.Rms.Risk.Mango.ExceptionToMarkupTests.Real() in xxx\dbMango\Tests.Rms.Risk.Mango\ExceptionToMarkupTests.cs:line 13";
        const string expected = @"* *Tests.Rms.Risk.Mango.ExceptionToMarkupTests*.`Real`*()* `xxx\dbMango\Tests.Rms.Risk.Mango\ExceptionToMarkupTests.cs` 13";
        
        var sb = new StringBuilder();
        ExceptionHelpers.AppendStackLine(test, sb);
        var s = sb.ToString().Replace("\r", "").Replace("\n", "");
        Assert.AreEqual(expected, s);
    }

    [Test] public void NoSource()
    {
        const string test=@"   at Tests.Rms.Risk.Mango.ExceptionToMarkupTests.Real()";
        const string expected = @"* *Tests.Rms.Risk.Mango.ExceptionToMarkupTests.Real()*";
        
        var sb = new StringBuilder();
        ExceptionHelpers.AppendStackLine(test, sb);
        var s = sb.ToString().Replace("\r", "").Replace("\n", "");
        Assert.AreEqual(expected, s);
    }
}
