module TickSepc.Build.Tests.BindingVerificationTests

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Security
open System.Text.Json
open System.Xml.Linq
open NUnit.Framework
open TickSpec
open TickSpec.Build
open TickSpec.CodeGen

let private repositoryRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", ".."))

let private runDotnet folder (arguments:string list) (environmentValue:string option) =
    let startInfo = ProcessStartInfo("dotnet")
    startInfo.WorkingDirectory <- folder
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    for argument in arguments do
        startInfo.ArgumentList.Add(argument)
    startInfo.Environment.Remove("TickSpecBuildVerifyBindings") |> ignore
    environmentValue |> Option.iter(fun value -> startInfo.Environment["TickSpecBuildVerifyBindings"] <- value)

    use child = new Process()
    child.StartInfo <- startInfo
    if child.Start() |> not then
        failwith "Could not start dotnet"
    let stdout = child.StandardOutput.ReadToEndAsync()
    let stderr = child.StandardError.ReadToEndAsync()
    if child.WaitForExit(120000) |> not then
        child.Kill(true)
        child.WaitForExit()
        failwith ("dotnet timed out: " + String.concat " " arguments)
    child.ExitCode, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult()

let private reference (assembly:Assembly) =
    let name = assembly.GetName().Name
    let path = SecurityElement.Escape(assembly.Location)
    $"""<Reference Include="{name}"><HintPath>{path}</HintPath></Reference>"""

let private buildFeaturesWithFramework (framework:string option) (runtimeConfiguration:string option) (features:(string * string) list) (propertyValue:string option) (environmentValue:string option) =
    let folder = Path.Combine(Path.GetTempPath(), "TickSpec.Build.Tests." + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(folder) |> ignore
    try
        let references =
            [ typeof<AbstractFeature>.Assembly; typeof<StepDefinitions>.Assembly; typeof<TestAttribute>.Assembly ]
            |> List.map reference
            |> String.concat Environment.NewLine
        let targets = Path.Combine(repositoryRoot, "build", "TickSpec.Build.targets")
        let tool = SecurityElement.Escape(typeof<Feature>.Assembly.Location)
        let property =
            propertyValue
            |> Option.map(fun value -> $"<TickSpecBuildVerifyBindings>{value}</TickSpecBuildVerifyBindings>")
            |> Option.defaultValue ""
        let output = Path.Combine("bin", "output with spaces") + string Path.DirectorySeparatorChar
        let runtimeProperty =
            runtimeConfiguration
            |> Option.map(fun value -> $"<GenerateRuntimeConfigurationFiles>{value}</GenerateRuntimeConfigurationFiles>")
            |> Option.defaultValue ""
        let frameworkReference =
            framework
            |> Option.map(fun value -> $"""<FrameworkReference Include="{value}" />""")
            |> Option.defaultValue ""
        let frameworkTypes =
            framework
            |> Option.map(fun _ -> """
type Controller() =
    inherit Microsoft.AspNetCore.Mvc.ControllerBase()
            """)
            |> Option.defaultValue ""
        let project =
            $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>BindingProbe.Tests</AssemblyName>
    <OutputPath>{output}</OutputPath>
    <NuGetAudit>false</NuGetAudit>
    {property}
    {runtimeProperty}
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Steps.fs" />
    <ProjectReference Include="Shared\Shared.fsproj" />
    <PackageReference Update="FSharp.Core" Version="10.1.402" />
    {references}
  </ItemGroup>
  <Import Project="{SecurityElement.Escape(targets)}" />
  <PropertyGroup>
    <TickSpecBuildAssembly>dotnet &quot;{tool}&quot;</TickSpecBuildAssembly>
  </PropertyGroup>
</Project>"""
        let sharedFolder = Path.Combine(folder, "Shared")
        Directory.CreateDirectory(sharedFolder) |> ignore
        File.WriteAllText(Path.Combine(sharedFolder, "Shared.fsproj"), $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>BindingProbe.Shared</AssemblyName>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Steps.fs" />
    <PackageReference Update="FSharp.Core" Version="10.1.402" />
    {frameworkReference}
    {references}
  </ItemGroup>
</Project>""")
        File.WriteAllText(Path.Combine(sharedFolder, "Steps.fs"), """
namespace BindingProbe
open TickSpec

type SharedSteps =
    [<Given("a shared step")>]
    static member Shared() : unit = failwith "Shared steps must not execute during verification"
        """ + frameworkTypes)
        File.WriteAllText(Path.Combine(folder, "Steps.fs"), """
namespace BindingProbe
open TickSpec

type Steps =
    static member SharedBindingType = typeof<SharedSteps>

    [<BeforeScenario>]
    static member Before() : unit = failwith "Hooks must not execute during verification"

    [<AfterScenario>]
    static member After() : unit = failwith "Hooks must not execute during verification"

    [<Given("a bound step")>]
    static member Bound() : unit = failwith "Steps must not execute during verification"

    [<Given("an ambiguous step")>]
    static member First() : unit = failwith "Steps must not execute during verification"

    [<Given("an ambiguous step")>]
    static member Second() : unit = failwith "Steps must not execute during verification"

    [<Given("number (.*)")>]
    static member Number(value:int) : unit = failwith "Steps must not execute during verification"
        """)
        let projectFile = Path.Combine(folder, "Consumer.fsproj")
        File.WriteAllText(projectFile, project)
        for file, text in features do
            let path = Path.Combine(folder, file)
            Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
            File.WriteAllText(path, text)

        runDotnet folder
            [ "build"; projectFile; "--nologo"; "--verbosity"; "minimal"; "--disable-build-servers" ]
            environmentValue
    finally
        Directory.Delete(folder, true)

let private buildFeatures features propertyValue environmentValue =
    buildFeaturesWithFramework None None features propertyValue environmentValue

let private build (step:string) propertyValue environmentValue =
    let feature = $"Feature: Binding checks\nScenario: Verify a step\nGiven {step}\n"
    buildFeatures [ "Probe.feature", feature ] propertyValue environmentValue

[<TestCase("a bound step")>]
[<TestCase("a shared step")>]
[<TestCase("number not-an-integer")>]
let ``Resolvable bindings pass without executing scenarios`` step =
    let exitCode, output = build step (Some "true") None
    Assert.That(exitCode, Is.EqualTo(0), output)
    Assert.That(output, Does.Contain("Verified bindings for 1 scenarios in 1 feature files."))

[<TestCase("an unknown step", "Missing step definition")>]
[<TestCase("an ambiguous step", "Ambiguous step definition")>]
let ``Invalid bindings fail opted-in builds`` step (error:string) =
    let exitCode, output = build step (Some "true") None
    Assert.That(exitCode, Is.Not.EqualTo(0), output)
    Assert.That(output, Does.Contain(error))
    Assert.That(output, Does.Contain("Probe.feature"))
    Assert.That(output, Does.Not.Contain("Verified bindings for"))

[<Test>]
let ``Binding verification is disabled by default``() =
    let exitCode, output = build "an unknown step" None None
    Assert.That(exitCode, Is.EqualTo(0), output)
    Assert.That(output, Does.Not.Contain("Verifying step bindings"))

[<Test>]
let ``Environment variable enables binding verification``() =
    let exitCode, output = build "an unknown step" None (Some "true")
    Assert.That(exitCode, Is.Not.EqualTo(0), output)
    Assert.That(output, Does.Contain("Missing step definition"))

[<Test>]
let ``Project property can disable verification enabled by environment``() =
    let exitCode, output = build "an unknown step" (Some "false") (Some "true")
    Assert.That(exitCode, Is.EqualTo(0), output)
    Assert.That(output, Does.Not.Contain("Verifying step bindings"))

[<Test>]
let ``Verification without runtime configuration still supports ordinary libraries``() =
    let feature = "Feature: Ordinary bindings\nScenario: Bound\nGiven a bound step\n"
    let exitCode, output =
        buildFeaturesWithFramework None (Some "false")
            [ "Probe.feature", feature ] (Some "true") None
    Assert.That(exitCode, Is.EqualTo(0), output)
    Assert.That(output, Does.Contain("Verified bindings for 1 scenarios in 1 feature files."))

[<Test>]
let ``Verification uses shared ASPNET Core runtime dependencies``() =
    let feature = "Feature: Framework bindings\nScenario: Bound\nGiven a shared step\n"
    let exitCode, output =
        buildFeaturesWithFramework (Some "Microsoft.AspNetCore.App") None
            [ "Probe.feature", feature ] (Some "true") None
    Assert.That(exitCode, Is.EqualTo(0), output)
    Assert.That(output, Does.Contain("Verified bindings for 1 scenarios in 1 feature files."))

[<Test>]
let ``Framework-aware verification still rejects missing bindings``() =
    let feature = "Feature: Framework bindings\nScenario: Missing\nGiven an unknown step\n"
    let exitCode, output =
        buildFeaturesWithFramework (Some "Microsoft.AspNetCore.App") None
            [ "Probe.feature", feature ] (Some "true") None
    Assert.That(exitCode, Is.Not.EqualTo(0), output)
    Assert.That(output, Does.Contain("Missing step definition"))
    Assert.That(output, Does.Not.Contain("Unable to load step-binding types"))
    Assert.That(output, Does.Not.Contain("Verified bindings for"))

[<Test>]
let ``Unresolved framework types report build-visible loader errors``() =
    let feature = "Feature: Framework bindings\nScenario: Bound\nGiven a shared step\n"
    let exitCode, output =
        buildFeaturesWithFramework (Some "Microsoft.AspNetCore.App") (Some "false")
            [ "Probe.feature", feature ] (Some "true") None
    Assert.That(exitCode, Is.Not.EqualTo(0), output)
    Assert.That(output, Does.Match(@"(?m)^(?:EXEC : error :|ERROR:).*Microsoft\.AspNetCore\.Mvc\.Core"))
    Assert.That(output, Does.Not.Contain("Verified bindings for"))

[<Test>]
let ``Background bindings are verified``() =
    let feature = """Feature: Background bindings
Background:
    Given an unknown step
Scenario: Bound
    Given a bound step
    """
    let exitCode, output = buildFeatures [ "Probe.feature", feature ] (Some "true") None
    Assert.That(exitCode, Is.Not.EqualTo(0), output)
    Assert.That(output, Does.Contain("Missing step definition"))

[<Test>]
let ``Every scenario outline example is verified``() =
    let feature = """Feature: Outline bindings
Scenario Outline: Expand every example
    Given a <binding> step
Examples:
    | binding |
    | bound   |
    | unknown |
    """
    let exitCode, output = buildFeatures [ "Probe.feature", feature ] (Some "true") None
    Assert.That(exitCode, Is.Not.EqualTo(0), output)
    Assert.That(output, Does.Contain("Missing step definition"))

[<Test>]
let ``Every embedded feature is verified including subfolders``() =
    let features =
        [
            "Valid.feature", "Feature: Valid\nScenario: Bound\nGiven a bound step\n"
            Path.Combine("Nested", "Invalid.feature"), "Feature: Invalid\nScenario: Missing\nGiven an unknown step\n"
        ]
    let exitCode, output = buildFeatures features (Some "true") None
    Assert.That(exitCode, Is.Not.EqualTo(0), output)
    Assert.That(output, Does.Contain("Missing step definition"))
    Assert.That(output, Does.Contain("Nested.Invalid.feature"))

[<Test>]
let ``Missing assembly is an explicit error``() =
    let missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dll")
    Assert.That(Action(fun () -> BindingVerifier.Verify missing), Throws.TypeOf<FileNotFoundException>())

[<Test>]
let ``Assembly without embedded features is an explicit error``() =
    Assert.That(
        Action(fun () -> BindingVerifier.Verify typeof<Feature>.Assembly.Location),
        Throws.TypeOf<InvalidOperationException>().With.Message.Contains("No embedded .feature resources"))

[<Test>]
let ``NuGet package restores runtime binding dependencies transitively``() =
    let specification = XDocument.Load(Path.Combine(repositoryRoot, "build", "TickSpec.Build.nuspec"))
    let runtimeProject = XDocument.Load(Path.Combine(repositoryRoot, "src", "TickSpec.CodeGen", "TickSpec.CodeGen.fsproj"))
    let framework = runtimeProject.Descendants(XName.Get "TargetFramework") |> Seq.exactlyOne |> fun x -> x.Value
    let group =
        specification.Descendants(XName.Get "group")
        |> Seq.filter(fun x -> x.Attribute(XName.Get "targetFramework").Value = framework)
        |> Seq.exactlyOne
    let dependencies =
        group.Elements(XName.Get "dependency")
        |> Seq.map(fun x -> x.Attribute(XName.Get "id").Value, x.Attribute(XName.Get "version").Value)
        |> Map.ofSeq
    let references =
        runtimeProject.Descendants(XName.Get "PackageReference")
        |> Seq.map(fun x ->
            let id =
                match x.Attribute(XName.Get "Include") with
                | null -> x.Attribute(XName.Get "Update").Value
                | attribute -> attribute.Value
            id, x.Attribute(XName.Get "Version").Value)
        |> Map.ofSeq
    Assert.That(dependencies, Is.EqualTo<Map<string,string>>(references), "Runtime dependencies must match the runtime project's package references")

    let folder = Path.Combine(Path.GetTempPath(), "TickSpec.Build.PackageTests." + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(folder) |> ignore
    try
        let metadata = specification.Root.Element(XName.Get "metadata")
        let packageId = metadata.Element(XName.Get "id").Value
        let packageVersion = "0.0.0-tests.g" + Guid.NewGuid().ToString("N")
        metadata.Element(XName.Get "version").Value <- packageVersion
        let nuspec = Path.Combine(folder, "Package.nuspec")
        specification.Save(nuspec)
        let feed = Path.Combine(folder, "feed")
        let pack specificationPath outputFolder =
            runDotnet repositoryRoot
                [
                    "pack"; Path.Combine(repositoryRoot, "src", "TickSpec.Build", "TickSpec.Build.fsproj")
                    "--configuration"; "Release"; "--no-restore"; "--output"; outputFolder
                    "-p:NuspecFile=" + specificationPath
                    "-p:NuspecBasePath=" + Path.Combine(repositoryRoot, "build")
                    "--nologo"; "--verbosity"; "minimal"; "--disable-build-servers"
                ] None
        let packExitCode, packOutput = pack nuspec feed
        Assert.That(packExitCode, Is.EqualTo(0), packOutput)
        Assert.That(packOutput, Does.Not.Contain("NU5100"))
        Assert.That(packOutput, Does.Not.Contain("NU5128"))

        group.Remove()
        let invalidNuspec = Path.Combine(folder, "Invalid.nuspec")
        specification.Save(invalidNuspec)
        let invalidExitCode, invalidOutput = pack invalidNuspec (Path.Combine(folder, "invalid-feed"))
        Assert.That(invalidExitCode, Is.Not.EqualTo(0), invalidOutput)
        Assert.That(invalidOutput, Does.Contain("NU5128"))
        Assert.That(invalidOutput, Does.Not.Contain("NU5100"))

        let consumer = Path.Combine(folder, "consumer")
        Directory.CreateDirectory(consumer) |> ignore
        let packages = SecurityElement.Escape(Path.Combine(folder, "packages"))
        let projectFile = Path.Combine(consumer, "Consumer.fsproj")
        File.WriteAllText(projectFile, $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>{framework}</TargetFramework>
    <TickSpecBuildVerifyBindings>true</TickSpecBuildVerifyBindings>
    <NuGetAudit>false</NuGetAudit>
    <RestoreSources>{SecurityElement.Escape(feed)};https://api.nuget.org/v3/index.json</RestoreSources>
    <RestorePackagesPath>{packages}</RestorePackagesPath>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Steps.fs" />
    <PackageReference Include="{packageId}" Version="{packageVersion}" />
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
</Project>""")
        File.WriteAllText(Path.Combine(consumer, "Steps.fs"), """
namespace PackageProbe
open TickSpec

type Steps =
    [<Given("a packaged step")>]
    static member Bound() : unit = failwith "Steps must not execute during verification"

type Controller() =
    inherit Microsoft.AspNetCore.Mvc.ControllerBase()
        """)
        File.WriteAllText(Path.Combine(consumer, "Probe.feature"), "Feature: Package bindings\nScenario: Bound\nGiven a packaged step\n")
        // The SDK's implicit FSharp.Core reference must meet the package's minimum.
        let exitCode, output =
            runDotnet consumer
                [
                    "build"; projectFile; "--nologo"; "--verbosity"; "minimal"; "--disable-build-servers"
                    "-p:FSharpCoreImplicitPackageVersion=" + dependencies["FSharp.Core"]
                ]
                None
        Assert.That(exitCode, Is.EqualTo(0), output)
        Assert.That(output, Does.Contain("Verified bindings for 1 scenarios in 1 feature files."))
        Assert.That(output, Does.Not.Contain("NU1605"))

        use assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(consumer, "obj", "project.assets.json")))
        let libraries =
            assets.RootElement.GetProperty("libraries").EnumerateObject()
            |> Seq.map(fun x ->
                let parts = x.Name.Split('/')
                parts[0], parts[1])
            |> Map.ofSeq
        for KeyValue(id, minimumVersion) in dependencies do
            Assert.That(libraries.ContainsKey(id), Is.True, id)
            Assert.That(Version.Parse(libraries[id]), Is.GreaterThanOrEqualTo(Version.Parse(minimumVersion)), id)
        let directDependencies =
            assets.RootElement.GetProperty("project").GetProperty("frameworks").GetProperty(framework).GetProperty("dependencies").EnumerateObject()
            |> Seq.map(fun x -> x.Name)
            |> Seq.toArray
        Assert.That(directDependencies, Does.Not.Contain("NUnit"))
        Assert.That(directDependencies, Does.Not.Contain("TickSpec"))
    finally
        Directory.Delete(folder, true)
