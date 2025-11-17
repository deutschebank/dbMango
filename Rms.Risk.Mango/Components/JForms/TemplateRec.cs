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
﻿using Newtonsoft.Json.Linq;
using Rms.Risk.Mango.Pivot.UI.Services;

namespace Rms.Risk.Mango.Components.JForms;

public class TemplateRec
{
    public class ParamRec
    {
        public string  Name    { get; init; } = "";
        public string? Icon    { get; init; }
        public string  Type    { get; init; } = "text";
        public string  TypeArg { get; init; } = "";
        public string  Path    { get; init; } = "$";
        public string  Class   { get; init; } = "";
        public string  Text    { get; set;  } = "";
    }

    public Uri? DocUri { get; set; }
    public List<ParamRec> Parameters { get; set; } = [];

    public void UpdateText(string value)
    {
        var root = JsonUtils.FromJson<JToken>(value);
        if (root == null) 
            return;

        foreach (var p in Parameters)
        {
            p.Text = GetText(root, p.Path);
        }
    }

    public string UpdateCommand(string value)
    {
        if (!JsonUtils.IsValidJson(value))
            return value;

        var root = JsonUtils.FromJson<JToken>(value);
        if (root == null)
            return value;

        foreach (var p in Parameters)
        {
            if (p.Type == "json")
                SetObject(root, p.Text, p.Path);
            else if ( p.Type == "select" )
                SetText(root, p.Text, p.Path, p.TypeArg);
            else if ( p.Type != "-" )
                SetText(root, p.Text, p.Path, p.Type);
        }

        return JsonUtils.ToJson(root, new() { WriteIndented = true });
    }

    private static void SetText(JToken root, string value, string path, string typeName)
    {
        if (typeName == "string")
        {
            root.ReplacePath(path, value.Replace("\r", ""));
        }
        else
        {
            try
            {
                var type = GetType(typeName);

                var convertedValue = Convert.ChangeType(value, type);
                root.ReplacePath(path, convertedValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert value to type '{typeName}': {ex.Message}", ex);
            }
        }
    }

    private static Type GetType(string typeName)
    {
        switch (typeName)
        {
            case "text":
            case "string":
                return typeof(string);
            case "zeroone":
            case "int":
                return typeof(int);
            case "long":
                return typeof(long);
            case "bool":
                return typeof(bool);
            case "number":
            case "double":
                return typeof(double);
            case "decimal":
                return typeof(decimal);
            case "DateTime":
                return typeof(DateTime);
        }

        if (string.IsNullOrEmpty(typeName))
            return typeof(string);
        var type = Type.GetType(typeName);
        if (type == null)
            throw new InvalidOperationException($"Type '{typeName}' not found.");
        return type;
    }

    private static void SetObject(JToken root, string value, string path)
    {
        if (!JsonUtils.IsValidJson(value))
            return;

        var obj = JsonUtils.FromJson<JToken>(value.Replace("\r", ""));
        if (obj == null)
            return;

        root.ReplacePath(path, obj);
    }

    private string GetText(JToken root, string path)
    {
        var obj = root.GetOneByPath(path);
        if (obj == null)
            return "";
        if (obj.Type == JTokenType.Object)
            return JsonUtils.ToJson(obj, new() { WriteIndented = true });
        return obj.ToString();
    }
}

