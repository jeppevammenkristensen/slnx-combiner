using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FileBasedApp.Toolkit;
using FileBasedApp.Toolkit.CommandCli;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using TruePath;
using FileSystem = System.IO.Abstractions.FileSystem;

namespace SlnxCombiner.Commands;

/// <summary>
/// Identifies the type of source files discovered by the combine operation.
/// </summary>
public enum TypeToCombine
{
    /// <summary>
    /// Find solution files and combine them into a single SLNX file.
    /// </summary>
    Solution,

    /// <summary>
    /// Find project files and combine them into a single SLNX file.
    /// </summary>
    Project, 
    
    FartPlasma
}

/// <summary>
/// Runs the command-line workflow that combines discovered solution files into a single SLNX file.
/// </summary>
[UsedImplicitly]
public class CombineCommand : AsyncCommand<CombineCommand.Settings> // For synchronous commands, use Command and Execute instead of ExecuteAsync.
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var slnxCombinerService = new SlnxCombinerService(new FileSystem(), AnsiConsole.Console);
        await slnxCombinerService.Combine(settings, cancellationToken);
        AnsiConsole.MarkupLine($"[green]Combined solution files into {settings.OutputFilePath}[/]");

        return 0; // 0 for success
    }

    /// <summary>
    /// Defines and validates the command-line options used by the combine operation.
    /// </summary>
    public class Settings : ExtendedCommandSettings
    {
        /// <summary>
        /// Gets or sets the requested output file supplied on the command line.
        /// </summary>
        [CommandArgument(0, "<Output>")]
        [Description("The output file to write the combined solution to.")]
        public string? OutputFile { get; set; }

        /// <summary>
        /// Gets the validated absolute path of the output file.
        /// </summary>
        public AbsolutePath OutputFilePath { get; internal set; }

        /// <summary>
        /// Gets or sets the optional directory to search for solution files.
        /// </summary>
        [CommandArgument(1, "[TraverseDirectory]")]
        [Description(
            "The directory to traverse for solution files. This is not necessarily the same as the output directory.")]
        public string[]? TraverseDirectory { get; set; } = [];

        [CommandOption("--overwrite")]
        [Description("If set, the output file will be overwritten if it already exists.")]
        public bool Overwrite { get; set; }

        [CommandOption("--type <TYPE>")]
        [Description("The type of files to combine.")]
        [DefaultValue(TypeToCombine.Solution)]
        public TypeToCombine Type { get; set; } = TypeToCombine.Solution;

        [CommandOption("--include")]
        [Description("If set, this regular expression will include matching solution/project file names.")]
        public string? Include { get; set; }

        [CommandOption("--exclude")]
        [Description("If set, this regular expression will exclude matching solution/project file names.")]
        public string? Exclude { get; set; }

        public Regex? IncludeRegex { get; private set; }
        public Regex? ExcludeRegex { get; private set; }


        /// <summary>
        /// Gets the validated absolute path of the directory searched for solution files.
        /// </summary>
        public ImmutableArray<AbsolutePath> TraversePath { get; internal set; } = ImmutableArray<AbsolutePath>.Empty;

        protected override ValidationResult DoValidate()
        {
            // Exceptions thrown here will be displayed as validation errors.
            // Relative paths are resolved against the current working directory.
            if (string.IsNullOrWhiteSpace(OutputFile))
            {
                throw new ArgumentException("An output file is required.", nameof(OutputFile));
            }

            OutputFilePath = TryGetFile(OutputFile, shouldExist: false, PredefinedRootPath.CurrentDirectory);

            if (TraverseDirectory?.Length > 0)
            {
                TraversePath = ImmutableArray<AbsolutePath>.Empty;

                foreach (var directory in TraverseDirectory)
                {
                    var path = TryGetDirectory(directory, false, shouldExist: true, PredefinedRootPath.CurrentDirectory);
                    TraversePath = TraversePath.Add(path);
                }
            }
            else
            {
                TraversePath = [OutputFilePath / ".."];
            }

            IncludeRegex = ValidateAndGetRegex(Include);
            ExcludeRegex = ValidateAndGetRegex(Exclude);

            return base.DoValidate();
        }

        private Regex? ValidateAndGetRegex(string? pattern, [CallerArgumentExpression(nameof(pattern))] string? parameterName = null)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return null;
            }

            try
            {
                return new Regex(pattern, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException(
                    $"'{pattern}' is not a valid regular expression.",
                    parameterName,
                    exception);
            }
        }
    }
}