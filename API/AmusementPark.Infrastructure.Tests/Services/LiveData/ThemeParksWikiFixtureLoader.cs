using System.Reflection;

namespace AmusementPark.Infrastructure.Tests.Services.LiveData;

internal static class ThemeParksWikiFixtureLoader
{
    public static string Read(string fileName)
    {
        Assembly assembly = typeof(ThemeParksWikiFixtureLoader).Assembly;
        string resourceName = assembly.GetManifestResourceNames().Single(name =>
            name.EndsWith($".Fixtures.{fileName}", StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Fixture '{fileName}' was not found.");
        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
