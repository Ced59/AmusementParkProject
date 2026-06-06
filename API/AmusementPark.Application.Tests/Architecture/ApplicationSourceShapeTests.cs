using System.Runtime.CompilerServices;
using Xunit;

namespace AmusementPark.Application.Tests.Architecture;

public sealed class ApplicationSourceShapeTests
{
    private const int MaximumApplicationFileLineCount = 650;
    private const int MaximumApplicationHandlerFileLineCount = 550;

    [Fact]
    public void ApplicationSourceFiles_ShouldStayBelowTheArchitecturalSizeBudget()
    {
        string applicationDirectory = ResolveProjectDirectory("AmusementPark.Application");
        IReadOnlyCollection<string> violations = Directory
            .EnumerateFiles(applicationDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !Path.GetFileName(path).EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            .Select(path => new SourceFileLineCount(path, File.ReadLines(path).Count()))
            .Where(static file => file.LineCount > MaximumApplicationFileLineCount)
            .Select(file => FormatViolation(applicationDirectory, file, MaximumApplicationFileLineCount))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationHandlers_ShouldStaySmallEnoughToExposeOneUseCaseClearly()
    {
        string applicationDirectory = ResolveProjectDirectory("AmusementPark.Application");
        IReadOnlyCollection<string> violations = Directory
            .EnumerateFiles(applicationDirectory, "*Handler*.cs", SearchOption.AllDirectories)
            .Where(static path => !Path.GetFileName(path).EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            .Select(path => new SourceFileLineCount(path, File.ReadLines(path).Count()))
            .Where(static file => file.LineCount > MaximumApplicationHandlerFileLineCount)
            .Select(file => FormatViolation(applicationDirectory, file, MaximumApplicationHandlerFileLineCount))
            .ToArray();

        Assert.Empty(violations);
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

    private static string FormatViolation(string rootDirectory, SourceFileLineCount file, int maximumLineCount)
    {
        string relativePath = Path.GetRelativePath(rootDirectory, file.Path);
        return $"{relativePath} has {file.LineCount} lines. Maximum allowed: {maximumLineCount}.";
    }

    private sealed record SourceFileLineCount(string Path, int LineCount);
}
