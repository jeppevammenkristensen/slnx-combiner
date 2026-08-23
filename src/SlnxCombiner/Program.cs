using System.Diagnostics;
using Spectre.Console.Cli;
using System.Text;

var commandApp = new CommandApp<RunCommand>().WithDescription(
    "Combine projects from multiple .sln and .slnx files into a single XML solution.");
commandApp.Configure(ctx =>
{
    ctx.UseAssemblyInformationalVersion();
    ctx.PropagateExceptions();
});
return await commandApp.RunAsync(args);
