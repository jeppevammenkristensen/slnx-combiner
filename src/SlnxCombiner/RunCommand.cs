using System.Collections.Immutable;
using System.ComponentModel;
using FileBasedApp.Toolkit;
using FileBasedApp.Toolkit.CommandCli;
using Spectre.Console;
using Spectre.Console.Cli;
using TruePath;
using FileSystem = System.IO.Abstractions.FileSystem;

/// <summary>
/// Runs the command-line workflow that combines discovered solution files into a single SLNX file.
/// </summary>
public class RunCommand : AsyncCommand<RunCommand.Settings> // For sync only you can use Command (and have Execute instead of ExecuteAsync
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
        [Description("If toggled the output file will be overwritten if it already exists.")]
        public bool Overwrite { get; set; }
        
        
        /// <summary>
        /// Gets the validated absolute path of the directory searched for solution files.
        /// </summary>
        public ImmutableArray<AbsolutePath> TraversePath { get; internal set; } = ImmutableArray<AbsolutePath>.Empty;

        protected override ValidationResult DoValidate()
        {
            // Exceptions here will bubble up and outputted as validation		
            // This will evaluate the path. If the path is relative, it will relative (in this case) against the execution folder. That would be the
            // directory that this .cs lives in
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
            
            return base.DoValidate();
        }
    }
}
