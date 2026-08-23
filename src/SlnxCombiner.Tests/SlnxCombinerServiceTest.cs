using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Spectre.Console;
using TruePath;
using Xunit;

namespace Slnx_Combiner.Tests;

[TestSubject(typeof(SlnxCombinerService))]
public class SlnxCombinerServiceTest
{
    [Fact]
    public async Task Combine_WhenNoSolutionFilesExist_Throws()
    {
        var fileSystem = new MockFileSystem();
        var traversePath = TestPaths.Absolute("solutions");
        fileSystem.Directory.CreateDirectory(traversePath.Value);
        var service = new SlnxCombinerService(fileSystem, CreateConsole(out _));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.Combine(CreateSettings(TestPaths.Absolute("output", "combined.slnx"), traversePath),
                CancellationToken.None));

        Assert.Equal("No solution files found in this direcotry or it's subdirectories", exception.Message);
    }

    [Fact]
    public async Task Combine_WhenDestinationExtensionIsNotSlnx_ReportsCorrectionBeforeThrowingForNoSolutions()
    {
        var fileSystem = new MockFileSystem();
        var traversePath = TestPaths.Absolute("solutions");
        fileSystem.Directory.CreateDirectory(traversePath.Value);
        var service = new SlnxCombinerService(fileSystem, CreateConsole(out var output));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.Combine(CreateSettings(TestPaths.Absolute("output", "combined.txt"), traversePath),
                CancellationToken.None));

        Assert.Contains("File extension must be .slnx. Found txt. Changed to .slnx", output.ToString());
    }

    private static IAnsiConsole CreateConsole(out StringWriter output)
    {
        output = new StringWriter();
        return AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(output),
            Interactive = InteractionSupport.No,
        });
    }

    private static RunCommand.Settings CreateSettings(AbsolutePath outputFilePath, AbsolutePath traversePath)
    {
        return new RunCommand.Settings
        {
            OutputFilePath = outputFilePath,
            TraversePath = traversePath,
        };
    }
}