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
using MongoDB.Bson;
using Rms.Risk.Mango.Pivot.Core.MongoDb;
using static System.Xml.Schema.XmlSchemaInference;

namespace Rms.Risk.Mango.Services;

public static class JsonSchemaExtractor
{
    private sealed class FieldStats
    {
        public int InstanceCount           { get; set; }
        public int ObjectInstanceCount     { get; set; }
        public int ArrayInstanceCount      { get; set; }
        public HashSet<string> Types       { get; } = [];

        public Dictionary<string, FieldStats> Properties { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, int> PropertyPresence  { get; } = new(StringComparer.Ordinal);

        public FieldStats? ItemsStats      { get; set; }
        public bool ArrayItemsAlwaysUnique { get; set; } = true;

        public double? Minimum             { get; set; }
        public double? Maximum             { get; set; }

        public int? MinLength              { get; set; }
        public int? MaxLength              { get; set; }

        public int? MinItems               { get; set; }
        public int? MaxItems               { get; set; }

        public int? MinProperties          { get; set; }
        public int? MaxProperties          { get; set; }

        public bool HasNonScalarValues     { get; set; }
        public bool HasDateTimeValues      { get; set; }
        public StringHeuristics StringHints { get; set; } = new();

        private bool _enumOverflow;
        private readonly Dictionary<string, BsonValue> _enumValues = new(StringComparer.Ordinal);

        public IReadOnlyCollection<BsonValue> EnumValues => _enumValues.Values;
        public bool CanUseEnum => !_enumOverflow && _enumValues.Count > 0;

        public void AddEnumValue(BsonValue value, int enumLimit)
        {
            if (_enumOverflow)
            {
                return;
            }

            var key = value.ToJson();
            _enumValues.TryAdd(key, value);

            if (_enumValues.Count > enumLimit)
            {
                _enumOverflow = true;
                _enumValues.Clear();
            }
        }
    }

    public static async Task<BsonDocument> InferJsonSchema(
        IMongoDbService<BsonDocument> mongoService,
        int sampleSize = 1000,
        int enumLimit = 20,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(mongoService);

        if (sampleSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleSize), sampleSize, "Sample size must be greater than zero.");
        }

        if (enumLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(enumLimit), enumLimit, "Enum limit must be greater than zero.");
        }

        using var timeoutCts = token == CancellationToken.None 
            ? new CancellationTokenSource(TimeSpan.FromSeconds(30)) 
            : null
            ;

        var effectiveToken = timeoutCts?.Token ?? token;

        var samplePipeline = $"[{{ \"$sample\": {{ \"size\": {sampleSize} }} }}]";
        var rootStats = new FieldStats();

        await foreach (var document in mongoService.AggregateAsyncRaw(samplePipeline, sampleSize, effectiveToken))
        {
            AnalyzeValue(document, rootStats, enumLimit);
        }

        var collectionName = mongoService.CollectionName;
        var schema = new BsonDocument
        {
            ["$schema"    ] = "https://json-schema.org/draft/2020-12/schema",
            ["$id"        ] = $"https://example.com/{collectionName}.schema.json",
            ["title"      ] = $"Schema for {collectionName}",
            ["description"] = $"Inferred schema for MongoDB collection '{collectionName}'."
        };

        var rootBody = BuildSchema(rootStats);
        var withDefs = ExtractDefinitions(rootBody);
        schema.AddRange(withDefs);
        return schema;
    }

    private static void AnalyzeValue(BsonValue value, FieldStats stats, int enumLimit)
    {
        stats.InstanceCount++;

        var jsonType = MapToJsonType(value);
        stats.Types.Add(jsonType);

        // Detect BSON date time values and infer string formats using heuristics
        var isDateTime = value.BsonType == BsonType.DateTime;
        if (!isDateTime && value.IsString)
        {
            var s = value.AsString;
            if (!string.IsNullOrWhiteSpace(s))
            {
                var heur = StringHeuristics.Infer(s);
                if (heur.Format is not null && string.Equals(heur.Format, "date-time", StringComparison.OrdinalIgnoreCase))
                {
                    isDateTime = true;
                }
                else
                {
                    // fallback to general parse if heuristics didn't match
                    if (DateTimeOffset.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out _)
                        || DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                    {
                        isDateTime = true;
                    }
                }
            }
        }

        if (isDateTime)
        {
            stats.HasDateTimeValues = true;
            // record date-time heuristic
            stats.StringHints = StringHeuristics.Merge(stats.StringHints, new StringHeuristics(Format: "date-time"));
        }

        switch (jsonType)
        {
            case "null":
            case "boolean":
                AnalyzeScalar(value, jsonType, stats);
                break;
            case "integer":
                // Allow enums for integers and booleans (not for floating point numbers or dates)
                stats.AddEnumValue(value, enumLimit);
                AnalyzeScalar(value, jsonType, stats);
                break;
            case "string":
                // Strings can be enums except when they are date/datetime values
                if (!isDateTime)
                {
                    stats.AddEnumValue(value, enumLimit);
                }

                // infer string heuristics (format/pattern)
                if (value.IsString)
                {
                    var s = value.AsString;
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        var heur = StringHeuristics.Infer(s);
                        stats.StringHints = StringHeuristics.Merge(stats.StringHints, heur);
                    }
                }

                AnalyzeScalar(value, jsonType, stats);
                break;
            case "number":
                // numeric (floating point) values: analyze numeric constraints but do not collect enum values
                AnalyzeScalar(value, jsonType, stats);
                break;
            case "object":
                stats.HasNonScalarValues = true;
                AnalyzeObject(value.AsBsonDocument, stats, enumLimit);
                break;
            case "array":
                stats.HasNonScalarValues = true;
                AnalyzeArray(value.AsBsonArray, stats, enumLimit);
                break;
        }
    }

    private static void AnalyzeScalar(BsonValue value, string jsonType, FieldStats stats)
    {
        if (jsonType is "integer" or "number")
        {
            var number = value.ToDouble();
            stats.Minimum = stats.Minimum is null ? number : Math.Min(stats.Minimum.Value, number);
            stats.Maximum = stats.Maximum is null ? number : Math.Max(stats.Maximum.Value, number);
        }

        if (jsonType == "string")
        {
            var length = (value.ToString() ?? string.Empty).Length;
            stats.MinLength = stats.MinLength is null ? length : Math.Min(stats.MinLength.Value, length);
            stats.MaxLength = stats.MaxLength is null ? length : Math.Max(stats.MaxLength.Value, length);
        }
    }

    private static void AnalyzeObject(BsonDocument document, FieldStats stats, int enumLimit)
    {
        stats.ObjectInstanceCount++;

        var propertyCount = document.ElementCount;
        stats.MinProperties = stats.MinProperties is null ? propertyCount : Math.Min(stats.MinProperties.Value, propertyCount);
        stats.MaxProperties = stats.MaxProperties is null ? propertyCount : Math.Max(stats.MaxProperties.Value, propertyCount);

        foreach (var element in document)
        {
            if (!stats.PropertyPresence.TryAdd(element.Name, 1))
            {
                stats.PropertyPresence[element.Name]++;
            }

            if (!stats.Properties.TryGetValue(element.Name, out var childStats))
            {
                childStats = new FieldStats();
                stats.Properties[element.Name] = childStats;
            }

            AnalyzeValue(element.Value, childStats, enumLimit);
        }
    }

    private static void AnalyzeArray(BsonArray array, FieldStats stats, int enumLimit)
    {
        stats.ArrayInstanceCount++;

        var itemCount = array.Count;
        stats.MinItems = stats.MinItems is null ? itemCount : Math.Min(stats.MinItems.Value, itemCount);
        stats.MaxItems = stats.MaxItems is null ? itemCount : Math.Max(stats.MaxItems.Value, itemCount);

        if (stats.ItemsStats is null)
        {
            stats.ItemsStats = new FieldStats();
        }

        var uniqueValues = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in array)
        {
            AnalyzeValue(item, stats.ItemsStats, enumLimit);
            uniqueValues.Add(item.ToJson());
        }

        if (stats.ArrayItemsAlwaysUnique && uniqueValues.Count != itemCount)
        {
            stats.ArrayItemsAlwaysUnique = false;
        }
    }

    private static BsonDocument BuildSchema(FieldStats stats)
    {
        var schema = new BsonDocument();

        if (stats.Types.Count > 0)
        {
            if (stats.Types.Count == 1)
            {
                schema["type"] = stats.Types.First();
            }
            else
            {
                schema["type"] = new BsonArray(stats.Types.OrderBy(x => x));
            }
        }

        var scalarOnly = !stats.HasNonScalarValues;
        if (scalarOnly && stats.CanUseEnum)
        {
            if (stats.EnumValues.Count == 1)
            {
                schema["const"] = stats.EnumValues.First();
            }
            else
            {
                schema["enum"] = new BsonArray(stats.EnumValues);
            }
        }

        var usingEnum = scalarOnly && stats.CanUseEnum;
        var sameValue = Math.Abs((stats.Minimum ?? 0.0) - (stats.Maximum ?? 0.0)) < 0.00001;

        if (stats.Minimum is not null && !usingEnum && !sameValue)
        {
            schema["minimum"] = stats.Minimum.Value;
        }

        if (stats.Maximum is not null && !usingEnum && !sameValue)
        {
            schema["maximum"] = stats.Maximum.Value;
        }

        // Export string length constraints only for non-date strings
        if (stats.Types.Contains("string") && !stats.HasDateTimeValues && !usingEnum)
        {
            if (stats.MinLength is not null)
            {
                schema["minLength"] = stats.MinLength.Value;
            }

            if (stats.MaxLength is not null)
            {
                schema["maxLength"] = stats.MaxLength.Value;
            }
        }

        // Emit string format/pattern hints when this field is a string
        if (stats.Types.Contains("string"))
        {
            if (stats.HasDateTimeValues)
            {
                schema["format"] = "date-time";
            }
            else if (stats.StringHints.Format is not null)
            {
                schema["format"] = stats.StringHints.Format;
            }

            if (stats.StringHints.Pattern is not null)
            {
                schema["pattern"] = stats.StringHints.Pattern;
            }
        }

        if (stats.MinItems is not null)
        {
            schema["minItems"] = stats.MinItems.Value;
        }

        if (stats.MaxItems is not null)
        {
            schema["maxItems"] = stats.MaxItems.Value;
        }

        if (stats.ArrayInstanceCount > 0 && stats.ArrayItemsAlwaysUnique)
        {
            schema["uniqueItems"] = true;
        }

        if (stats.ItemsStats is not null && stats.ItemsStats.InstanceCount > 0)
        {
            schema["items"] = BuildSchema(stats.ItemsStats);
        }

        if (stats.MinProperties is not null)
        {
            schema["minProperties"] = stats.MinProperties.Value;
        }

        if (stats.MaxProperties is not null)
        {
            schema["maxProperties"] = stats.MaxProperties.Value;
        }

        if (stats.Properties.Count > 0)
        {
            var children = stats.Properties
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new
                {
                    Name = x.Key,
                    Stats = x.Value,
                    Schema = BuildSchema(x.Value),
                    ShapeSignature = GetShapeSignature(x.Value)
                })
                .ToList();

            // Automatic map/dictionary-like object detection:
            // many sibling properties sharing the same structural shape -> additionalProperties
            var shapeGroups = children
                .GroupBy(x => x.ShapeSignature, StringComparer.Ordinal)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .ToList();

            var dominantGroup = shapeGroups.First();
            var dominantCount = dominantGroup.Count();
            var totalCount = children.Count;
            var dominantRatio = totalCount == 0 ? 0.0 : (double)dominantCount / totalCount;

            var requiredCount = children.Count(child =>
                stats.PropertyPresence.TryGetValue(child.Name, out var presenceCount)
                && presenceCount == stats.ObjectInstanceCount);

            var requiredRatio = totalCount == 0 ? 0.0 : (double)requiredCount / totalCount;
            var avgPresenceRatio = totalCount == 0 || stats.ObjectInstanceCount == 0
                ? 1.0
                : children.Average(child =>
                    stats.PropertyPresence.TryGetValue(child.Name, out var presenceCount)
                        ? (double)presenceCount / stats.ObjectInstanceCount
                        : 0.0);

            var hasDominantCluster = dominantCount >= 3
                                     && (dominantRatio >= 0.25 || dominantCount == totalCount);

            var hasDynamicKeySignal = requiredRatio < 0.95
                                      || avgPresenceRatio < 0.95
                                      || dominantCount == totalCount;

            var isMapLikeObject = totalCount >= 3
                                  && hasDominantCluster
                                  && hasDynamicKeySignal;

            if (isMapLikeObject)
            {
                // Dynamic keys with a common value schema.
                // Build merged schema for the dominant group to avoid overfitting to the first key/value.
                var mergedDominantStats = MergeFieldStats(dominantGroup.Select(x => x.Stats));
                schema["additionalProperties"] = BuildSchema(mergedDominantStats);

                // Keep non-dominant known keys (if any) as explicit properties
                var outliers = children.Where(x => x.ShapeSignature != dominantGroup.Key).ToList();
                if (outliers.Count > 0)
                {
                    var properties = new BsonDocument();
                    var required = new BsonArray();

                    foreach (var child in outliers)
                    {
                        properties[child.Name] = child.Schema;

                        if (stats.PropertyPresence.TryGetValue(child.Name, out var presenceCount) && presenceCount == stats.ObjectInstanceCount)
                        {
                            required.Add(child.Name);
                        }
                    }

                    schema["properties"] = properties;
                    if (required.Count > 0)
                    {
                        schema["required"] = required;
                    }
                }
            }
            else
            {
                var properties = new BsonDocument();
                var required = new BsonArray();

                foreach (var child in children)
                {
                    properties[child.Name] = child.Schema;

                    if (stats.PropertyPresence.TryGetValue(child.Name, out var presenceCount) && presenceCount == stats.ObjectInstanceCount)
                    {
                        required.Add(child.Name);
                    }
                }

                schema["properties"] = properties;

                if (required.Count > 0)
                {
                    schema["required"] = required;
                }
            }
        }

        return schema;
    }

    private static BsonDocument ExtractDefinitions(BsonDocument root)
    {
        var canonicalRoot = CanonicalizeSchemaNode(root);
        var rootSignature = GetExtractionSignature(canonicalRoot);

        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        var representatives = new Dictionary<string, BsonDocument>(StringComparer.Ordinal);

        CollectNodes(canonicalRoot, occurrences, representatives);

        var extractSet = occurrences
            .Where(x => x.Value >= 2 && x.Key != rootSignature && WorthExtracting(representatives[x.Key]))
            .Select(x => x.Key)
            .ToHashSet(StringComparer.Ordinal);

        if (extractSet.Count == 0)
        {
            return canonicalRoot;
        }

        var orderedExtractSigs = extractSet
            .OrderByDescending(sig => Complexity(representatives[sig]))
            .ThenBy(sig => sig, StringComparer.Ordinal)
            .ToList();

        var defNameBySig = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < orderedExtractSigs.Count; i++)
        {
            defNameBySig[orderedExtractSigs[i]] = $"Def{i + 1}";
        }

        var defsBySig = new Dictionary<string, BsonDocument>(StringComparer.Ordinal);
        var inProgress = new HashSet<string>(StringComparer.Ordinal);
        var completed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var sig in orderedExtractSigs)
        {
            EmitDefinition(sig, representatives, extractSet, defNameBySig, defsBySig, inProgress, completed);
        }

        var emittedRoot = EmitNode(canonicalRoot, true, extractSet, defNameBySig);

        var defs = new BsonDocument();
        foreach (var sig in orderedExtractSigs)
        {
            defs[defNameBySig[sig]] = defsBySig[sig];
        }

        emittedRoot["$defs"] = defs;
        return emittedRoot;
    }

    private static void EmitDefinition(
        string signature,
        IReadOnlyDictionary<string, BsonDocument> representatives,
        HashSet<string> extractSet,
        IReadOnlyDictionary<string, string> defNameBySig,
        Dictionary<string, BsonDocument> defsBySig,
        HashSet<string> inProgress,
        HashSet<string> completed)
    {
        if (completed.Contains(signature))
        {
            return;
        }

        if (inProgress.Contains(signature))
        {
            return;
        }

        inProgress.Add(signature);

        var node = representatives[signature];
        var emitted = EmitNode(node, true, extractSet, defNameBySig);
        defsBySig[signature] = emitted;

        inProgress.Remove(signature);
        completed.Add(signature);
    }

    private static BsonDocument EmitNode(
        BsonDocument node,
        bool isRoot,
        HashSet<string> extractSet,
        IReadOnlyDictionary<string, string> defNameBySig)
    {
        var sig = GetExtractionSignature(node);
        if (!isRoot && extractSet.Contains(sig))
        {
            return new BsonDocument("$ref", $"#/$defs/{defNameBySig[sig]}");
        }

        var emitted = new BsonDocument();
        foreach (var element in node)
        {
            emitted[element.Name] = EmitValue(element.Value, extractSet, defNameBySig);
        }

        return emitted;
    }

    private static BsonValue EmitValue(
        BsonValue value,
        HashSet<string> extractSet,
        IReadOnlyDictionary<string, string> defNameBySig)
    {
        if (value is BsonDocument doc)
        {
            return EmitNode(doc, false, extractSet, defNameBySig);
        }

        if (value is BsonArray arr)
        {
            var emittedArray = new BsonArray();
            foreach (var item in arr)
            {
                emittedArray.Add(EmitValue(item, extractSet, defNameBySig));
            }
            return emittedArray;
        }

        return value;
    }

    private static void CollectNodes(
        BsonDocument node,
        Dictionary<string, int> occurrences,
        Dictionary<string, BsonDocument> representatives)
    {
        var sig = GetExtractionSignature(node);
        if (!occurrences.TryAdd(sig, 1))
        {
            occurrences[sig]++;
        }

        representatives.TryAdd(sig, GeneralizeForExtraction(node).AsBsonDocument);

        foreach (var child in EnumerateChildSchemas(node))
        {
            CollectNodes(child, occurrences, representatives);
        }
    }

    private static string GetExtractionSignature(BsonValue value)
    {
        return GetSignature(GeneralizeForExtraction(value));
    }

    private static BsonValue GeneralizeForExtraction(BsonValue value)
    {
        if (value is BsonArray arr)
        {
            var normalized = new BsonArray();
            foreach (var item in arr)
            {
                normalized.Add(GeneralizeForExtraction(item));
            }

            return normalized;
        }

        if (value is not BsonDocument doc)
        {
            return value;
        }

        var normalizedDoc = new BsonDocument();
        foreach (var element in doc)
        {
            // Drop highly value-specific constraints during definition grouping
            if (element.Name is "const" or "enum" or "minimum" or "maximum" or "minLength" or "maxLength" or "minItems" or "maxItems" or "minProperties" or "maxProperties")
            {
                continue;
            }

            normalizedDoc[element.Name] = GeneralizeForExtraction(element.Value);
        }

        return normalizedDoc;
    }

    private static IEnumerable<BsonDocument> EnumerateChildSchemas(BsonDocument node)
    {
        foreach (var element in node)
        {
            if (element.Name == "properties" && element.Value is BsonDocument props)
            {
                foreach (var p in props)
                {
                    if (p.Value is BsonDocument pDoc)
                    {
                        yield return pDoc;
                    }
                }
                continue;
            }

            if (element.Name is "items" or "contains" or "if" or "then" or "else" && element.Value is BsonDocument childDoc)
            {
                yield return childDoc;
                continue;
            }

            if (element.Name == "additionalProperties" && element.Value is BsonDocument additionalPropsDoc)
            {
                yield return additionalPropsDoc;
                continue;
            }

            if (element.Name is "allOf" or "anyOf" or "oneOf" && element.Value is BsonArray arr)
            {
                foreach (var item in arr)
                {
                    if (item is BsonDocument itemDoc)
                    {
                        yield return itemDoc;
                    }
                }
                continue;
            }
        }
    }

    private static string GetShapeSignature(FieldStats stats)
    {
        var typeSignature = string.Join("|", stats.Types.OrderBy(x => x, StringComparer.Ordinal));

        if (stats.Properties.Count == 0 && stats.ItemsStats is null)
        {
            if (stats.Types.Contains("string"))
            {
                var fmt = stats.HasDateTimeValues ? "date-time" : stats.StringHints.Format ?? "";
                var pat = stats.StringHints.Pattern ?? "";
                return $"scalar({typeSignature})[{fmt}|{pat}]";
            }

            return $"scalar({typeSignature})";
        }

        if (stats.ItemsStats is not null)
        {
            return $"array({GetShapeSignature(stats.ItemsStats)})";
        }

        var children = stats.Properties
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new
            {
                Name = x.Key,
                ChildSignature = GetShapeSignature(x.Value)
            })
            .ToList();

        if (children.Count > 0)
        {
            var groups = children
                .GroupBy(x => x.ChildSignature, StringComparer.Ordinal)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .ToList();

            var dominant = groups.First();
            var dominantCount = dominant.Count();
            var dominantRatio = (double)dominantCount / children.Count;

            var isShapeMapLike = children.Count >= 3 && dominantCount >= 3 && dominantRatio >= 0.66;
            if (isShapeMapLike)
            {
                var outlierParts = groups
                    .Skip(1)
                    .SelectMany(g => g.OrderBy(x => x.Name, StringComparer.Ordinal)
                        .Select(x => $"{x.Name}:{x.ChildSignature}"))
                    .ToList();

                return outlierParts.Count == 0
                    ? $"objectMap({dominant.Key})"
                    : $"objectMap({dominant.Key})+out({string.Join(",", outlierParts)})";
            }
        }

        var propParts = children.Select(x => $"{x.Name}:{x.ChildSignature}");

        return $"object({string.Join(",", propParts)})";
    }

    private static FieldStats MergeFieldStats(IEnumerable<FieldStats> sourceStats)
    {
        var merged = new FieldStats();
        foreach (var source in sourceStats)
        {
            MergeFieldStatsInto(merged, source);
        }

        return merged;
    }

    private static void MergeFieldStatsInto(FieldStats target, FieldStats source)
    {
        target.InstanceCount += source.InstanceCount;
        target.ObjectInstanceCount += source.ObjectInstanceCount;
        target.ArrayInstanceCount += source.ArrayInstanceCount;

        target.Types.UnionWith(source.Types);

        target.Minimum = target.Minimum is null
            ? source.Minimum
            : source.Minimum is null ? target.Minimum : Math.Min(target.Minimum.Value, source.Minimum.Value);

        target.Maximum = target.Maximum is null
            ? source.Maximum
            : source.Maximum is null ? target.Maximum : Math.Max(target.Maximum.Value, source.Maximum.Value);

        target.MinLength = target.MinLength is null
            ? source.MinLength
            : source.MinLength is null ? target.MinLength : Math.Min(target.MinLength.Value, source.MinLength.Value);

        target.MaxLength = target.MaxLength is null
            ? source.MaxLength
            : source.MaxLength is null ? target.MaxLength : Math.Max(target.MaxLength.Value, source.MaxLength.Value);

        target.MinItems = target.MinItems is null
            ? source.MinItems
            : source.MinItems is null ? target.MinItems : Math.Min(target.MinItems.Value, source.MinItems.Value);

        target.MaxItems = target.MaxItems is null
            ? source.MaxItems
            : source.MaxItems is null ? target.MaxItems : Math.Max(target.MaxItems.Value, source.MaxItems.Value);

        target.MinProperties = target.MinProperties is null
            ? source.MinProperties
            : source.MinProperties is null ? target.MinProperties : Math.Min(target.MinProperties.Value, source.MinProperties.Value);

        target.MaxProperties = target.MaxProperties is null
            ? source.MaxProperties
            : source.MaxProperties is null ? target.MaxProperties : Math.Max(target.MaxProperties.Value, source.MaxProperties.Value);

        target.ArrayItemsAlwaysUnique = target.ArrayItemsAlwaysUnique && source.ArrayItemsAlwaysUnique;
        target.HasNonScalarValues = target.HasNonScalarValues || source.HasNonScalarValues;
        target.HasDateTimeValues = target.HasDateTimeValues || source.HasDateTimeValues;
        target.StringHints = StringHeuristics.Merge(target.StringHints, source.StringHints);

        foreach (var value in source.EnumValues)
        {
            target.AddEnumValue(value, 2048);
        }

        foreach (var (name, presenceCount) in source.PropertyPresence)
        {
            if (!target.PropertyPresence.TryAdd(name, presenceCount))
            {
                target.PropertyPresence[name] += presenceCount;
            }
        }

        foreach (var (name, childSourceStats) in source.Properties)
        {
            if (!target.Properties.TryGetValue(name, out var childTargetStats))
            {
                childTargetStats = new FieldStats();
                target.Properties[name] = childTargetStats;
            }

            MergeFieldStatsInto(childTargetStats, childSourceStats);
        }

        if (source.ItemsStats is not null)
        {
            target.ItemsStats ??= new FieldStats();
            MergeFieldStatsInto(target.ItemsStats, source.ItemsStats);
        }
    }

    private static bool WorthExtracting(BsonDocument node)
    {
        if (!node.TryGetValue("type", out var typeValue))
        {
            return false;
        }

        var types = ExtractTypes(typeValue);
        if (types.Contains("object"))
        {
            return true;
        }

        if (types.Contains("array") && Complexity(node) >= 8)
        {
            return true;
        }

        return false;
    }

    private static int Complexity(BsonDocument node)
    {
        if (!node.TryGetValue("type", out var typeValue))
        {
            return 1;
        }

        var types = ExtractTypes(typeValue);
        if (types.Contains("object"))
        {
            var score = 2;
            if (node.TryGetValue("properties", out var propertiesValue) && propertiesValue is BsonDocument props)
            {
                score += props.ElementCount;
                foreach (var p in props)
                {
                    if (p.Value is BsonDocument pDoc)
                    {
                        score += Complexity(pDoc);
                    }
                }
            }

            return score;
        }

        if (types.Contains("array"))
        {
            if (node.TryGetValue("items", out var itemsValue) && itemsValue is BsonDocument itemsDoc)
            {
                return 2 + Complexity(itemsDoc);
            }

            return 2;
        }

        return 1;
    }

    private static HashSet<string> ExtractTypes(BsonValue typeValue)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (typeValue.IsString)
        {
            result.Add(typeValue.AsString);
            return result;
        }

        if (typeValue is BsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item.IsString)
                {
                    result.Add(item.AsString);
                }
            }
        }

        return result;
    }

    private static BsonDocument CanonicalizeSchemaNode(BsonDocument node)
    {
        var result = new BsonDocument();
        foreach (var key in node.Names.OrderBy(x => x, StringComparer.Ordinal))
        {
            var value = node[key];
            result[key] = CanonicalizeValue(key, value);
        }

        return result;
    }

    private static BsonValue CanonicalizeValue(string key, BsonValue value)
    {
        if (key == "properties" && value is BsonDocument properties)
        {
            var canonicalProps = new BsonDocument();
            foreach (var name in properties.Names.OrderBy(x => x, StringComparer.Ordinal))
            {
                canonicalProps[name] = properties[name] is BsonDocument pd ? CanonicalizeSchemaNode(pd) : properties[name];
            }
            return canonicalProps;
        }

        if (value is BsonDocument doc)
        {
            return CanonicalizeSchemaNode(doc);
        }

        if (value is BsonArray arr)
        {
            var list = arr.Select(item => item is BsonDocument itemDoc ? (BsonValue)CanonicalizeSchemaNode(itemDoc) : item).ToList();

            if (key is "required" or "type")
            {
                list = list.OrderBy(x => x.ToString(), StringComparer.Ordinal).ToList();
            }
            else if (key is "allOf" or "anyOf" or "oneOf")
            {
                list = list.OrderBy(GetSignature, StringComparer.Ordinal).ToList();
            }

            return new BsonArray(list);
        }

        return value;
    }

    private static string GetSignature(BsonValue value)
    {
        if (value is BsonDocument doc)
        {
            var parts = doc.Elements.Select(e => $"{e.Name}:{GetSignature(e.Value)}");
            return "{" + string.Join(",", parts) + "}";
        }

        if (value is BsonArray arr)
        {
            return "[" + string.Join(",", arr.Select(GetSignature)) + "]";
        }

        return value.ToJson();
    }

    private static string MapToJsonType(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.Null              => "null",
            BsonType.Boolean           => "boolean",
            BsonType.Int32             => "integer",
            BsonType.Int64             => "integer",
            BsonType.Double            => "number",
            BsonType.Decimal128        => "number",
            BsonType.DateTime          => "string",
            BsonType.ObjectId          => "string",
            BsonType.Binary            => "string",
            BsonType.Symbol            => "string",
            BsonType.JavaScript        => "string",
            BsonType.RegularExpression => "string",
            BsonType.Timestamp         => "string",
            BsonType.Array             => "array",
            BsonType.Document          => "object",
            _                          => "string"
        };
    }

    private sealed record StringHeuristics(
        string? Format = null,
        string? Pattern = null,
        bool IsEnumCandidate = false)
    {
        public string Signature => $"{Format ?? ""}|{Pattern ?? ""}|{(IsEnumCandidate ? "E" : "")}";

        private static readonly Regex IsoDateTime =
            new(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+\-]\d{2}:\d{2})$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex Uuid =
            new(@"^[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex Email =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex Uri =
            new(@"^https?://", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ObjectIdHex =
            new(@"^[0-9a-fA-F]{24}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static StringHeuristics Infer(string s)
        {
            if (IsoDateTime.IsMatch(s)) return new StringHeuristics(Format: "date-time");
            if (Uuid.IsMatch(s))        return new StringHeuristics(Format: "uuid");
            if (Email.IsMatch(s))       return new StringHeuristics(Format: "email");
            if (Uri.IsMatch(s))         return new StringHeuristics(Format: "uri");
            if (ObjectIdHex.IsMatch(s)) return new StringHeuristics(Pattern: "^[0-9a-fA-F]{24}$");

            return new StringHeuristics();
        }

        public static StringHeuristics Merge(StringHeuristics a, StringHeuristics b)
        {
            // keep a format only if they agree, otherwise drop to plain string
            var format = (a.Format is not null && a.Format == b.Format) ? a.Format : null;
            var pattern = (a.Pattern is not null && a.Pattern == b.Pattern) ? a.Pattern : null;
            return new StringHeuristics(format, pattern, a.IsEnumCandidate || b.IsEnumCandidate);
        }
    }

}
