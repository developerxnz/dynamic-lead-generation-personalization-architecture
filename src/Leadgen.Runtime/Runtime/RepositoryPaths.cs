namespace Leadgen.Runtime;

internal sealed record RepositoryPaths(
    string RootDirectory,
    string MockDataDirectory,
    string ScenariosDirectory,
    string SharedDirectory)
{
    public static RepositoryPaths Find()
    {
        foreach (var start in CandidateStartDirectories())
        {
            var current = new DirectoryInfo(start);
            while (current is not null)
            {
                var readmePath = Path.Combine(current.FullName, "README.md");
                var mockDataPath = Path.Combine(current.FullName, "mock-data");
                if (File.Exists(readmePath) && Directory.Exists(mockDataPath))
                {
                    return new RepositoryPaths(
                        current.FullName,
                        mockDataPath,
                        Path.Combine(mockDataPath, "scenarios"),
                        Path.Combine(mockDataPath, "shared"));
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not find the repository root from the current runtime context.");
    }

    private static IEnumerable<string> CandidateStartDirectories()
    {
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
    }
}
