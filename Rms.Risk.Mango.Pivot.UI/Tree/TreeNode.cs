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
using Microsoft.AspNetCore.Components;

namespace Rms.Risk.Mango.Pivot.UI.Tree;

internal static class TreeNodeCounter
{
    private static int _count;

    public static int GetNew() => Interlocked.Increment(ref _count);
}

public class TreeNode<TItem>
{

    public string                Label      { get; set; } = $"New Node {TreeNodeCounter.GetNew()}";
    public TItem?                Data       { get; set; }
    public TreeNode<TItem>?      Parent     { get; set; }
    public List<TreeNode<TItem>> Children   { get; set; } = [];
    public bool                  IsExpanded { get; set; }

    public RenderFragment<TreeNode<TItem>>? LabelFragment { get; set; }
    public RenderFragment<TreeNode<TItem>>? ContentsFragment { get; set; }

    public override string ToString() => Label;
}