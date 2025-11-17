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
﻿using global::Rms.Risk.Mango.Pivot.UI.Pivot;

namespace Tests.Rms.Risk.Mango;

[TestFixture]
public class PivotKeysComponentTests
{
    private static PivotKeysComponent.ItemWithSelection Make(string text) =>
        new() { Text = text, IsSelected = false, Changed = (_, _) => { } };

    [Test]
    public void ReorderItemsUnsafe_EmptyFiltered_ReturnsCopySameOrder()
    {
        var items = new List<PivotKeysComponent.ItemWithSelection> { Make("A"), Make("B"), Make("C") };

        var result = PivotKeysComponent.ReorderItemsUnsafe(items, Array.Empty<PivotKeysComponent.ItemWithSelection>());

        Assert.That(result, Is.SameAs(items));
    }

    [Test]
    public void ReorderItemsUnsafe_SubsetSameOrder_ReturnsOriginalOrder()
    {
        var a = Make("A"); var b = Make("B"); var c = Make("C"); var d = Make("D"); var e = Make("E");
        var items    = new List<PivotKeysComponent.ItemWithSelection> { a, b, c, d, e };
        var filtered = new List<PivotKeysComponent.ItemWithSelection> { b, d };

        var result = PivotKeysComponent.ReorderItemsUnsafe(items, filtered);

        Assert.That(result.Select(x => x.Text), Is.EqualTo(new[] { "A", "B", "C", "D", "E" }));
    }

    [Test]
    public void ReorderItemsUnsafe_ReverseContiguousSubset_ReordersOnlySubset()
    {
        var a = Make("A"); var b = Make("B"); var c = Make("C"); var d = Make("D"); var e = Make("E"); var f = Make("F");
        var items    = new List<PivotKeysComponent.ItemWithSelection> { a, b, c, d, e, f };
        var filtered = new List<PivotKeysComponent.ItemWithSelection> { d, c, b }; // reverse B,C,D

        var result = PivotKeysComponent.ReorderItemsUnsafe(items, filtered);

        Assert.That(result.Select(x => x.Text), Is.EqualTo(new[] { "A", "D", "C", "B", "E", "F" }));
        Assert.That(IsSubsequence(result.Select(x => x.Text), new[] { "A", "E", "F" }), Is.True);
    }

    [Test]
    public void ReorderItemsUnsafe_MoveNonContiguousItems_PreservesOthersOrder()
    {
        var a = Make("A"); var b = Make("B"); var c = Make("C"); var d = Make("D");
        var e = Make("E"); var f = Make("F"); var g = Make("G"); var h = Make("H");

        var items    = new List<PivotKeysComponent.ItemWithSelection> { a, b, c, d, e, f, g, h };
        var filtered = new List<PivotKeysComponent.ItemWithSelection> { h, b, f }; // new requested order

        var result = PivotKeysComponent.ReorderItemsUnsafe(items, filtered);

        Assert.Multiple(() =>
        {
            Assert.That(result.Where(x => filtered.Contains(x)).Select(x => x.Text), Is.EqualTo(new[] { "H", "B", "F" }));
            Assert.That(IsSubsequence(result.Select(x => x.Text), new[] { "A", "C", "D", "E", "G" }), Is.True);
            Assert.That(result.Select(x => x.Text).OrderBy(x => x), Is.EqualTo(items.Select(x => x.Text).OrderBy(x => x)));
        });
    }

    [Test]
    public void ReorderItemsUnsafe_ItemMovedBackward_PreservesOthersOrder()
    {
        var a = Make("A"); var b = Make("B"); var c = Make("C"); var d = Make("D"); var e = Make("E");
        var items    = new List<PivotKeysComponent.ItemWithSelection> { a, b, c, d, e };
        var filtered = new List<PivotKeysComponent.ItemWithSelection> { d, b }; // d before b (b originally before d)

        var result = PivotKeysComponent.ReorderItemsUnsafe(items, filtered);

        Assert.That(result.Select(x => x.Text), Is.EqualTo(new[] { "A", "D", "B", "C", "E" }));
        Assert.That(IsSubsequence(result.Select(x => x.Text), new[] { "A", "C", "E" }), Is.True);
    }

    [Test]
    public void ReorderItemsUnsafe_InvalidSubset_Throws()
    {
        var a = Make("A"); var b = Make("B");
        var items = new List<PivotKeysComponent.ItemWithSelection> { a, b };
        var outside = Make("X");

        Assert.That(() => PivotKeysComponent.ReorderItemsUnsafe(items, new[] { outside }),
            Throws.TypeOf<ArgumentException>());
    }

    private static bool IsSubsequence(IEnumerable<string> sequence, IEnumerable<string> subseq)
    {
        using var iter = sequence.GetEnumerator();
        foreach (var target in subseq)
        {
            var found = false;
            while (iter.MoveNext())
            {
                if (iter.Current == target)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }
}