using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Abstractions;
using System.Xml.Linq;
using JetBrains.Annotations;
using Spectre.Console;
using TruePath;
using Xunit;

namespace Slnx_Combiner.Tests;

[TestSubject(typeof(SlnxCombinerService))]
public class SlnxCombinerServiceIntegrationTest
{
    [Fact]
    public async Task Combine_WithTwoSolutions_WritesRelativeProjectsAndRemovesDuplicates()
    {
        var root = Path.Combine(Path.GetTempPath(), "SlnxCombiner.Tests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var inputDirectory = Path.Combine(root, "solutions");
        Directory.CreateDirectory(inputDirectory);
        var output = Path.Combine(root, "combined.slnx");

        File.WriteAllText(Path.Combine(inputDirectory, "team-a.slnx"), Solution("/TeamA/", "src/App/App.csproj"));
        File.WriteAllText(Path.Combine(inputDirectory, "team-b.slnx"), Solution("/TeamB/", "src/App/App.csproj", "tests/App.Tests/App.Tests.csproj"));

        var service = new SlnxCombinerService(new FileSystem(), AnsiConsole.Create(
            new AnsiConsoleSettings { Interactive = InteractionSupport.No }));

        await service.Combine(new RunCommand.Settings
        {
            OutputFilePath = AbsolutePath.Create(output),
            TraversePath = [AbsolutePath.Create(inputDirectory)],
        }, CancellationToken.None);

        var document = XDocument.Load(output);
        var projects = document.Descendants("Project").ToArray();

        Assert.Equal(2, projects.Length);
        Assert.Contains(projects, project => project.Attribute("Path")?.Value == "solutions/src/App/App.csproj");
        Assert.Contains(projects, project => project.Attribute("Path")?.Value == "solutions/tests/App.Tests/App.Tests.csproj");
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
            new AnsiConsoleSettings { Interactive = InteractionSupport.No }));
        var requestedOutput = AbsolutePath.Create(Path.Combine(root, "combined.txt"));

        await service.Combine(new RunCommand.Settings
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
