using System;
using System.IO;
using JetBrains.Annotations;
using Spectre.Console;
using Xunit;

namespace Slnx_Combiner.Tests;

[TestSubject(typeof(CombineCommand.Settings))]
public class RunCommandSettingsTest
{
    [Fact]
    public void Validate_WithValidFilters_CreatesCaseInsensitiveRegexes()
    {
        var settings = CreateSettings();
        settings.Include = "^team-";
        settings.Exclude = "skip$";

        var result = settings.ValidateSettings();

        Assert.True(result.Successful);
        Assert.Matches(settings.IncludeRegex!, "TEAM-app");
        Assert.Matches(settings.ExcludeRegex!, "app-SKIP");
    }

    [Theory]
    [InlineData(true, "Include")]
    [InlineData(false, "Exclude")]
    public void Validate_WithInvalidFilter_Throws(bool invalidInclude, string parameterName)
    {
        var settings = CreateSettings();
        if (invalidInclude)
        {
            settings.Include = "[";
        }
        else
        {
            settings.Exclude = "[";
        }

        var exception = Assert.Throws<ArgumentException>(settings.ValidateSettings);

        Assert.Equal(parameterName, exception.ParamName);
        Assert.StartsWith("'[' is not a valid regular expression.", exception.Message);
    }

    private static TestSettings CreateSettings()
    {
        var directory = Path.Combine(Path.GetTempPath(), "SlnxCombiner.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        return new TestSettings
        {
            OutputFile = Path.Combine(directory, "combined.slnx"),
            TraverseDirectory = [directory]
        };
    }

    private sealed class TestSettings : CombineCommand.Settings
    {
        public ValidationResult ValidateSettings() => DoValidate();
    }
}