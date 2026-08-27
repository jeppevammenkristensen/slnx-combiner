using System.Diagnostics;
using Spectre.Console.Cli;
using System.Text;

const string description = "Combine projects from multiple .sln and .slnx files into a single XML solution.";

var commandApp = new CommandApp<CombineCommand>().WithDescription(description);
commandApp.Configure(ctx =>
{
    ctx.UseAssemblyInformationalVersion();
    ctx.PropagateExceptions();
    ctx.AddCommand<CombineCommand>("combine").WithDescription(description);
});
return await commandApp.RunAsync(args);
