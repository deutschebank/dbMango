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

public class AstPipeline : AstNodeBase
{
    public static readonly AstPipeline None = new();

    public AstPipeline()
    {
    }

    public AstPipeline(IEnumerable<AstStage> stages)
    {
        foreach (var stage in stages)
            Add(stage);
    }

    public IReadOnlyList<AstStage> Stages => Children.OfType<AstStage>().ToList();

    public override void Append(StringBuilder sb, int indent)
    {
        foreach (var stage in Stages)
            stage.Append(sb, indent);
    }

    public override JsonNode? AsJson() => new JsonArray([.. Stages.Select(x => x.AsJson())]);
}
