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
﻿using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public static class FilterExpressionTree
{
    public const string IsoDatePrefix = "ISODate(\"";
    public const string IsoDateSuffix = "\")";


    public interface IFilterExpression
    {
        List<IFilterExpression> Children { get; }
        IFilterExpression       Clone();
    }

    public sealed class ExpressionGroup : IFilterExpression, ICloneable
    {
        public enum ConditionType               { And, Or }

        public ConditionType           Condition { get; set; } = ConditionType.And;
        public List<IFilterExpression> Children  { get; set; }      = [];

        object ICloneable.Clone() => Clone();

        public IFilterExpression Clone()
        {
            var clone = new ExpressionGroup
            {
                Condition = Condition
            };

            clone.Children.AddRange(Children.Select(x => x.Clone()));

            return clone;
        }

        public override string ToString()
        {
            if (IsEmpty)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append("( ");

            for (var i = 0; i < Children.Count; i++)
            {
                sb.Append(Children[i]);
                if (i < Children.Count - 1)
                {
                    sb.Append(Condition == ConditionType.And ? " AND " : " OR ");
                }
            }

            sb.Append(" )");
            return sb.ToString();
        }

        public bool IsEmpty => Children.Count == 0;

        public string ToJson(Dictionary<string, Type> fieldTypes) => FilterExpressionTree.ToJson(this, fieldTypes);
    }

    public enum FieldConditionType
    {
        [Description( "Contains")                ] Contains,
        [Description( "Starts with")             ] StartsWith,
        [Description( "Ends with")               ] EndsWith,
        [Description( "==")                      ] EqualTo,
        [Description( "!=")                      ] NotEqualTo,
        [Description( ">")                       ] GreaterThan,
        [Description( "<")                       ] LessThan,
        [Description( ">=")                      ] GreaterThanOrEqualTo,
        [Description( "<=")                      ] LessThanOrEqualTo,
        [Description( "Is empty")                ] IsEmpty,
        [Description( "Not is empty")            ] NotIsEmpty,
        [Description( "Is null")                 ] IsNull,
        [Description( "Not is null")             ] NotIsNull,
        [Description( "Matches")                 ] Matches,
        [Description( "Does not match")          ] DoesNotMatch,
        [Description( "Does not contain")        ] DoesNotContain,
        [Description( "Does not start with")     ] DoesNotStartWith,
        [Description( "Does not end with")       ] DoesNotEndWith,
    }

    public sealed class FieldExpression : IFilterExpression, ICloneable
    {
        public  List<IFilterExpression> Children => [];

        public FieldConditionType Condition { get; set; } = FieldConditionType.EqualTo;

        public string Field
        {
            get;
            set
            {
                if (field == value)
                    return;
                field = value.Trim();
            }
        } = "";

        public string Argument
        {
            get;
            set
            {
                if (field == value)
                    return;
                field = value.Trim();
            }
        } = "";

        object ICloneable.Clone() => Clone();

        public IFilterExpression Clone()
        {
            var clone = new FieldExpression
            {
                Condition = Condition,
                Field     = Field,
                Argument  = Argument
            };

            return clone;
        }

        public override string ToString()
        {
            var description = Condition.GetType()
                .GetField(Condition.ToString())
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() as DescriptionAttribute;

            return $"{Field} {description?.Description.ToLower() ?? Condition.ToString()} {Argument}";
        }

    }

    public static ExpressionGroup ParseJson( string filterText )
    {
        if (string.IsNullOrWhiteSpace(filterText))
        {
            return new();
        }

        BsonDocument? bson;
        try
        {
            bson = BsonDocument.Parse( filterText );

            var e = bson.Elements.First();
            if ( e.Name != "$and" )
                bson = null;
        }
        catch ( Exception )
        {
            // ignore
            bson = null;
        }

        if ( bson == null )
        {
            try
            {
                bson = BsonDocument.Parse($"{{ \"$and\" : [ {filterText } ] }}");
            }
            catch ( Exception )
            {
                return new();
            }
        }

        try
        {
            if (ParseCondition( bson.Elements.First() ) is ExpressionGroup cond)
                return cond;
        }
        catch ( Exception )
        {
            // ignore
        }

        return new();
    }

    public static string ToJson(IFilterExpression? filter, IDictionary<string, Type> fieldTypes)
    {
        if (filter == null || (filter is ExpressionGroup grp && (grp.Children?.Count ?? 0) == 0))
            return "";

        var bson = MakeJsonExpression(filter, fieldTypes);
        if ( bson == null )
            return "";

        return bson.ElementCount > 0
            ? bson.ToJson(new() { Indent = true, OutputMode = JsonOutputMode.RelaxedExtendedJson })
            : "";
    }

    public static string ToSQL(ExpressionGroup parsedFilter, IDictionary<string, Type> fieldTypes)
    {
        if (parsedFilter == null || (parsedFilter.Children?.Count ?? 0) == 0)
            return "";
            
        var sql = MakeSqlExpression(parsedFilter, fieldTypes, 0);
        return sql;
    }

    private static IFilterExpression? ParseCondition(BsonElement element)
    {
        switch (element.Name)
        {
            case "$and":
            case "$or":
            {
                var cond = new ExpressionGroup
                {
                    Condition = element.Name == "$and"
                        ? ExpressionGroup.ConditionType.And
                        : ExpressionGroup.ConditionType.Or
                };

                foreach (var e in element.Value.AsBsonArray.Select(x => x.AsBsonDocument.First()))
                {
                    var expr = ParseCondition(e);
                    if ( expr == null )
                        continue;

                    cond.Children.Add( expr );
                }

                return cond;
            }


            default:
            {
                var fieldName = element.Name;
                if ( !element.Value.IsBsonDocument )
                {
                    var val = element.Value.ToString() ?? "";

                    if ( val.StartsWith( "/" ) && val.EndsWith( "/" ))
                        return ParsePropertyExpression(fieldName, "$regex", val);
                    return ParsePropertyExpression( fieldName, "$eq", val );
                }

                var e   = element.Value.AsBsonDocument.Elements.First();
                var op  = e.Name;
                var arg = e.Value.ToString() ?? "";
                return ParsePropertyExpression( fieldName, op, arg );
            }
        }
    }

    private static IFilterExpression? ParsePropertyExpression( string property, string op, string arg )
    {
        switch (op)
        {
            case "$ne":
                return CreateBinaryExpression(property, arg, FieldConditionType.NotEqualTo);
            case "$eq":                                 
                return CreateBinaryExpression(property, arg, FieldConditionType.EqualTo);
            case "$lte":                                
                return CreateBinaryExpression(property, arg, FieldConditionType.LessThanOrEqualTo);
            case "$lt":                                 
                return CreateBinaryExpression(property, arg, FieldConditionType.LessThan);
            case "$gte":                                
                return CreateBinaryExpression(property, arg, FieldConditionType.GreaterThanOrEqualTo);
            case "$gt":
                return CreateBinaryExpression(property, arg, FieldConditionType.GreaterThan);
            case "$regex":
            {
                arg = arg.Trim( '/' );
                var newCond = RegexToCondition( arg, out var newArg );

                return CreateBinaryExpression( property, newArg ?? arg, newCond );
            }
            default:
                return null;
        }
    }

    private static IFilterExpression CreateBinaryExpression(string property, string arg, FieldConditionType c)
        => new FieldExpression {Field = property, Condition = c, Argument = arg};


    private static string Shield(string s) =>
        s
            .Replace("\\", "\\\\") // first
            .Replace(".",  "\\.")
            .Replace("(",  "\\(")
            .Replace(")",  "\\)")
            .Replace("*",  "\\*")
            .Replace("?",  "\\?")
            .Replace("[",  "\\]")
            .Replace("]",  "\\]");

    private static string Unshield(string s) =>
        s
            .Replace("\\.",  ".")
            .Replace("\\(",  "(")
            .Replace("\\)",  ")")
            .Replace("\\*",  "*")
            .Replace("\\?",  "?")
            .Replace("\\]",  "[")
            .Replace("\\]",  "]")
            .Replace("\\\\", "\\"); // last

    private static FieldConditionType RegexToCondition( string regex, out string? arg )
    {
        arg = null;
        switch ( regex )
        {
            case "^$":
                return FieldConditionType.IsEmpty;
            case "^.+$":
                return FieldConditionType.NotIsEmpty;
            default:
                if ( regex.StartsWith( "^.*" ) && regex.EndsWith(".*$"))
                {
                    arg = Unshield(regex.Substring( 3, regex.Length - 3 - 3 ));
                    return FieldConditionType.Contains;
                }
                if (regex.StartsWith("^.*") && regex.EndsWith("$"))
                {
                    arg = Unshield(regex.Substring(3, regex.Length - 3 - 1 ));
                    return FieldConditionType.EndsWith;
                }
                if (regex.StartsWith("^") && regex.EndsWith(".*$"))
                {
                    arg = Unshield(regex.Substring(1, regex.Length - 1- 3));
                    return FieldConditionType.StartsWith;
                }
                if (regex.StartsWith("^(?!") && regex.EndsWith(")$"))
                {
                    arg = Unshield(regex.Substring(1, regex.Length - 4 - 2));
                    return FieldConditionType.DoesNotMatch;
                }
                if (regex.StartsWith("^.*(?!") && regex.EndsWith(").*$"))
                {
                    arg = Unshield(regex.Substring(1, regex.Length - 6 - 4));
                    return FieldConditionType.DoesNotContain;
                }
                if (regex.StartsWith("^(?!") && regex.EndsWith(").*$"))
                {
                    arg = Unshield(regex.Substring(1, regex.Length - 4 - 4));
                    return FieldConditionType.DoesNotStartWith;
                }
                if (regex.StartsWith("^.*(?!") && regex.EndsWith(")$"))
                {
                    arg = Unshield(regex.Substring(1, regex.Length - 6 - 2));
                    return FieldConditionType.DoesNotEndWith;
                }
                return FieldConditionType.Matches;
        }
    }

    private static BsonDocument? MakeJsonExpression(IFilterExpression cond, IDictionary<string, Type> fieldTypes)
    {
        if (cond == null)
            throw new InvalidExpressionException("Expected FieldExpression but got NULL" );

        switch ( cond )
        {
            case ExpressionGroup when cond.Children.Count == 0:
                // empty group, return null
                return null;
            case ExpressionGroup when cond.Children is [FieldExpression { Condition: FieldConditionType.EqualTo }]:
                // skip grouping if only one child
                {
                    var fieldExpr = (FieldExpression)cond.Children[0];
                    return [new(fieldExpr.Field, ConvertValue(fieldExpr.Field, fieldExpr.Argument))];
                }
            case ExpressionGroup group:
                var items = cond.Children.Select( cond1 => MakeJsonExpression(cond1, fieldTypes) ).Where( x => x != null ).ToList();
                if ( items.Count == 0 )
                    return null;

                var elem = new BsonElement(group.Condition == ExpressionGroup.ConditionType.And ? "$and" : "$or", new BsonArray(items));
                return [elem];
        }

        if ( cond is not FieldExpression propExpr )
            throw new InvalidExpressionException($"Expected FieldExpression but got {cond} ({cond.GetType()})" );

        var prop = propExpr.Field;
        var op   = propExpr.Condition;
        var val  = propExpr.Argument;

        var arg = ConvertValue(prop, val);

        var o = ConvertCondition( op, arg.ToString() ?? "", out var regex );
        var d = new BsonDocument {new( o, regex ?? arg )};

        return [new(prop, d)];

        BsonValue ConvertValue(string name, string value)
        {
            if (!fieldTypes.TryGetValue(name, out var propType))
                propType = typeof(string);

            BsonValue bsonValue;
            if (propType == typeof(double))
                bsonValue = new BsonDouble(Convert.ToDouble(value));
            else if (propType == typeof(int))
                bsonValue = new BsonInt32(Convert.ToInt32(value));
            else if (propType == typeof(decimal))
                bsonValue = new BsonDouble(Convert.ToDouble(value));
            else if (propType == typeof(long))
                bsonValue  = new BsonInt64(Convert.ToInt64(value));
            else bsonValue = propType == typeof(DateTime) || value.StartsWith(IsoDatePrefix)
                    ? new BsonDateTime(ConvertToDateTime(value)) 
                    : new BsonString(value)
                    ;
            return bsonValue;
        }
    }

    private const DateTimeStyles DateTimeStyle = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    private static DateTime ConvertToDateTime(string val)
    {

        if (val.StartsWith(IsoDatePrefix) && val.EndsWith(IsoDateSuffix))
        {
            var inner = val.Substring(IsoDatePrefix.Length, val.Length - IsoDatePrefix.Length - IsoDateSuffix.Length);
            return ConvertExact(inner);
        }

        return ConvertExact(val);

        DateTime ConvertExact(string value)
        {
            var formats = new[]
            {
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:ss.fffZ",
                "yyyy-MM-ddTHH:mm:ssK",
                "yyyy-MM-ddTHH:mm:ss.fffK",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss.fff",
                "yyyy-MM-dd",
                "yyyy/MM/ddTHH:mm:ssZ",
                "yyyy/MM/ddTHH:mm:ss.fffZ",
                "yyyy/MM/ddTHH:mm:ssK",
                "yyyy/MM/ddTHH:mm:ss.fffK",
                "yyyy/MM/dd HH:mm:ss",
                "yyyy/MM/dd HH:mm:ss.fff",
                "yyyy/MM/dd",
                // really don't recommend these
                "dd/MM/yyyy",
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy HH:mm:ss.fff",
                "dd-MM-yyyy"
            };

            foreach ( var format in formats )
            {
                if (DateTime.TryParseExact(value, format, null, DateTimeStyle, out var d))
                    return DateTime.SpecifyKind(d, DateTimeKind.Utc);
            }
            throw new ApplicationException("Invalid date/time: " + value);
        }
    }

    private static string ConvertCondition(FieldConditionType op, string arg, out string? regex)
    {
        regex = null;
        switch (op)
        {
            case FieldConditionType.EqualTo:
                return "$eq";
            case FieldConditionType.NotEqualTo:
                return "$ne";
            case FieldConditionType.LessThanOrEqualTo:
                return "$lte";
            case FieldConditionType.LessThan:
                return "$lt";
            case FieldConditionType.GreaterThanOrEqualTo:
                return "$gte";
            case FieldConditionType.GreaterThan:
                return "$gt";
            case FieldConditionType.Contains:
                regex = $"^.*{Shield(arg)}.*$";
                return "$regex";
            case FieldConditionType.StartsWith:
                regex = $"^{Shield(arg)}.*$";
                return "$regex";
            case FieldConditionType.EndsWith:
                regex = $"^.*{Shield(arg)}$";
                return "$regex";
            case FieldConditionType.IsEmpty:
                regex = "^$";
                return "$regex";
            case FieldConditionType.NotIsEmpty:
                regex = "^.+$";
                return "$regex";
            case FieldConditionType.IsNull:
                regex = "^$";
                return "$regex";
            case FieldConditionType.NotIsNull:
                regex = "^.+$";
                return "$regex";
            case FieldConditionType.Matches:
                return "$regex";
            case FieldConditionType.DoesNotMatch:
                regex = $"^(?!{arg})$";
                return "$regex";
            case FieldConditionType.DoesNotContain:
                // https://stackoverflow.com/a/406408
                regex = $"^((?!{Shield(arg)}).)*$";
                return "$regex";
            case FieldConditionType.DoesNotStartWith:
                regex = $"^(?!{Shield(arg)}).*$";
                return "$regex";
            case FieldConditionType.DoesNotEndWith:
                regex = $"^.*(?!{Shield(arg)})$";
                return "$regex";
            default:
                throw new ApplicationException($"Unsupported operation {op}");
        }
    }

    /// <summary>
    /// https://www.codeproject.com/Tips/483763/Equivalent-function-of-mysql-real-escape-string-in
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private static string ShieldSql(string str) =>
        Regex.Replace(str, @"[\x00'""\b\n\r\t\cZ\\%_]",
            delegate(Match match)
            {
                var v = match.Value;
                switch (v)
                {
                    case "\x00": // ASCII NUL (0x00) character
                        return "\\0";   
                    case "\b": // BACKSPACE character
                        return "\\b";
                    case "\n": // NEWLINE (linefeed) character
                        return "\\n";
                    case "\r": // CARRIAGE RETURN character
                        return "\\r";
                    case "\t": // TAB
                        return "\\t";
                    case "\u001A": // Ctrl-Z
                        return "\\Z";
                    default:
                        return "\\" + v;
                }
            });

    private static string MakeSqlExpression(IFilterExpression filter, IDictionary<string, Type> fieldTypes, int level)
    {
        var prefix = new string(' ', level);
        if (filter is ExpressionGroup group)
        {
            var sb = new StringBuilder();
            if ( group.Condition == ExpressionGroup.ConditionType.And )
            {
                sb.AppendJoin(
                    $"\n\t{prefix}AND ",
                    group.Children.Select(x => MakeSqlExpression(x, fieldTypes, level+1)));
            }
            else
            {
                sb.AppendLine($"\n\t{prefix}(");
                sb.AppendJoin(
                    $"\n\t{prefix}OR ",
                    group.Children.Select(x => MakeSqlExpression(x, fieldTypes, level+1)));
                sb.AppendLine($"\n\t{prefix})");
            }
            return sb.ToString();
        }

        var expr = (FieldExpression) filter;
        var s    = $"\t{prefix}{expr.Field} {GetSqlCondition(expr, fieldTypes)}";
        return s;
    }

    private static string GetSqlArg(FieldExpression expr, IDictionary<string, Type> fieldTypes)
    {
        if (expr.Argument == null)
            return "''";

        if (!fieldTypes.TryGetValue(expr.Field, out var propType))
            propType = typeof(string);

        if (propType == typeof(string))
            return $"'{ShieldSql(expr.Argument)}'";
        if (propType == typeof(double))
            return string.IsNullOrWhiteSpace(expr.Argument) ? "0" : Convert.ToDouble(expr.Argument).ToString(CultureInfo.InvariantCulture);
        if (propType == typeof(int))
            return string.IsNullOrWhiteSpace(expr.Argument) ? "0" : Convert.ToInt32(expr.Argument).ToString(CultureInfo.InvariantCulture);
        if (propType == typeof(decimal))
            return string.IsNullOrWhiteSpace(expr.Argument) ? "0" : Convert.ToDouble(expr.Argument).ToString(CultureInfo.InvariantCulture);
        if (propType == typeof(long))
            return string.IsNullOrWhiteSpace(expr.Argument) ? "0" : Convert.ToInt64(expr.Argument).ToString(CultureInfo.InvariantCulture);
        if (propType == typeof(DateTime))
            return string.IsNullOrWhiteSpace(expr.Argument)
                ? "0"
                : $"timestamp( '{ToDateTime(expr):yyyy-MM-dd HH:mm:ss}' )"; // should work for Oracle and ClickHouse
        if (propType == typeof(DateOnly))
            return string.IsNullOrWhiteSpace(expr.Argument)
                ? "0"
                : $"timestamp( '{ToDateOnly(expr):yyyy-MM-dd}' )"; // should work for Oracle and ClickHouse
        return ShieldSql(expr.Argument);
    }

    private static DateTime ToDateTime(FieldExpression expr)
    {
        try
        {
            return DateTime.Parse(expr.Argument, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }
        catch (FormatException)
        {
            return DateTime.ParseExact(expr.Argument, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }
    }

    private static DateOnly ToDateOnly(FieldExpression expr)
        => DateOnly.Parse(expr.Argument, CultureInfo.InvariantCulture);

    private static string GetSqlCondition(FieldExpression expr, IDictionary<string, Type> fieldTypes)
    {
        var arg = GetSqlArg(expr, fieldTypes);
        return expr.Condition switch
        {
            FieldConditionType.EqualTo              => $"= {arg}",
            FieldConditionType.NotEqualTo           => $"!= {arg}",
            FieldConditionType.LessThanOrEqualTo    => $"<= {arg}",
            FieldConditionType.LessThan             => $"< {arg}",
            FieldConditionType.GreaterThanOrEqualTo => $">= {arg}",
            FieldConditionType.GreaterThan          => $"> {arg}",
            FieldConditionType.Contains             => $"LIKE '%{ShieldSql(expr.Argument)}%'",
            FieldConditionType.StartsWith           => $"LIKE '{ShieldSql(expr.Argument)}%'",
            FieldConditionType.EndsWith             => $"LIKE '%{ShieldSql(expr.Argument)}'",
            FieldConditionType.IsEmpty              => "= ''",
            FieldConditionType.NotIsEmpty           => "!= ''",
            FieldConditionType.IsNull               => "IS NULL",
            FieldConditionType.NotIsNull            => "IS NOT NULL",
            FieldConditionType.Matches              => $"LIKE '{ShieldSql(expr.Argument)}'",
            FieldConditionType.DoesNotMatch         => $"NOT LIKE '{ShieldSql(expr.Argument)}'",
            FieldConditionType.DoesNotContain       => $"NOT LIKE '%{ShieldSql(expr.Argument)}%'",
            FieldConditionType.DoesNotStartWith     => $"NOT LIKE '{ShieldSql(expr.Argument)}%'",
            FieldConditionType.DoesNotEndWith       => $"NOT LIKE '%{ShieldSql(expr.Argument)}'",
            _                                       => throw new ApplicationException($"Unsupported operation {expr.Condition}")
        };
    }
}