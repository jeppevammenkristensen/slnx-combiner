using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TruePath;
using Xunit;

namespace Slnx_Combiner.Tests;

[TestSubject(typeof(SolutionParser))]
public class SolutionParserTest
{
    [Fact]
    public async Task ReadSolutionAsync_ReadsSlnxProjectsAndMetadata()
    {
        var path = TestPaths.CreateTemporaryFile("input.slnx", """
            <Solution>
              <Folder Name="/Applications/">
                <Project Path="src/App/App.csproj" DisplayName="Application" Type="{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" />
              </Folder>
            </Solution>
            """);

        var result = await SolutionParser.ReadSolutionAsync(path, CancellationToken.None);

        var project = Assert.Single(result.SolutionProjects);
        Assert.Equal(Path.Combine("src", "App", "App.csproj"), project.FilePath);
        Assert.Equal("Application", project.DisplayName);
        Assert.Equal("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", project.Type);
    }

    [Fact]
    public async Task ReadSolutionAsync_WhenExtensionIsUnsupported_ThrowsArgumentException()
    {
        var path = TestPaths.CreateTemporaryFile("input.txt", "not a solution");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            SolutionParser.ReadSolutionAsync(path, CancellationToken.None));

        Assert.Equal("path", exception.ParamName);
    }
}
