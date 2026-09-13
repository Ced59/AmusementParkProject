using System.Runtime.CompilerServices;
using Xunit;

namespace AmusementPark.Application.Tests.Architecture;

internal sealed record SourceFileLineCount(string Path, int LineCount);
