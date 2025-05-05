using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.Schemas;
using Amazon.Schemas.Model;
using EventBridgeBindingGenerator;

namespace BindingGenerator
{
    class Program
    {
        const string GeneratedClassesFolderName = "GeneratedClasses";
        const string testFileName = "aws.codebuild@CodeBuildBuildPhaseChange-v1.json";
        private static string testFilePath = @$"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}SampleSchemas{Path.DirectorySeparatorChar}";
        
        private static List<SchemaSummary> SchemaSummaries = new List<SchemaSummary>();
        private static List<string> Schemas = new List<string>();

        static void Main(string[] args)
        {
            if (!args.Any())
            {
                Console.WriteLine("You must specify one of:  test | discovered | <full path to schema file>");
                return;
            }

            string filepath = string.Empty;
            bool useDiscovered = false;
            var client = new AmazonSchemasClient(RegionEndpoint.USWest2);

            switch (args[0])
            {
                case "discovered":
                    PopulateSchemasList("discovered-schemas", null, client);
                    useDiscovered = true;
                    break;
                
                case "test":
                    filepath = Path.Combine(testFilePath, testFileName);
                    break;
                
                default:
                    if (!File.Exists(args[0]))
                    {
                        Console.WriteLine($"File {args[0]} does not exist");
                        return;
                    }
                    filepath = args[0];
                    break;
            }

            if (!useDiscovered)
            {
                Console.WriteLine($"Using schema file {filepath}");
                Schemas.Add(File.ReadAllText(filepath));
            }
            else
            {
                Console.WriteLine("Using discovered schemas");
                foreach (var schema in SchemaSummaries)
                {
                    var schemaText = FetchSchema("discovered-schemas", schema.SchemaName, client);
                    Schemas.Add(schemaText);
                }
            }

            var outPath = @$"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}{GeneratedClassesFolderName}{Path.DirectorySeparatorChar}";
            if (!Directory.Exists(outPath))
                Directory.CreateDirectory(outPath);

            foreach (var schemaText in Schemas)
            {
                var generator = new Generator();
                Console.WriteLine($"Generating code for schema {schemaText}");

                // To get all the classes together in a zip file, uncomment below, and comment out the block that writes the files directly
                //var zipBytes = generator.GenerateCodeFiles(schemaText, out string schemaName);
                //using (var fileStream = File.OpenWrite(Path.Combine(outPath, schemaName + ".zip")))
                //{
                //    fileStream.Write(zipBytes, 0, zipBytes.Length);
                //}

                var classes = generator.GenerateCodeFilesAsStrings(schemaText, out string schemaName);
                foreach (var classContainer in classes)
                {
                    WriteClassAndChildClassesToFile(classContainer, outPath);
                }
            }

            Console.WriteLine("Code generation complete");
        }

        private static void WriteClassAndChildClassesToFile(ClassContainer classContainer, string outPath)
        {
            var fileName = classContainer.ClassFileName;
            File.WriteAllText(Path.Combine(outPath, fileName), classContainer.ClassDefinitionAsString);
            foreach (var childClass in classContainer.ChildClasses)
            {
                WriteClassAndChildClassesToFile(childClass, outPath);
            }
        }

        private static void PopulateSchemasList(string schemaRegistryName, string filter, IAmazonSchemas schemasClient)
        {
            var request = new ListSchemasRequest { RegistryName = schemaRegistryName, SchemaNamePrefix = filter };
            var response = schemasClient.ListSchemasAsync(request).Result;

            SchemaSummaries.AddRange(response.Schemas);
        }

        private static string FetchSchema(string schemaRegistryName, string schemaName, IAmazonSchemas schemasClient)
        {
            var request = new DescribeSchemaRequest { RegistryName = schemaRegistryName, SchemaName = schemaName };
            var response = schemasClient.DescribeSchemaAsync(request).Result;
            var schema = response.Content;

            return schema;
        }
    }
}
