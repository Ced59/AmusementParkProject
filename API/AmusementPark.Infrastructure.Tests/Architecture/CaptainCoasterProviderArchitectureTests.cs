using System.Runtime.CompilerServices;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Architecture;

public sealed class CaptainCoasterProviderArchitectureTests
{
    private const int MaximumProviderLineCount = 700;

    [Fact]
    public void Provider_ShouldRemainAFocusedOrchestrator()
    {
        string infrastructureDirectory = ResolveProjectDirectory("AmusementPark.Infrastructure");
        string providerPath = Path.Combine(
            infrastructureDirectory,
            "Services",
            "DataSources",
            "CaptainCoaster",
            "CaptainCoasterDataSourceProvider.cs");

        int lineCount = File.ReadLines(providerPath).Count();

        Assert.True(
            lineCount <= MaximumProviderLineCount,
            $"CaptainCoasterDataSourceProvider.cs has {lineCount} lines. Maximum allowed: {MaximumProviderLineCount}.");
    }

    private static string ResolveProjectDirectory(string projectName, [CallerFilePath] string callerFilePath = "")
    {
        DirectoryInfo? directory = new FileInfo(callerFilePath).Directory;
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, projectName)))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Cannot resolve project directory '{projectName}' from '{callerFilePath}'.");
        }

        return Path.Combine(directory.FullName, projectName);
    }
}
