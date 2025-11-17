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
﻿using System.Globalization;

namespace Rms.Risk.Mango.Language.Ast;

public class AstStageBucket : AstStage
{
    private readonly List<string> _buckets = [];
    public           bool         Auto            { get; internal set; }
    public           int          NumberOfBuckets { get; internal set; }
    public           string?      Granularity     { get; internal set; }

    public  IReadOnlyList<AstLet> Fields => Children.OfType<AstLet>().ToList();

    public IReadOnlyList<string> Buckets => _buckets;

    public void AddBucket(string bucket) => _buckets.Add(bucket);
    public void AddBucket(double bucket) => _buckets.Add(bucket.ToString(CultureInfo.InvariantCulture));
    public void AddBucket(long bucket) => _buckets.Add(bucket.ToString());

    public string?       DefaultBucket { get; internal set; }
    public AstExpression GroupBy       { get; internal set; } = null!;

    public override void Append(StringBuilder sb, int indent)
    {
        if (Auto)
            AppendAuto(sb, indent);
        else
            AppendPlain(sb, indent);
    }

    private void AppendPlain(StringBuilder sb, int indent)
    {
        sb.AppendLine($"{Spaces(indent)}BUCKET");

        sb.Append($"{Spaces(indent+1)}");
        GroupBy.Append(sb, indent+1);
        sb.AppendLine();

        sb.Append($"{Spaces(indent+1)}BOUNDARIES ");

        var first = true;
        foreach (var bucket in Buckets)
        {
            if ( first )
                first = false;
            else
                sb.Append(", ");

            if (double.TryParse(bucket, out _))
                sb.Append(bucket);
            else
                sb.Append($"\"{bucket}\"");
        }
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(DefaultBucket))
        {
            sb.Append($"{Spaces(indent+1)}DEFAULT ");

            if (double.TryParse(DefaultBucket, out _))
                sb.Append(DefaultBucket);
            else
                sb.Append($"\"{DefaultBucket}\"");
            sb.AppendLine();
        }

        var fields = Fields;
        if (fields.Count > 0)
        {
            sb.AppendLine($"{Spaces(indent + 1)}LET");
            first = true;
            foreach (var field in Fields)
            {
                if (!first)
                    sb.AppendLine(",");
                else
                    first = false;

                if (field is AstLetExpression let)
                {
                    if ((let.Expression is AstExpressionVariable ev) && string.IsNullOrWhiteSpace(let.Name))
                    {
                        sb.Append($"{Spaces(indent + 2)}");
                        ev.Append(sb, indent + 2);
                    }
                    else
                        let.Append(sb, indent + 2);
                }
                else
                    ((AstNodeBase)field).Append(sb, indent + 2);
            }
            sb.AppendLine();
        }
    }

    private void AppendAuto(StringBuilder sb, int indent)
    {
        sb.AppendLine($"{Spaces(indent)}BUCKET AUTO");

        sb.Append($"{Spaces(indent+1)}");
        GroupBy.Append(sb, indent+1);
        sb.AppendLine();

        sb.AppendLine($"{Spaces(indent+1)}BUCKETS {NumberOfBuckets}");
        if ( !string.IsNullOrWhiteSpace(Granularity) )
            sb.AppendLine($"{Spaces(indent+1)}GRANULARITY \"{Granularity}\"");

        var fields = Fields;
        if (fields.Count > 0)
        {
            sb.AppendLine($"{Spaces(indent + 1)}LET");
            var first = true;
            foreach (var field in Fields)
            {
                if (!first)
                    sb.AppendLine(",");
                else
                    first = false;

                if (field is AstLetExpression let)
                {
                    if ((let.Expression is AstExpressionVariable ev) && string.IsNullOrWhiteSpace(let.Name))
                    {
                        sb.Append($"{Spaces(indent + 2)}");
                        ev.Append(sb, indent + 2);
                    }
                    else
                        let.Append(sb, indent + 2);
                }
                else
                    ((AstNodeBase)field).Append(sb, indent + 2);
            }
            sb.AppendLine();
        }
    }

    public override JsonNode? AsJson()
        => Auto
            ? AsJsonAuto()
            : AsJsonPlain()
            ;

    public JsonNode? AsJsonPlain()
    {
        var body = new JsonObject
        {
            ["groupBy"] = GroupBy.AsJson()
        };

        var bounds = new JsonArray();
        foreach (var bucket in Buckets)
        {
            if (long.TryParse(bucket, out var l))
                bounds.Add(l);
            else if (double.TryParse(bucket, out var d))
                bounds.Add(d);
            else
                bounds.Add(bucket);
            
        }
        body["boundaries"] = bounds;

        if (!string.IsNullOrWhiteSpace(DefaultBucket))
        {
            if (long.TryParse(DefaultBucket, out var l))
                body["default"] = JsonValue.Create(l);
            else if (double.TryParse(DefaultBucket, out var d))
                body["default"] = JsonValue.Create(d);
            else
                body["default"] = JsonValue.Create(DefaultBucket);

        }

        var fields = Fields;
        if (fields.Count > 0)
        {
            var let = new JsonObject();

            foreach (var field in Fields)
            {
                field.AddToJson(let);
            }

            body["output"] = let;
        }

        var stage = new JsonObject([ new("$bucket", body)]);
        return ApplyOptions(stage);
    }

    public JsonNode? AsJsonAuto()
    {
        var body = new JsonObject
        {
            ["groupBy"] = GroupBy.AsJson(),
            ["buckets"] = NumberOfBuckets
        };

        if ( !string.IsNullOrWhiteSpace(Granularity) )
            body["granularity"] = Granularity;

        var fields = Fields;
        if (fields.Count > 0)
        {
            var let = new JsonObject();

            foreach (var field in Fields)
            {
                field.AddToJson(let);
            }

            body["output"] = let;
        }

        var stage = new JsonObject([ new("$bucketAuto", body)]);
        return ApplyOptions(stage);
    }
}