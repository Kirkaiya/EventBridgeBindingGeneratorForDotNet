# EventBridgeBindingGeneratorForDotNet

A quick and dirty .NET Standard 2.0 library that generates all the classes to use as bindings for deserializing EventBridge schemas. This is handy for generating the (sometimes many) classes if you're using a .NET Core Lambda function, for instance, as the target of an [AWS EventBridge rule](https://aws.amazon.com/eventbridge/).
The generator library itself returns a byte array containing a zip file that has all the class files in it. 
In order to demo this, there is also a trivial .NET Core 3.1 console app that passes in the schema as a string, and then writes the resulting zip file to disk.
Depending on free time, I may create a .NET Core serverless app (API Gateway + Lambda) that lets you pick from the available schemas, and then returns the zip file to you.

## Disclaimer

The code itself is sorta ugly, and opportunities for cleanup and improvement. There's a fair bit of string concatenation. More importantly, **I only tested this on some five or six different schemas**, all of which were for AWS events. Also note that there is **no exception handling** (with one exception, no pun intended). And no unit tests!!

## Known issue

For one of the schemas I tested (in file aws.codepipeline@CodePipelineStageExecutionStateChange-v1.json), the detail property's properties.version attribute property is listed as "string" type in the json schema doc. But when I actually used the generated class in a Lambda function that consumes real events, the deserialization barfs, complaining that the actual value is a number (an int).  And in fact, in the actual event json, there are no quotes around the numeric value. So maybe this is an error in AWS's schemas, or something else.  The generated class could work around this by having more complicated getters/setters, and making the appropriate conversion, but it doesn't yet do that. But this is pretty minor.
I've reported the issue to the EventBridge team, and they're looking into it.

## Getting Schemas

This could be extended to pull the schemas directly from the AWS Schema Repository, the way VS Code does, but in the meantime, you can download AWS EventBridge schemas from your AWS account at https://console.aws.amazon.com/events/home?#/schemas?registry=aws.events

I've put a couple of the schemas that I downloaded into the SampleSchemas folder that you can use to test with. This could also be abstracted out as a web app, so that it could be used without having to pull down this code and run it (feel free!)

## Usage

From a command line in the `bin\Debug\netcoreapp3.1` folder:

    BindingGenerator <path | test>

*Example*:

    BindingGenerator C:\foldername\EventBridgeBindingGeneratorForDotNet\SampleSchemas\aws.codebuild@CodeBuildBuildPhaseChange-v1.json

If specify `test` instead of a path, the Program class will fall back to using *`aws.codebuild@CodeBuildBuildPhaseChange-v1.json`*, using a relative path to the SampleSchemas folder. This should work whether whether on Windows or Linux/macOS.

## Output

The output will be all of the generated C# class files in a folder named for the "title" property of the Schema (which is also the class name of the top-level class).  For each complex property in the schema (meaning, not a primitive type), a new class is created (in a new file). For some schemas, this will result in only two files, while for others it might be more than 10.  

As an example, the output of `BindingGenerator test` will create the following files for the schema `aws.codebuild@CodeBuildBuildPhaseChange-v1.json` (other schemas will lead to as few as two files):

 * AdditionalInformationItem.cs
 * CodeBuildBuildPhaseChange.cs
 * CodeBuildBuildPhaseChangeDetail.cs
 * CodeBuildBuildPhaseChangeDetailAdditionalInformation.cs
 * CodeBuildBuildPhaseChangeDetailAdditionalInformationArtifact.cs
 * CodeBuildBuildPhaseChangeDetailAdditionalInformationCache.cs
 * CodeBuildBuildPhaseChangeDetailAdditionalInformationEnvironment.cs
 * CodeBuildBuildPhaseChangeDetailAdditionalInformationLogs.cs
 * CodeBuildBuildPhaseChangeDetailAdditionalInformationNetworkInterface.cs
 * CodeBuildBuildPhaseChangeDetailAdditionalInformationSource.cs
 * CodeBuildBuildPhaseChangeDetailAdditionalInformationSourceAuth.cs
 * CodeBuildBuildPhaseChangeDetailAdditionalInformationVpcConfig.cs
 * EnvironmentItem.cs
 * VpcConfigItem.cs

## Using Generated Classes

I've two sets of generated classes in actual Lambda functions so far (CodeBuildBuildPhaseChange and CodePipelineStageExecutionStateChange), and they both work (with caveat that the second one requires changing "Version" from string to int/float). You can use the class that has the same name as the folder they're written to as the Lambda event type, as in this example, which uses the code generated from schema aws.codepipeline@CodePipelineStageExecutionStateChange-v1.json.

Be sure to copy the generated folder full of generated classes into your project and adjust any namespaces if desired.

```c#
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PipelineStateChangeHandler
{
    public class Function
    {
        public void FunctionHandler(CodePipelineStageExecutionStateChange pipelineEvent, ILambdaContext context)
        {
            LambdaLogger.Log($"Account ID: {pipelineEvent.Account}, Pipeline: {pipelineEvent.Detail.Pipeline}");
            LambdaLogger.Log($"Stage: {pipelineEvent.Detail.Stage}, state: {pipelineEvent.Detail.State}");
            LambdaLogger.Log($"Resources array count: {pipelineEvent.Resources.Length}");
        }
    }
}
```