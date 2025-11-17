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
using System.Text.RegularExpressions;

namespace Rms.Risk.Mango.Language.Ast;

public abstract partial class AstNodeBase
{
    private readonly Lazy<List<AstNodeBase>>          _children   = new();
    private readonly Lazy<Dictionary<string, object>> _properties = new();
    
    public bool                       Empty    => Count == 0;
    public int                        Count    => _children.IsValueCreated ? _children.Value.Count : 0;
    public IReadOnlyList<AstNodeBase> Children => _children.Value;
    public AstNodeBase?               Parent   { get; set; } = null;


    public void Add(AstNodeBase child)
    {
        _children.Value.Add(child);
        child.Parent = this;
    }

    public void SetProperty(string name, object value) => _properties.Value[name] = value;

    public T? GetProperty<T>(string name, T? defaultValue = default(T), bool checkParent = true )
    {
        if (_properties.IsValueCreated && _properties.Value.TryGetValue(name, out var v))
            return (T)v;

        if (Parent == null || !checkParent)
            return defaultValue;

        return Parent.GetProperty<T>(name);
    }

    public override string ToString() => AsText();

    public string AsText()
    {
        var sb = new StringBuilder();
        Append(sb, 0);
        return sb.ToString();
    }

    public abstract void Append(StringBuilder sb, int indent);
    public abstract JsonNode? AsJson();

    protected static string Spaces(int indent)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < indent; i++)
            sb.Append("  ");
        return sb.ToString();
    }

    [GeneratedRegex("^[a-zA-Z_][a-zA-Z0-9_]+$")]
    private static partial Regex SimpleVarNameRegex();

    [GeneratedRegex("^[a-zA-Z_][a-zA-Z0-9_.]+$")]
    private static partial Regex ComplexVarNameRegex();

    protected static string PreprocessFieldName(string? name)
        => name?.Trim('\"').Trim('\'').TrimStart('$') ?? "";

    protected static void AppendField(StringBuilder sb, string name)
    {
        if ( SimpleVarNameRegex().IsMatch(name) )
            sb.Append(name);
        else if ( ComplexVarNameRegex().IsMatch(name) )
            sb.Append($"${name}");    
        else
            sb.Append($"\'{name}'");    
    }

    protected static void AppendFieldName(StringBuilder sb, string name)
    {
        if ( SimpleVarNameRegex().IsMatch(name) )
            sb.Append(name);
        else
            sb.Append($"\"{name}\"");
    }

}
