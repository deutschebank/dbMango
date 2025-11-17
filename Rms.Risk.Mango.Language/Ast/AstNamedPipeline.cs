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

public class AstNamedPipeline : AstNodeBase
{
    public AstNamedPipeline(string name, AstPipeline pipeline)
    {
        Name = name;
        Add(pipeline);
    }

    public string       Name     { get; internal set => field = PreprocessFieldName(value); } = "";
    public AstPipeline? Pipeline => Children.OfType<AstPipeline>().FirstOrDefault();

    public override void Append(StringBuilder sb, int indent)
    {
        if (Pipeline == null)
            throw new($"Pipeline is mandatory: {Name}");

        sb.Append($"{Spaces(indent)}");
        AppendField(sb, Name);
        sb.AppendLine(" PIPELINE {");
        Pipeline.Append(sb, indent + 1);
        sb.Append($"{Spaces(indent)}}}");
    }

    public override JsonNode? AsJson()
        => new JsonObject() { new(Name, Pipeline?.AsJson() ?? throw new($"Pipeline is mandatory: {Name}")) };
}