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
        public string? OutputFile { get; set; }

        /// <summary>
        /// Gets the validated absolute path of the output file.
        /// </summary>
        public AbsolutePath OutputFilePath { get; internal set; }
        
        /// <summary>
        /// Gets or sets the optional directory to search for solution files.
        /// </summary>
        [CommandArgument(0, "[TraverseDirectory]")]
        [DisplayName("The directory to traverse for solution files. This is not necesarrily the same as the output directory.")]
        public string? TraverseDirectory { get; set; }
        
        /// <summary>
        /// Gets the validated absolute path of the directory searched for solution files.
        /// </summary>
        public AbsolutePath TraversePath { get; internal set; }

        protected override ValidationResult DoValidate()
        {
            // Exceptions here will bubble up and outputted as validation		
            // This will evaluate the path. If the path is relative, it will relative (in this case) against the execution folder. That would be the
            // directory that this .cs lives in
            OutputFilePath = this.TryGetFile(OutputFile, shouldExist: false, PredefinedRootPath.CurrentDirectory);
            
            if (!string.IsNullOrWhiteSpace(TraverseDirectory))
            {
                TraversePath = this.TryGetDirectory(TraverseDirectory, false, shouldExist: true, PredefinedRootPath.CurrentDirectory); 
            }
            else
            {
                TraversePath = OutputFilePath / "..";
            }
            
            return base.DoValidate();
        }
    }
}
