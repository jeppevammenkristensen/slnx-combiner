using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;
using TruePath;
using Xunit;

namespace Slnx_Combiner.Tests;

[TestSubject(typeof(SlnFolderBuilder))]
public class SlnFolderBuilderTest
{
    [Fact]
    public void Build_WhenFolderContainsNoProjects_AddsNormalizedEmptyFolder()
    {
        var solution = new XElement("Solution");
        var folder = new SlnFolderBuilder("/Applications/", TestPaths.Absolute("repository"), null);

        folder.Build(solution);

        var folderElement = Assert.Single(solution.Elements("Folder"));
        Assert.Equal("/Applications/", folderElement.Attribute("Name")!.Value);
        Assert.Empty(folderElement.Elements());
    }

    [Fact]
    public void Build_WithProjects_AddsRelativePathsAndOnlyProvidedMetadata()
    {
        var solution = new XElement("Solution");
        var folder = new SlnFolderBuilder("Applications", TestPaths.Absolute("repository"), null);
        folder.AddProject(
                TestPaths.Absolute("repository", "src", "App", "App.csproj"),
                "Application",
                "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")
            .AddProject(TestPaths.Absolute("repository", "tests", "App.Tests", "App.Tests.csproj"), " ", null);

        folder.Build(solution);

        var projects = solution.Element("Folder")!.Elements("Project").ToArray();
        Assert.Equal(2, projects.Length);

        Assert.Equal("src/App/App.csproj", projects[0].Attribute("Path")!.Value);
        Assert.Equal("Application", projects[0].Attribute("DisplayName")!.Value);
        Assert.Equal("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", projects[0].Attribute("Type")!.Value);

        Assert.Equal("tests/App.Tests/App.Tests.csproj", projects[1].Attribute("Path")!.Value);
        Assert.Null(projects[1].Attribute("DisplayName"));
        Assert.Null(projects[1].Attribute("Type"));
    }
}
