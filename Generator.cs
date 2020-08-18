using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace BindingGenerator
{
    internal class Generator
    {
        public static JsonDocument Schema;
        private string _schemaClassName;

        public void GenerateCodeFiles(string rawSchemaText)
        {
            Schema = JsonDocument.Parse(rawSchemaText);

            _schemaClassName = Schema.RootElement.GetProperty("info").GetProperty("title").GetString();

            Directory.CreateDirectory(_schemaClassName);

            var properties = Schema.RootElement.GetProperty("components").GetProperty("schemas")
                .GetProperty("AWSEvent").GetProperty("properties");

            WriteClassFile(properties, _schemaClassName);
        }

        private void WriteClassFile(JsonElement properties, string className)
        {
            var sb = new StringBuilder("using System.Text.Json.Serialization;");
            var fixedClassName = Member.GetMemberName(className);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"public class {fixedClassName} {{");

            foreach (var property in properties.EnumerateObject())
            {
                var member = new Member(property, fixedClassName);
                Console.WriteLine(member.ToString());

                if (member.IsComplexType || member.IsArrayOfComplexType) {
                    WriteClassFile(member.ChildNode, member.TypeName);
                }

                sb.AppendLine(member.ToString());
            }

            sb.AppendLine("}");
            Console.WriteLine(sb.ToString());
            File.WriteAllText(Path.Combine(_schemaClassName, $"{className}.cs"), sb.ToString());
        }
    }
}
