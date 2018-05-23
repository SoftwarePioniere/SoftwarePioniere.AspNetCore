#load "./scripts/utils.cake"
#load "./scripts/gitversion.cake"
#load "./scripts/dotnet.cake"
#load "./scripts/git.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
var target          = Argument("target", "Default");
var configuration   = Argument("configuration", "Release");
var isDryRun        = HasArgument("dryrun1");

///////////////////////////////////////////////////////////////////////////////
// VARIABLES
///////////////////////////////////////////////////////////////////////////////

var artifactsDirectory  = Directory("./artifacts");
var version             = "0.0.0";
var solutionFile        = File("./SoftwarePioniere.AspNetCore.sln");
var nugetApiKey         = "VSTS";

///////////////////////////////////////////////////////////////////////////////
// SETUP/TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{         
    vstsToken           = EnvironmentVariable("VSTS_TOKEN") ?? vstsToken;
    nugetApiKey         = EnvironmentVariable("NUGET_API_KEY") ?? nugetApiKey;

    if (IsTfs(context)) {
        vstsToken = EnvironmentVariable("SYSTEM_ACCESSTOKEN");
        if (string.IsNullOrEmpty(vstsToken))
            throw new System.InvalidOperationException("Please allow VSTS Token Access");
    }

    MyGitVersion.Init(context); 
    MyDotNet.Init(context, configuration, isDryRun, packageSource, vstsToken, nugetApiKey);
  
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Version")
 .Does((context) =>
{    
    version = MyGitVersion.Calculate(); 
    Information("Version: {0}", version);
    SetBuildNumber(context, version);

    MyGitVersion.WriteArtifacts(artifactsDirectory); 
});

Task("Clean")
    .Does(() =>
{
    CleanDirectories(new DirectoryPath[] { artifactsDirectory });
});


Task("Restore")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")
    .Does(context =>
{
    MyDotNet.RestoreSolution(solutionFile);            
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")
    .IsDependentOn("Restore")
    .Does(context =>
{

    MyDotNet.BuildSolution(solutionFile);   
   
});


Task("Test")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .Does(context =>
{
    // MyDotNet.TestProjects("./test/**/*.csproj");
 
});

Task("DockerBuild")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")    
    .Does(context =>
{
    MyDotNet.DockerBuild(image);
  
});

Task("DockerPush")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")    
    .Does(context =>
{
    MyDotNet.DockerPush(image);  
});


///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Default");


Task("BuildTestPackLocalPush")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack")
    .IsDependentOn("PushPackagesLocal")
    ;


Task("DockerBuildPush") 
    .IsDependentOn("Clean")
    .IsDependentOn("Version")
    .IsDependentOn("DockerBuild")
    .IsDependentOn("DockerPack")
    .IsDependentOn("DockerPushPackages")
    ;

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);