using Spectre.Console.Cli;
using SlnxCombiner;
using SlnxCombiner.Commands;

const string description = "Combine projects from multiple .sln and .slnx files into a single XML solution.";

var commandApp = new CommandApp<CombineCommand>().WithDescription(description);
commandApp.Configure(ctx =>
{
    ctx.UseAssemblyInformationalVersion();
    ctx.PropagateExceptions();
    ctx.SetHelpProvider(new EnumValueHelpProvider(ctx.Settings, typeof(CombineCommand.Settings)));
    ctx.AddCommand<CombineCommand>("combine").WithDescription(description);
});
return await commandApp.RunAsync(args);