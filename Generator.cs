using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace BindingGenerator
{
    internal class Generator
    {
        private JsonDocument _schema;
        private string _schemaClassName;

        public void GenerateCodeFiles(string rawSchemaText)
        {
            _schema = JsonDocument.Parse(rawSchemaText);

            _schemaClassName = _schema.RootElement.GetProperty("info").GetProperty("title").GetString();

            Directory.CreateDirectory(_schemaClassName);

            var properties = _schema.RootElement.GetProperty("components").GetProperty("schemas")
                .GetProperty("AWSEvent").GetProperty("properties");

            WriteClassFile(properties, _schemaClassName);
        }

        private void WriteClassFile(JsonElement properties, string className)
        {
            var sb = new StringBuilder("using System.Text.Json.Serialization;");
            var fixedClassName = GetMemberName(className);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"public class {fixedClassName} {{");

            foreach (var property in properties.EnumerateObject())
            {
                var fields = property.Value.EnumerateObject().Select(jp => ValueTuple.Create(jp.Name, jp.Value));

                var thing = fields.First();
                var typeName = thing.Item2.GetString();

                if (thing.Item1 == "$ref") {
                    var childNodePath = thing.Item2.GetString().TrimStart('#', '"');
                    var childNode = GetElementByPath(childNodePath).GetProperty("properties");
                    typeName = fixedClassName + GetMemberName(property.Name);

                    WriteClassFile(childNode, typeName);
                }

                sb.AppendLine();

                var propName = GetMemberName(property.Name);
                if (property.Name.Contains('-'))
                {
                    sb.AppendLine($"    [JsonPropertyName(\"{property.Name}\")]");
                }

                if (typeName == "array")
                {
                    var arrayinfo = fields.First(x => x.Item1 == "items");

                    var elementsType = arrayinfo.Item2.EnumerateObject();
                    if (elementsType.First().Name == "$ref")
                    {
                        var childNodePath = elementsType.First().Value.GetString().TrimStart('#', '"');
                        var childNode = GetElementByPath(childNodePath).GetProperty("properties");
                        var arrayItemType = GetMemberName(childNodePath.Substring(childNodePath.LastIndexOf('/') + 1));

                        WriteClassFile(childNode, arrayItemType);
                        typeName = arrayItemType + "[]";

                    } else
                    {
                        typeName = arrayinfo.Item2.GetProperty("type").GetString() + "[]";
                    }
                }

                sb.AppendLine($"    public {typeName} {propName} {{ get; set; }}");
            }

            sb.AppendLine("}");
            Console.WriteLine(sb.ToString());
            File.WriteAllText(Path.Combine(_schemaClassName, $"{className}.cs"), sb.ToString());
        }

        private JsonElement GetElementByPath(string path)
        {
            var propNames = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (!propNames.Any()) return new JsonElement();

            var je = _schema.RootElement;

            foreach (var propName in propNames)
            {
                je = je.GetProperty(propName);
            }

            return je;
        }

        private string GetMemberName(string jsonPropName)
        {
            var pieces = jsonPropName.Split("-", StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            foreach (var piece in pieces) {
                sb.Append(char.ToUpperInvariant(piece.First()) + piece.Substring(1));
            }
            
            return sb.ToString();
        }
    }
}
