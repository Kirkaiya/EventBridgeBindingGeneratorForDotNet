using System;
using System.IO;
using System.Linq;
using EventBridgeBindingGenerator;

namespace BindingGenerator
{
    class Program
    {
        const string testFileName = "aws.codebuild@CodeBuildBuildPhaseChange-v1.json";
        private static string testFilePath = @$"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}SampleSchemas{Path.DirectorySeparatorChar}";

        static void Main(string[] args)
        {
            if (!args.Any())
            {
                Console.WriteLine("You must specify the full path to the input file.");
                return;
            }

            var filepath = args[0] == "test" ? Path.Combine(testFilePath, testFileName) : args[0];

            var rawtext = File.ReadAllText(filepath);

            var generator = new Generator();
            string schemaName;

            var zipBytes = generator.GenerateCodeFiles(rawtext, out schemaName);
            using (var fileStream = File.OpenWrite(schemaName + ".zip"))
            {
                fileStream.Write(zipBytes, 0, zipBytes.Length);
            }

            Console.WriteLine("Code generation complete");
        }
    }
}
