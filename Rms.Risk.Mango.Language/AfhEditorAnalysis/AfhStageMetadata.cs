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
namespace Rms.Risk.Mango.Language.AfhEditorAnalysis;

/// <summary>
/// Metadata for an AFH stage: keyword, syntax, completion template, and valid continuations.
/// </summary>
public class AfhStageMetadata
{
    public string Keyword { get; set; } = "";
    public string SyntaxPreview { get; set; } = "";
    public string CompletionTemplate { get; set; } = "";
    public string[] ValidContinuations { get; set; } = [];
    public string Description { get; set; } = "";
}

/// <summary>
/// Catalog of all supported AFH stages with metadata for intellisense.
/// </summary>
public static class AfhStageCatalog
{
    public static readonly Dictionary<string, AfhStageMetadata> Stages = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "WHERE", new AfhStageMetadata
            {
                Keyword = "WHERE",
                SyntaxPreview = "WHERE <expression>",
                CompletionTemplate = "WHERE ",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Filter documents based on an expression"
            }
        },
        {
            "ADD", new AfhStageMetadata
            {
                Keyword = "ADD",
                SyntaxPreview = "ADD <field> [AS <alias>], ...",
                CompletionTemplate = "ADD\n  ",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Add new fields to documents"
            }
        },
        {
            "PROJECT", new AfhStageMetadata
            {
                Keyword = "PROJECT",
                SyntaxPreview = "PROJECT [ID { ... }] <field>, ... | PROJECT EXCLUDE <field>, ...",
                CompletionTemplate = "PROJECT ",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Include or exclude fields from documents"
            }
        },
        {
            "GROUP", new AfhStageMetadata
            {
                Keyword = "GROUP",
                SyntaxPreview = "GROUP BY <field>, ... [LET <aggregate>, ...]",
                CompletionTemplate = "GROUP BY ",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Group documents by fields and calculate aggregates"
            }
        },
        {
            "SORT", new AfhStageMetadata
            {
                Keyword = "SORT",
                SyntaxPreview = "SORT BY <field> [ASC|DESC], ...",
                CompletionTemplate = "SORT BY ",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Sort documents by fields"
            }
        },
        {
            "BUCKET", new AfhStageMetadata
            {
                Keyword = "BUCKET",
                SyntaxPreview = "BUCKET <expr> BOUNDARIES <num>, ... | BUCKET AUTO <expr> BUCKETS <num>",
                CompletionTemplate = "BUCKET ",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Group documents into buckets based on an expression"
            }
        },
        {
            "FACET", new AfhStageMetadata
            {
                Keyword = "FACET",
                SyntaxPreview = "FACET <name> PIPELINE { ... }, ...",
                CompletionTemplate = "FACET ",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Create multiple pipelines within a single stage"
            }
        },
        {
            "JOIN", new AfhStageMetadata
            {
                Keyword = "JOIN",
                SyntaxPreview = "JOIN \"<collection>\" AS <alias> ON <field> == <field> [LET ...] [PIPELINE {...}]",
                CompletionTemplate = "JOIN \"",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Join documents with another collection"
            }
        },
        {
            "UNWIND", new AfhStageMetadata
            {
                Keyword = "UNWIND",
                SyntaxPreview = "UNWIND <field> [INDEX <var>]",
                CompletionTemplate = "UNWIND ",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Unwind an array field into separate documents"
            }
        },
        {
            "REPLACE", new AfhStageMetadata
            {
                Keyword = "REPLACE",
                SyntaxPreview = "REPLACE ID { ... } <field>, ...",
                CompletionTemplate = "REPLACE ID { ",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Replace the root document"
            }
        },
        {
            "DO", new AfhStageMetadata
            {
                Keyword = "DO",
                SyntaxPreview = "DO { <raw JSON> }",
                CompletionTemplate = "DO { ",
                ValidContinuations = ["ADD", "BUCKET", "FACET", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"],
                Description = "Embed raw MongoDB aggregation JSON"
            }
        }
    };

    /// <summary>
    /// Get all valid stage keywords for root pipeline and nested pipelines.
    /// </summary>
    public static IEnumerable<string> AllStageKeywords => Stages.Keys;

    /// <summary>
    /// Get the metadata for a stage by keyword.
    /// </summary>
    public static AfhStageMetadata? GetStage(string keyword)
    {
        return Stages.TryGetValue(keyword, out var meta) ? meta : null;
    }
}
