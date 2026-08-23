using System.ComponentModel;
using FileBasedApp.Toolkit;
using FileBasedApp.Toolkit.CommandCli;
using Spectre.Console;
using Spectre.Console.Cli;
using TruePath;
using FileSystem = System.IO.Abstractions.FileSystem;

public class RunCommand : AsyncCommand<RunCommand.Settings> // For sync only you can use Command (and have Execute instead of ExecuteAsync
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var slnxCombinerService = new SlnxCombinerService(new FileSystem(), AnsiConsole.Console);
        await slnxCombinerService.Combine(settings, cancellationToken);

        return 0; // 0 for success
    }

    public class Settings : ExtendedCommandSettings
    {
        [CommandArgument(0, "<Output>")]
        public string? OutputFile { get; set; }
        /// <summary></summary>
        public AbsolutePath OutputFilePath { get; internal set; }
        
        [CommandArgument(0, "[TraverseDirectory]")]
        [DisplayName("The directory to traverse for solution files. This is not necesarrily the same as the output directory.")]
        public string? TraverseDirectory { get; set; }
        
        /// <summary></summary>
        public AbsolutePath TraversePath { get; internal set; }
        
        [CommandOption("-n|--name ")]
        [DefaultValue("All")]
        public string Name { get; set; } = "All";

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