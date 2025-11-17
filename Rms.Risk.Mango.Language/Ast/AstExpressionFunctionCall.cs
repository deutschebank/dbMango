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

public class AstExpressionFunctionCall : AstExpression
{
    public const string WithinFunctionProperty = "within_function";

    public AstExpressionFunctionCall(string name, IEnumerable<AstFunctionArgument> namedArgs)
    {
        SetProperty(WithinFunctionProperty, true);
        Name = PreprocessFieldName(name);
        foreach (var arg in namedArgs)
            Add(arg);
    }

    public string Name { get;  internal set{ field = PreprocessFieldName(value); } }
    public IReadOnlyList<AstFunctionArgument> UnnamedArgs => Children.OfType<AstFunctionArgument>().Where(x => string.IsNullOrEmpty(x.Name)).ToList();
    public IReadOnlyList<AstFunctionArgument> NamedArgs => Children.OfType<AstFunctionArgument>().Where(x => !string.IsNullOrEmpty(x.Name)).ToList();

    public override void Append(StringBuilder sb, int indent)
    {
        sb.Append($"{Name}( ");
        var namedArgs = NamedArgs;
        if ( namedArgs.Count == 0 )
        {
            // unnamed args
            bool first = true;
            foreach (var value in UnnamedArgs)
            {
                if ( !first )
                    sb.Append(", ");
                else
                    first = false;
                    
                value.Append(sb, indent+1);
            }
            sb.Append(" )");
        }
        else
        {
            // named args
            sb.AppendLine();
            bool first = true;
            foreach (var value in namedArgs)
            {
                if ( !first )
                {
                    sb.AppendLine(", ");
                }
                else
                    first = false;

                sb.Append(Spaces(indent + 1));
                value.Append(sb, indent + 2);
            }
            sb.AppendLine();
            sb.Append($"{Spaces(indent)})");
        }
    }

    public override JsonNode? AsJson()
    {
        var namedArgs = NamedArgs;
        var unnamedArgs = UnnamedArgs;

        if (namedArgs.Count == 0)
        {
            // no need for array if only one parameter
            if ( unnamedArgs.Count == 1 )
                return new JsonObject(
                    [
                        new($"${Name}", unnamedArgs[0].AsJson())
                    ]);
            return new JsonObject(
                [
                    new(
                        $"${Name}",
                        new JsonArray([.. unnamedArgs.Select(x => x.AsJson())]))
                    ]);
        }

        return new JsonObject(
            [
                new(
                    $"${Name}", 
                    new JsonObject([.. namedArgs.Select(x => new KeyValuePair<string, JsonNode?>(x.Name, x.Value.AsJson()))])
                )
            ]);
    }
}
