namespace Leadgen.Runtime;

internal static class Cli
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var paths = RepositoryPaths.Find();
            if (args.Length > 0 && IsToolingCommand(args[0]))
            {
                return await ToolingCommands.RunAsync(args[0], args.Skip(1).ToArray(), paths);
            }

            var options = CliOptions.Parse(args);
            var runner = new ScenarioRunner(paths);
            await runner.RunAsync(options);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }
    }

    private static bool IsToolingCommand(string arg)
    {
        return arg is "validate" or "dashboard" or "evaluate" or "seed" or "reset" or "inspect";
    }
}

internal sealed record CliOptions(
    string Scenario,
    string Source,
    string AiMode,
    string PromptSource,
    bool SeedCosmos,
    string? OutputDir)
{
    public static CliOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException(
                "Usage: dotnet run --project src/Leadgen.Runtime/Leadgen.Runtime.csproj -- <scenario> [--source fixtures|cosmos] [--ai-mode expected|ollama] [--prompt-source fixture|rag] [--seed-cosmos] [--output-dir <path>]");
        }

        var scenario = args[0];
        var source = "fixtures";
        var aiMode = "expected";
        var promptSource = "fixture";
        var seedCosmos = false;
        string? outputDir = null;

        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--source":
                    source = ReadValue(args, ref index, arg);
                    break;
                case "--ai-mode":
                    aiMode = ReadValue(args, ref index, arg);
                    break;
                case "--prompt-source":
                    promptSource = ReadValue(args, ref index, arg);
                    break;
                case "--seed-cosmos":
                    seedCosmos = true;
                    break;
                case "--output-dir":
                    outputDir = ReadValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        if (source is not ("fixtures" or "cosmos"))
        {
            throw new ArgumentException($"Unsupported --source value: {source}");
        }

        if (aiMode is not ("expected" or "ollama"))
        {
            throw new ArgumentException($"Unsupported --ai-mode value: {aiMode}");
        }

        if (promptSource is not ("fixture" or "rag"))
        {
            throw new ArgumentException($"Unsupported --prompt-source value: {promptSource}");
        }

        if (seedCosmos && source != "cosmos")
        {
            throw new ArgumentException("--seed-cosmos requires --source cosmos.");
        }

        return new CliOptions(scenario, source, aiMode, promptSource, seedCosmos, outputDir);
    }

    private static string ReadValue(string[] args, ref int index, string flag)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {flag}");
        }

        index += 1;
        return args[index];
    }
}
