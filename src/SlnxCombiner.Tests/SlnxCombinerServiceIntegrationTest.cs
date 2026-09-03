using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Abstractions;
using System.Xml.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using SlnxCombiner.Commands;
using Spectre.Console;
using TruePath;
using Xunit;

namespace Slnx_Combiner.Tests;

[TestSubject(typeof(SlnxCombinerService))]
public class SlnxCombinerServiceIntegrationTest
{
    [Fact]
    public async Task Combine_WithSolutionType_WritesRelativeProjectsAndRemovesDuplicates()
    {
        var root = Path.Combine(Path.GetTempPath(), "SlnxCombiner.Tests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var inputDirectory = Path.Combine(root, "solutions");
        Directory.CreateDirectory(inputDirectory);
        var output = Path.Combine(root, "combined.slnx");

        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "team-a.slnx"),
            Solution("/TeamA/", "src/App/App.csproj"));
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "team-b.slnx"),
            Solution("/TeamB/", "src/App/App.csproj", "tests/App.Tests/App.Tests.csproj"));

        var service = new SlnxCombinerService(new FileSystem(), AnsiConsole.Create(
            new AnsiConsoleSettings {Interactive = InteractionSupport.No}));

        await service.Combine(new CombineCommand.Settings
        {
            OutputFilePath = AbsolutePath.Create(output),
            TraversePath = [AbsolutePath.Create(inputDirectory)],
            Type = TypeToCombine.Solution,
        }, CancellationToken.None);

        var document = XDocument.Load(output);
        var projects = document.Descendants("Project").ToArray();

        Assert.Equal(2, projects.Length);
        Assert.Contains(projects, project => project.Attribute("Path")?.Value == "solutions/src/App/App.csproj");
        Assert.Contains(projects,
            project => project.Attribute("Path")?.Value == "solutions/tests/App.Tests/App.Tests.csproj");
    }

    [Fact]
    public async Task Combine_WithProjectType_WritesDiscoveredProjectsAtSolutionRootAndIgnoresSolutions()
    {
        var root = Path.Combine(Path.GetTempPath(), "SlnxCombiner.Tests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var inputDirectory = Path.Combine(root, "repository");
        var appDirectory = Path.Combine(inputDirectory, "src", "App");
        var testDirectory = Path.Combine(inputDirectory, "tests", "App.Tests");
        Directory.CreateDirectory(appDirectory);
        Directory.CreateDirectory(testDirectory);
        var output = Path.Combine(root, "combined.slnx");

        File.WriteAllText(Path.Combine(appDirectory, "App.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(testDirectory, "App.Tests.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(inputDirectory, "ignored.slnx"),
            Solution("/Ignored/", "src/Ignored/Ignored.csproj"));

        var service = new SlnxCombinerService(new FileSystem(), AnsiConsole.Create(
            new AnsiConsoleSettings {Interactive = InteractionSupport.No}));

        await service.Combine(new CombineCommand.Settings
        {
            OutputFilePath = AbsolutePath.Create(output),
            TraversePath = [AbsolutePath.Create(inputDirectory)],
            Type = TypeToCombine.Project,
        }, CancellationToken.None);

        var document = XDocument.Load(output);
        Assert.Empty(document.Descendants("Folder"));

        var projects = document.Root!.Elements("Project").ToArray();
        Assert.Equal(2, projects.Length);
        Assert.Contains(projects, project => project.Attribute("Path")?.Value == "repository/src/App/App.csproj");
        Assert.Contains(projects,
            project => project.Attribute("Path")?.Value == "repository/tests/App.Tests/App.Tests.csproj");
        Assert.DoesNotContain(projects,
            project => project.Attribute("Path")?.Value == "repository/src/Ignored/Ignored.csproj");
    }

    [Fact]
    public async Task Combine_WithProjectTypeAndOverlappingTraversePaths_WritesProjectOnce()
    {
        var root = Path.Combine(Path.GetTempPath(), "SlnxCombiner.Tests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var inputDirectory = Path.Combine(root, "repository");
        var projectDirectory = Path.Combine(inputDirectory, "src", "App");
        Directory.CreateDirectory(projectDirectory);
        var output = Path.Combine(root, "combined.slnx");

        File.WriteAllText(Path.Combine(projectDirectory, "App.csproj"), "<Project />");

        var service = new SlnxCombinerService(new FileSystem(), AnsiConsole.Create(
            new AnsiConsoleSettings {Interactive = InteractionSupport.No}));

        await service.Combine(new CombineCommand.Settings
        {
            OutputFilePath = AbsolutePath.Create(output),
            TraversePath = [AbsolutePath.Create(inputDirectory), AbsolutePath.Create(projectDirectory)],
            Type = TypeToCombine.Project,
        }, CancellationToken.None);

        var project = Assert.Single(XDocument.Load(output).Descendants("Project"));
        Assert.Equal("repository/src/App/App.csproj", project.Attribute("Path")?.Value);
    }

    [Fact]
    public async Task Combine_WithProjectTypeAndDuplicateFileNames_AddsUniqueDisplayName()
    {
        var root = Path.Combine(Path.GetTempPath(), "SlnxCombiner.Tests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var inputDirectory = Path.Combine(root, "repository");
        var firstProjectDirectory = Path.Combine(inputDirectory, "src", "First");
        var secondProjectDirectory = Path.Combine(inputDirectory, "src", "Second");
        Directory.CreateDirectory(firstProjectDirectory);
        Directory.CreateDirectory(secondProjectDirectory);
        var output = Path.Combine(root, "combined.slnx");

        File.WriteAllText(Path.Combine(firstProjectDirectory, "App.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(secondProjectDirectory, "App.csproj"), "<Project />");

        var service = new SlnxCombinerService(new FileSystem(), AnsiConsole.Create(
            new AnsiConsoleSettings {Interactive = InteractionSupport.No}));

        await service.Combine(new CombineCommand.Settings
        {
            OutputFilePath = AbsolutePath.Create(output),
            TraversePath = [AbsolutePath.Create(inputDirectory)],
            Type = TypeToCombine.Project,
        }, CancellationToken.None);

        var xDocument = XDocument.Load(output);
        var projects = xDocument.Descendants("Project").ToArray();
        var folders = xDocument.Descendants("Folder").ToArray();

        projects.Should().HaveCount(2);
        projects.Where(x => x.Attribute("DisplayName") is null).Should().HaveCount(2);
        
        folders.Should().HaveCount(2);
        folders.Select(folder => folder.Attribute("Name")!.Value).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Combine_WhenOutputExtensionIsNotSlnx_WritesTheCorrectedSlnxFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "SlnxCombiner.Tests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var inputDirectory = Path.Combine(root, "solutions");
        Directory.CreateDirectory(inputDirectory);
        File.WriteAllText(Path.Combine(inputDirectory, "team.slnx"), Solution("/Team/", "src/App/App.csproj"));

        var service = new SlnxCombinerService(new FileSystem(), AnsiConsole.Create(
            new AnsiConsoleSettings {Interactive = InteractionSupport.No}));
        var requestedOutput = AbsolutePath.Create(Path.Combine(root, "combined.txt"));

        await service.Combine(new CombineCommand.Settings
        {
            OutputFilePath = requestedOutput,
            TraversePath = [AbsolutePath.Create(inputDirectory)],
        }, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(root, "combined.slnx")));
        Assert.False(File.Exists(requestedOutput.Value));
    }

    private static string Solution(string folder, params string[] projects) =>
        new XElement("Solution",
                new XElement("Folder", new XAttribute("Name", folder),
                    projects.Select(path => new XElement("Project", new XAttribute("Path", path)))))
            .ToString();
}