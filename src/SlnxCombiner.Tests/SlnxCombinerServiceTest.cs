using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SlnxCombiner.Commands;
using Spectre.Console;
using TruePath;
using Xunit;

namespace Slnx_Combiner.Tests;

[TestSubject(typeof(SlnxCombinerService))]
public class SlnxCombinerServiceTest
{
    [Theory]
    [InlineData("team-app.slnx", true)]
    [InlineData("TEAM-APP.sln", true)]
    [InlineData("other-app.slnx", false)]
    public void GenerateIncludeFilter_WithConfiguredRegex_FiltersByFilenameWithoutExtension(
        string fileName, bool expected)
    {
        var settings = CreateValidatedSettings(include: "^team-app$");

        var filter = SlnxCombinerService.GenerateIncludeFilter(settings);

        Assert.Equal(expected, filter(TestPaths.Absolute(fileName)));
    }

    [Theory]
    [InlineData("team-app.slnx", false)]
    [InlineData("TEAM-APP.sln", false)]
    [InlineData("other-app.slnx", true)]
    public void GenerateExcludeFilter_WithConfiguredRegex_FiltersByFilenameWithoutExtension(
        string fileName, bool expected)
    {
        var settings = CreateValidatedSettings(exclude: "^team-app$");

        var filter = SlnxCombinerService.GenerateExcludeFilter(settings);

        Assert.Equal(expected, filter(TestPaths.Absolute(fileName)));
    }

    [Fact]
    public void GenerateIncludeFilter_WithoutConfiguredRegex_IncludesPath()
    {
        var filter = SlnxCombinerService.GenerateIncludeFilter(new CombineCommand.Settings());

        Assert.True(filter(TestPaths.Absolute("solution.slnx")));
    }

    [Fact]
    public void GenerateExcludeFilter_WithoutConfiguredRegex_IncludesPath()
    {
        var filter = SlnxCombinerService.GenerateExcludeFilter(new CombineCommand.Settings());

        Assert.True(filter(TestPaths.Absolute("solution.slnx")));
    }

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

        Assert.Equal("No solution files found in this directory or it's subdirectories", exception.Message);
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

    private static CombineCommand.Settings CreateSettings(AbsolutePath outputFilePath, AbsolutePath traversePath)
    {
        return new CombineCommand.Settings
        {
            OutputFilePath = outputFilePath,
            TraversePath = [traversePath],
        };
    }

    private static TestSettings CreateValidatedSettings(string include = null, string exclude = null)
    {
        var settings = new TestSettings
        {
            OutputFile = TestPaths.Absolute("combined.slnx").Value,
            Include = include,
            Exclude = exclude,
        };

        Assert.True(settings.ValidateSettings().Successful);
        return settings;
    }

    private sealed class TestSettings : CombineCommand.Settings
    {
        public ValidationResult ValidateSettings() => DoValidate();
    }
}
