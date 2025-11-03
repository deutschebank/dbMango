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
namespace Rms.Risk.Mango.Language.Ast;

public class AstLetArray : AstLet
{
    public AstLetArray()
    {
    }

    public AstLetArray(string? name, IEnumerable<AstLet> fields, bool isArray)
    {
        Name    = name?.Trim('\"');//PreprocessFieldName(name);
        IsArray = isArray;

        foreach (var field in fields)
            Add(field);
    }

    public bool IsArray { get; internal set; }

    public IReadOnlyList<AstLet> Fields => Children.OfType<AstLet>().ToList();

    public override void Append(StringBuilder sb, int indent)
    {
        var openingBracket = IsArray ? "[" : "{";
        var closingBracket = IsArray ? "]" : "}";

        sb.AppendLine($"{Spaces(indent)}{openingBracket}");
        var first = true;
        foreach (var field in Fields)
        {
            if ( !first )
                sb.AppendLine(",");
            else
                first = false;

            field.Append(sb, indent + 1);
        }
        sb.AppendLine();

        if ( !string.IsNullOrWhiteSpace(Name) )
        {
            sb.Append($"{Spaces(indent)}{closingBracket} AS ");
            AppendFieldName(sb, Name);
        }
        else
        {
            sb.Append($"{Spaces(indent)}{closingBracket}");
        }
    }

    public override void AddToJson(JsonObject res, bool simplifyTargetNames = false)
    {
        if ( string.IsNullOrWhiteSpace(Name))
            throw new ("Array name must be specified");

        var body = new JsonArray();
        foreach (var field in Fields)
        {
            field.AddToJson(body, simplifyTargetNames);
        }

        res.Add(Name!, body);
    }

    public override void AddToJson(JsonArray res, bool simplifyTargetNames = false)
    {
        if (IsArray)
        {
            var body = new JsonArray();

            if (!string.IsNullOrWhiteSpace(Name))
            {
                foreach (var field in Fields)
                {
                    field.AddToJson(body, simplifyTargetNames);
                }

                res.Add(new JsonObject([new(Name, body)]));
                return;
            }

            foreach (var field in Fields)
            {
                field.AddToJson(body, simplifyTargetNames);
            }
            res.Add(body);
        }
        else
        {
            // this is an object
            var body = new JsonObject();

            if (!string.IsNullOrWhiteSpace(Name))
            {
                foreach (var field in Fields)
                {
                    field.AddToJson(body, simplifyTargetNames);
                }

                res.Add(new JsonObject([new(Name, body)]));
                return;
            }

            foreach (var field in Fields)
            {
                field.AddToJson(body, simplifyTargetNames);
            }
            res.Add(body);
        }
    }

}
