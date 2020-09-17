using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace EventBridgeBindingGenerator
{
    public class Generator
    {
        public static JsonDocument Schema;
        private Dictionary<string, string> classesByFilename = new Dictionary<string, string>();

        private string _schemaClassName;

        public byte[] GenerateCodeFiles(string rawSchemaText, out string schemaClassName)
        {
            Schema = JsonDocument.Parse(rawSchemaText);

            _schemaClassName = Schema.RootElement.GetProperty("info").GetProperty("title").GetString();

            //Directory.CreateDirectory(_schemaClassName);

            var properties = Schema.RootElement.GetProperty("components").GetProperty("schemas")
                .GetProperty("AWSEvent").GetProperty("properties");

            CreateClass(properties, _schemaClassName);

            schemaClassName = _schemaClassName;

            return createZipFileInMemory();
        }

        private void CreateClass(JsonElement properties, string className)
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
                    CreateClass(member.ChildNode, member.TypeName);
                }

                sb.AppendLine(member.ToString());
            }

            sb.AppendLine("}");
            //File.WriteAllText(Path.Combine(_schemaClassName, filename), sb.ToString());
            classesByFilename.Add($"{className}.cs", sb.ToString());
        }

        private byte[] createZipFileInMemory()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var item in classesByFilename)
                    {
                        var fullPath = Path.Combine(_schemaClassName, item.Key);
                        var entry = zipArchive.CreateEntry(fullPath, CompressionLevel.Fastest);

                        using (var writer = new StreamWriter(entry.Open()))
                        {
                            writer.Write(item.Value);
                        }
                    }
                }

                memoryStream.Position = 0;
                var buffer = new byte[memoryStream.Length];
                memoryStream.Read(buffer, 0, buffer.Length);

                return buffer;
            }
        }
    }
}
