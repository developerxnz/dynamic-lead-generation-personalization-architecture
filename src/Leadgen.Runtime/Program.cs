using Leadgen.Runtime;

// Entry point for the local runtime CLI and tooling commands.
var exitCode = await Cli.RunAsync(args);
return exitCode;
