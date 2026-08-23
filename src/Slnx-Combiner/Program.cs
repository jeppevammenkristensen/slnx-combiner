using System.Diagnostics;
using Spectre.Console.Cli;
using System.Text;

var commandApp = new CommandApp<RunCommand>().WithDescription("Enter the description here");
commandApp.Configure(ctx =>
{
    ctx.PropagateExceptions();
});
return await commandApp.RunAsync(args);