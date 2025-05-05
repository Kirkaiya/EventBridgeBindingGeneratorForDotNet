using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace EventBridgeBindingGenerator
{
    public class Generator
    {
        public static JsonDocument Schema;
        //private Dictionary<string, string> classesByFilename = new Dictionary<string, string>();
        private List<ClassContainer> Classes = new List<ClassContainer>();

        private string _schemaClassName;

        public byte[] GenerateCodeFiles(string rawSchemaText, out string schemaClassName)
        {
            Schema = JsonDocument.Parse(rawSchemaText);
            _schemaClassName = GetNormalizedClassOrMemberName(Schema.RootElement.GetProperty("info").GetProperty("title").GetString());

            var properties = Schema.RootElement.GetProperty("components")
                .GetProperty("schemas")
                .GetProperty("AWSEvent")
                .GetProperty("properties");

            CreateClass(properties, _schemaClassName);

            schemaClassName = _schemaClassName;

            return createZipFileInMemory();
        }

        public List<ClassContainer> GenerateCodeFilesAsStrings(string rawSchemaText, out string schemaClassName)
        {
            Schema = JsonDocument.Parse(rawSchemaText);
            _schemaClassName = GetNormalizedClassOrMemberName(Schema.RootElement.GetProperty("info").GetProperty("title").GetString());

            var properties = Schema.RootElement.GetProperty("components")
                .GetProperty("schemas")
                .GetProperty("AWSEvent")
                .GetProperty("properties");

            CreateClass(properties, _schemaClassName);

            schemaClassName = _schemaClassName;

            return Classes;
        }

        internal static string GetNormalizedClassOrMemberName(string className)
        {
            if (className.IndexOf('.') > 0)
            {
                var pieces = className.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();

                for (var i = 0; i < pieces.Count; i++)
                    pieces[i] = pieces[i][0].ToString().ToUpper() + pieces[i][1..];

                className = string.Join("", pieces);
            }

            var cleanName = className.Replace(".", string.Empty)
                .Replace('-', '_')
                .Replace(' ', '_');

            return cleanName[..1].ToUpper() + className[1..];
        }

        private void CreateClass(JsonElement properties, string className, ClassContainer parentClass = null)
        {
            var sb = new StringBuilder("using System.Text.Json.Serialization;");
            var fixedClassName = Member.GetMemberName(className);
            var classContainer = new ClassContainer { ClassName = fixedClassName };

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"public class {fixedClassName} {{");
            sb.AppendLine();

            foreach (var property in properties.EnumerateObject())
            {
                var member = new Member(property, fixedClassName);
                Console.WriteLine(member.ToString());

                if (member.TypeName != "dynamic" && (member.IsComplexType || member.IsArrayOfComplexType))
                {
                    CreateClass(member.ChildNode, GetNormalizedClassOrMemberName(member.TypeName), classContainer);
                }

                sb.AppendLine(member.ToString());
            }

            sb.AppendLine("}");
            classContainer.ClassDefinitionAsString = sb.ToString();

            if (parentClass != null)
            {
                parentClass.ChildClasses.Add(classContainer);
            }
            else
            {
                Classes.Add(classContainer);
            }
        }

        private byte[] createZipFileInMemory()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var classContainer in Classes)
                    {
                        WriteEntry(classContainer, zipArchive);
                    }
                }

                memoryStream.Position = 0;
                var buffer = new byte[memoryStream.Length];
                memoryStream.Read(buffer, 0, buffer.Length);

                return buffer;
            }
        }

        private void WriteEntry(ClassContainer classContainer, ZipArchive zipArchive)
        {
            var fullPath = Path.Combine(_schemaClassName, classContainer.ClassFileName);
            var entry = zipArchive.CreateEntry(fullPath, CompressionLevel.Fastest);
            
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(classContainer.ClassDefinitionAsString);
            }

            foreach (var childClass in classContainer.ChildClasses)
            {
                WriteEntry(childClass, zipArchive);
            }
        }
    }
}
