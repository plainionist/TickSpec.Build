module TickSepc.Build.Tests.BindingVerificationTests

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Security
open NUnit.Framework
open TickSpec
open TickSpec.Build
open TickSpec.CodeGen

let private reference (assembly:Assembly) =
    let name = assembly.GetName().Name
    let path = SecurityElement.Escape(assembly.Location)
    $"""<Reference Include="{name}"><HintPath>{path}</HintPath></Reference>"""

let private buildFeatures (features:(string * string) list) (propertyValue:string option) (environmentValue:string option) =
    let folder = Path.Combine(Path.GetTempPath(), "TickSpec.Build.Tests." + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(folder) |> ignore
    try
        let references =
            [ typeof<AbstractFeature>.Assembly; typeof<StepDefinitions>.Assembly; typeof<TestAttribute>.Assembly ]
            |> List.map reference
            |> String.concat Environment.NewLine
        let targets = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "build", "TickSpec.Build.targets"))
        let tool = SecurityElement.Escape(typeof<Feature>.Assembly.Location)
        let property =
            propertyValue
            |> Option.map(fun value -> $"<TickSpecBuildVerifyBindings>{value}</TickSpecBuildVerifyBindings>")
            |> Option.defaultValue ""
        let output = Path.Combine("bin", "output with spaces") + string Path.DirectorySeparatorChar
        let project =
            $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>BindingProbe.Tests</AssemblyName>
    <OutputPath>{output}</OutputPath>
    <NuGetAudit>false</NuGetAudit>
    {property}
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Steps.fs" />
    <ProjectReference Include="Shared\Shared.fsproj" />
    <PackageReference Update="FSharp.Core" Version="8.0.401" />
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
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>BindingProbe.Shared</AssemblyName>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Steps.fs" />
    <PackageReference Update="FSharp.Core" Version="8.0.401" />
    {references}
  </ItemGroup>
</Project>""")
        File.WriteAllText(Path.Combine(sharedFolder, "Steps.fs"), """
namespace BindingProbe
open TickSpec

type SharedSteps =
    [<Given("a shared step")>]
    static member Shared() : unit = failwith "Shared steps must not execute during verification"
        """)
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

        let startInfo = ProcessStartInfo("dotnet")
        startInfo.WorkingDirectory <- folder
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        for argument in [ "build"; projectFile; "--nologo"; "--verbosity"; "minimal"; "--disable-build-servers" ] do
            startInfo.ArgumentList.Add(argument)
        startInfo.Environment.Remove("TickSpecBuildVerifyBindings") |> ignore
        environmentValue |> Option.iter(fun value -> startInfo.Environment["TickSpecBuildVerifyBindings"] <- value)

        use child = new Process()
        child.StartInfo <- startInfo
        if child.Start() |> not then
            failwith "Could not start the consuming project's build"
        let stdout = child.StandardOutput.ReadToEndAsync()
        let stderr = child.StandardError.ReadToEndAsync()
        if child.WaitForExit(120000) |> not then
            child.Kill(true)
            child.WaitForExit()
            failwith "The consuming project's build timed out"
        child.ExitCode, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult()
    finally
        Directory.Delete(folder, true)

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
    Assert.That(TestDelegate(fun () -> BindingVerifier.Verify missing), Throws.TypeOf<FileNotFoundException>())

[<Test>]
let ``Assembly without embedded features is an explicit error``() =
    Assert.That(
        TestDelegate(fun () -> BindingVerifier.Verify typeof<Feature>.Assembly.Location),
        Throws.TypeOf<InvalidOperationException>().With.Message.Contains("No embedded .feature resources"))
