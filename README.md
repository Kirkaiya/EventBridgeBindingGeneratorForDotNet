# EventBridgeBindingGeneratorForDotNet

A quick and dirty (very hacky!!) .NET Core console app that generates all the classes to use as bindings for deserializing EventBridge schemas.

## Disclaimer

I stopped editing the code as soon as it worked - so it's ugly, and has more opportunities for cleanup and improvement than one might expect from such a simple app.  Yes, I reassign values to the same strings. Yes, I have the same code in multiple places that could be abstracted out. The entire thing is just a bunch of string concatenation. I know, I know. More importantly, **I only tested this on some five or six different schemas**.

Also note that there is **no exception handling**. If there's an error, you'll see it ;-)

### Known issue

For one of the schemas I tested (in file aws.codepipeline@CodePipelineStageExecutionStateChange-v1.json), the "detail.properties.version" property is listed as "string" type in the json schema doc. But when I actually used the generated class in a Lambda function that consumes real events, the deserialization barfs, complaining that the actual value is an int (which it is, in the actual event json).  The generated class could work around this by having more complicated getters/setters, and making the appropriate conversion, but it doesn't yet do that. But this is pretty minor.

## Getting Schemas

This could be extended to pull the schemas directly from the AWS Schema Repository, the way VS Code does, but in the meantime, you can download AWS EventBridge schemas from your AWS account at https://console.aws.amazon.com/events/home?#/schemas?registry=aws.events

I've put a couple of the schemas that I downloaded into the SampleSchemas folder that you can use to test with. This could also be abstracted out as a web app, so that it could be used without having to pull down this code and run it (feel free!)



## Usage

From a command line in the `bin\Debug\netcoreapp3.1` folder:
   
    BindingGenerator <path | test>

*Example*:
    `BindingGenerator C:\foldername\EventBridgeBindingGeneratorForDotNet\SampleSchemas\CodePipelineStageExecutionStateChange.json`

If specify `test` instead of a path, the Program class will fall back to using *aws.codebuild@CodeBuildBuildPhaseChange-v1.json*, using a relative path to the SampleSchemas folder. This should work whether whether on Windows or Linux/macOS.

## Output

The output will be all of the generated C# class files in a folder named for the "title" property of the Schema (which is also the class name of the top-level class).  For each complex property in the schema (meaning, not a primitive type), a new class is created (in a new file). For some schemas, this will result in only two files, while for others it might be more than 10.  

As an example, the output of `BindingGenerator test` will create the following files:

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