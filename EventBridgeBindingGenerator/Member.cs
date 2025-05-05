using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace EventBridgeBindingGenerator
{
    public class Member
    {
        JsonProperty _jprop;

        public Member(JsonProperty jprop, string className)
        {
            _jprop = jprop;
            MemberName = _jprop.Name;

            var fields = jprop.Value.EnumerateObject().Select(jp => new KeyValuePair<string, JsonElement>(jp.Name, jp.Value));
            var firstKey = fields.First().Key;
            var firstVal = fields.First().Value;

            IsComplexType = firstKey == "$ref";

            if (firstKey == "type")
            {
                //primitive
                if (firstVal.GetString() == "array")
                {
                    //array, get type of items and set as TypeName (with [])
                    IsArray = true;
                    var arrayVal = fields.First(x => x.Key == "items").Value;
                    var elementsType = arrayVal.EnumerateObject();
                    if (elementsType.First().Name == "$ref")
                    {
                        //array items are of complex type
                        IsArrayOfComplexType = true;
                        var childNodePath = elementsType.First().Value.GetString().TrimStart('#', '"');

                        if (GetElementByPath(childNodePath).TryGetProperty("properties", out var childNodeProps)) {
                            ChildNode = childNodeProps;
                            TypeName = GetMemberName(childNodePath.Substring(childNodePath.LastIndexOf('/') + 1));
                        } 
                        else if (GetElementByPath(childNodePath).TryGetProperty("additionalProperties", out var additionalProps))
                        {
                            TypeName = "dynamic";
                        }

                    } else {
                        TypeName = arrayVal.GetProperty("type").GetString();
                    }

                } else
                {
                    var isNullable = fields.FirstOrDefault(x => x.Key == "nullable").Key != null;
                    TypeName = firstVal.GetString() + (isNullable ? "?" : string.Empty);
                }
            }

            if (firstKey == "$ref")
            {
                //complex - create a new class for this type
                var childNodePath = firstVal.GetString().TrimStart('#', '"');
                ChildNode = GetElementByPath(childNodePath).GetProperty("properties");
                TypeName = className + GetMemberName(MemberName);
            }
        }

        public string TypeName { get; set; }

        public string MemberName { get; private set; }

        public bool IsArray { get; private set; }

        public bool IsComplexType { get; private set; }

        public bool IsArrayOfComplexType { get; private set; }

        public JsonElement ChildNode { get; private set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (MemberName.Contains("-"))
                sb.AppendLine($"    [JsonPropertyName(\"{MemberName}\")]");

            var brackets = IsArray ? "[]" : string.Empty;

            sb.AppendLine($"    public {GetCsharpTypeName()}{brackets} {GetMemberName(MemberName)} {{ get; set; }}");

            return sb.ToString();
        }

        private JsonElement GetElementByPath(string path)
        {
            var propNames = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (!propNames.Any()) throw new Exception("Element path is not valid!");

            var je = Generator.Schema.RootElement;

            foreach (var propName in propNames)
            {
                je = je.GetProperty(propName);
            }

            return je;
        }

        private string GetCsharpTypeName()
        {
            switch (TypeName)
            {
                case "string":
                    return "string";
                case "boolean":
                    return "bool";
                case "number":
                    return "float";
                default:
                    return TypeName;
            }
        }

        public static string GetMemberName(string jsonPropName)
        {
            if (jsonPropName.IndexOf('.') > 0)
            {
                var fixedMemberName = Generator.GetNormalizedClassOrMemberName(jsonPropName);
                return fixedMemberName;
            }

            var pieces = jsonPropName.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            foreach (var piece in pieces)
            {
                sb.Append(char.ToUpperInvariant(piece.First()) + piece.Substring(1));
            }

            return sb.ToString();
        }
    }
}
