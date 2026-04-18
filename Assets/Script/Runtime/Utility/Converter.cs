using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using UnityEngine;

public static class Converter
{

#region JSON Schema
    static readonly StringBuilder GrammarBuilder = new();
    static readonly HashSet<string> DefinedRules = new();

    /// <summary>
    /// Converts a JSON Schema into GBNF grammar string.
    /// </summary>
    /// <param name="jsonSchema">The JSON schema as a string.</param>
    /// <param name="rootClassName">The root rule name.</param>
    /// <returns>GBNF grammar as a string.</returns>
    public static string JsonSchemaToGBNF(string jsonSchema, string rootClassName = "RootClass")
    {
        using var doc = JsonDocument.Parse(jsonSchema);

        GrammarBuilder.Clear();
        DefinedRules.Clear();

        string rootRule = ProcessSchema(doc.RootElement, rootClassName);
        GrammarBuilder.AppendLine(@$"root ::= {rootRule}");

        DefineBasicTypes();

        return GrammarBuilder.ToString();
    }

    /// <summary>
    /// Defines shared GBNF primitives like string, boolean, etc.
    /// </summary>
    static void DefineBasicTypes()
    {
        if (!DefinedRules.Contains("ws"))
        {
            GrammarBuilder.AppendLine(@"ws ::= [ \t\n]*");
            DefinedRules.Add("ws");
        }

        if (!DefinedRules.Contains("string"))
        {
            GrammarBuilder.AppendLine(@"string ::= ""\""""   ([^""]*)   ""\""""");
            DefinedRules.Add("string");
        }

        if (!DefinedRules.Contains("integer"))
        {
            GrammarBuilder.AppendLine(@"integer ::= ""-""? ([0-9] | [1-9] [0-9]*)");
            DefinedRules.Add("integer");
        }

        if (!DefinedRules.Contains("number"))
        {
            GrammarBuilder.AppendLine(@"number ::= integer (""."" [0-9]+)? ([eE] [-+]? [0-9]+)?");
            DefinedRules.Add("number");
        }

        if (!DefinedRules.Contains("boolean"))
        {
            GrammarBuilder.AppendLine(@"boolean ::= ""true"" | ""false""");
            DefinedRules.Add("boolean");
        }
    }

    /// <summary>
    /// Processes a schema node and returns its rule name for use in other rules.
    /// </summary>
    static string ProcessSchema(JsonElement schema, string ruleName)
    {
        StringBuilder ruleBuilder = new();

        if (schema.TryGetProperty("type", out var typeElement))
        {
            string type = typeElement.GetString();

            switch (type)
            {
                case "object":
                    ruleBuilder.Append(ruleName);
                    ProcessObject(schema, ruleName);
                    break;
                case "array":
                    ruleBuilder.Append($"{ruleName}list");
                    ProcessArray(schema, ruleName);
                    break;
                case "string":
                    ruleBuilder.Append("string");
                    break;
                case "integer":
                    ruleBuilder.Append("integer");
                    break;
                case "number":
                    ruleBuilder.Append("number");
                    break;
                case "boolean":
                    ruleBuilder.Append("boolean");
                    break;
                default:
                    ruleBuilder.Append(@"""null""");
                    break;
            }
        }

        return ruleBuilder.ToString();
    }

    /// <summary>
    /// Processes a JSON object schema and defines a GBNF rule for it.
    /// </summary>
    static void ProcessObject(JsonElement schema, string ruleName)
    {
        if (schema.TryGetProperty("properties", out var properties))
        {
            var fieldRules = new List<string>();

            foreach (var prop in properties.EnumerateObject())
            {
                string propertyName = prop.Name;
                string propertyRuleName = ProcessSchema(prop.Value, propertyName);

                string rule = @$"   ws   ""\""{propertyName}\"":""  ws   {propertyRuleName}   "; 
                fieldRules.Add(rule);
            }

            string combinedFieldsRule = string.Join(@""",""", fieldRules);
            GrammarBuilder.AppendLine(@$"{ruleName} ::= ""{{""{combinedFieldsRule}""}}""");
        }
    }

    /// <summary>
    /// Processes an array schema and defines a GBNF rule for it.
    /// </summary>
    static void ProcessArray(JsonElement schema, string ruleName)
    {
        if (schema.TryGetProperty("items", out var items))
        {
            string itemRuleName = ProcessSchema(items, ruleName);
            string itemListRuleName = $"{ruleName}list";

            GrammarBuilder.AppendLine(@$"{itemListRuleName} ::= ""[]"" | ""[""   ws   {itemRuleName}   ("",""   ws   {itemRuleName})*   ""]""");
        }
    }
#endregion

#region Texture
    public static Texture2D Base64StringToTexture(string base64Image)
    {
        byte[] imageBytes = Convert.FromBase64String(base64Image);

        Texture2D texture = new(2, 2);
        texture.LoadImage(imageBytes);
        texture.Apply();

        return texture;
    }

    public static List<Texture2D> Base64StringToTexture(List<string> base64Images)
    {
        List<Texture2D> textures = new();

        foreach (string base64Image in base64Images)
        {
            Texture2D texture = Base64StringToTexture(base64Image);
            textures.Add(texture);
        }

        return textures;
    }

    public static string TextureToBase64String(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToPNG();
        string base64Image = Convert.ToBase64String(imageBytes);

        return base64Image;
    }

    public static List<string> TextureToBase64String(List<Texture2D> textures)
    {
        List<string> base64Images = new();

        foreach (Texture2D texture in textures)
        {
            string base64Image = TextureToBase64String(texture);
            base64Images.Add(base64Image);
        }

        return base64Images;
    }
#endregion

}
