using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;
using TruePath;
using Xunit;

namespace Slnx_Combiner.Tests;

[TestSubject(typeof(SlnxBuilder))]
public class SlnxBuilderTest
{
    [Fact]
    public void Build_WhenNoFoldersWereAdded_ReturnsEmptySolution()
    {
        var builder = new SlnxBuilder(TestPaths.Absolute("repository"));

        var solution = builder.Build();

        Assert.True(XNode.DeepEquals(new XElement("Solution"), solution));
    }

    [Fact]
    public void Build_WithFolderAndProjects_GeneratesSlnxXmlWithRelativeProjectPaths()
    {
        var root = TestPaths.Absolute("repository");
        var slnPath = TestPaths.Absolute("sln.slnx");
        var builder = new SlnxBuilder(root);

        builder.NewSolutionFolder("/Applications/", slnPath)
            .AddProject(TestPaths.Absolute("repository", "src", "App", "App.csproj"), "Application",
                "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")
            .AddProject(TestPaths.Absolute("repository", "tests", "App.Tests", "App.Tests.csproj"), null, null)
            .AddTo(builder);

        var solution = builder.Build();

        Assert.Equal("Solution", solution.Name.LocalName);

        var folder = Assert.Single(solution.Elements("Folder"));
        Assert.Equal("/Applications/", folder.Attribute("Name")!.Value);

        var projects = folder.Elements("Project").ToArray();
        Assert.Equal(2, projects.Length);

        Assert.Equal("src/App/App.csproj", projects[0].Attribute("Path")!.Value);
        Assert.Equal("Application", projects[0].Attribute("DisplayName")!.Value);
        Assert.Equal("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", projects[0].Attribute("Type")!.Value);

        Assert.Equal("tests/App.Tests/App.Tests.csproj", projects[1].Attribute("Path")!.Value);
        Assert.Null(projects[1].Attribute("DisplayName"));
        Assert.Null(projects[1].Attribute("Type"));
    }
}