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
﻿#if MSJSON
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Rms.Risk.Forge.Service.Client.Impl;
#else
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace Rms.Risk.Mango.Pivot.UI.Services;

#if MSJSON
#else
/// <summary>
/// Mimics System.Text.Json, but works for Newtonsoft.Json.
/// </summary>
public class JsonSerializerOptions
{
    /// <summary>
    /// Converters list
    /// </summary>
    public List<JsonConverter> Converters { get; } = [];
    /// <summary>
    /// Indent output
    /// </summary>
    public bool WriteIndented { get; set; }
}
#endif
/// <summary>
/// Json serialization. Mostly to hide differences between Newtonsoft.Json and System.Text.Json.
/// </summary>
public static class JsonUtils
{
    /// <summary>
    /// Text.Json does not expose default configuration (02.03.2020).
    /// This is a reflection-based hack to set them properly.
    /// </summary>
    public static void ApplyJsonSerializerDefaultConfigHack()
    {
#if MSJSON
            var op = (JsonSerializerOptions)typeof( JsonSerializerOptions )
                                            .GetField( "s_defaultOptions",
                                                       System.Reflection.BindingFlags.Static |
                                                       System.Reflection.BindingFlags.NonPublic ).GetValue( null );
            op.IgnoreNullValues            = true;
            op.WriteIndented               = false;
            op.AllowTrailingCommas         = true;
            op.IgnoreReadOnlyProperties    = true;
            op.PropertyNameCaseInsensitive = true;
            op.ReadCommentHandling         = JsonCommentHandling.Skip;
            op.DictionaryKeyPolicy         = JsonNamingPolicy.CamelCase;
            op.PropertyNamingPolicy        = JsonNamingPolicy.CamelCase;
            op.Converters.Add( new KvpJsonConverterFactory() );
            //op.Encoder                   = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
#endif
    }

    /// <summary>
    /// Deserialize Json string into object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static T? FromJson<T>( string json, JsonSerializerOptions? options = null ) where T : class
    {
#if MSJSON
            return JsonSerializer.Deserialize<T>( json, options );
#else
        if (json == null)
            throw new ArgumentNullException(nameof(json));

        var shortJson = json[..Math.Min(json.Length, 24)];
        if (shortJson.IndexOf("<!doctype html>", StringComparison.OrdinalIgnoreCase) >= 0)
            throw new ApplicationException("Expected JSON, but got HTML");
        if (shortJson.IndexOf("<?xml version=\"", StringComparison.OrdinalIgnoreCase) >= 0)
            throw new ApplicationException("Expected JSON, but got XML");

        using var tr         = new StringReader(json);
        using var jsonReader = new JsonTextReader( tr );
        var       js         = new JsonSerializer();

        if ( options?.Converters != null )
        {
            foreach ( var conv in options.Converters )
                js.Converters.Add( conv );
        }

        return js.Deserialize<T>( jsonReader );
#endif
    }

    /// <summary>
    /// Check if string is a valid Json
    /// </summary>
    public static bool IsValidJson(string strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput))
            return false;

        strInput = strInput.Trim();
        if (strInput.StartsWith("{") && strInput.EndsWith("}") || //For object
            strInput.StartsWith("[") && strInput.EndsWith("]"))   //For array
        {
            try
            {
                _ = JToken.Parse(strInput);
                return true;
            }
            catch (JsonReaderException)
            {
                //Exception in parsing json
                return false;
            }
            catch (Exception) //some other exception
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Serialize an object to Json string.
    /// </summary>
    /// <param name="content"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static string ToJson( object content, JsonSerializerOptions? options = null )
    {
#if MSJSON
            return JsonUtils.ToJson( content );
#else
        using var tw  = new StringWriter();
        using var jtw = new JsonTextWriter( tw );
        var       js  = new JsonSerializer();

        if ( options?.Converters != null )
        {
            foreach ( var conv in options.Converters )
                js.Converters.Add( conv );
        }

        js.Formatting = options?.WriteIndented ?? false
                ? Formatting.Indented 
                : Formatting.None
            ;

        js.Serialize( jtw, content );
        return tw.ToString();
#endif
    }

    /// <summary>
    /// Ident json string
    /// </summary>
    public static string FormatJson(string? json)
    {
        try
        {
            if (json == null)
                return "{}";
            var jo = FromJson<object>(json);
            if (jo == null)
                return json;
            return ToJson(jo, new() { WriteIndented = true });
        }
        catch (Exception)
        {
            return json ?? "{}";
        }
    }

    public static IReadOnlyCollection<JToken> GetByPath(this JToken root, string path)
    {
        if (root == null || path == null)
            throw new ArgumentNullException();

        return root.SelectTokens(path).ToList();
    }    

    public static JToken? GetOneByPath(this JToken root, string path)
    {
        if (root == null || path == null)
            throw new ArgumentNullException();

        var list = root.SelectTokens(path).ToList();
        return list.FirstOrDefault();
    }    

    public static JToken ReplacePath<T>(this JToken root, string path, T newValue)
    {
        if (root == null || path == null)
            throw new ArgumentNullException();

        foreach (var value in root.SelectTokens(path).ToList())
        {
            if (value == root)
                root = JToken.FromObject(newValue!);
            else
                value.Replace(JToken.FromObject(newValue!));
        }

        return root;
    }    

    public static string ReplacePath<T>(string jsonString, string path, T newValue)
    {
        return JToken.Parse(jsonString).ReplacePath(path, newValue).ToString();
    } 
}