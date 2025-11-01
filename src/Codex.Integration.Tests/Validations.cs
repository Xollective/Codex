using System.Xml.Linq;
using BuildXL.Utilities.Collections;
using Codex.Search;
using Codex.Web.Common;
using Microsoft.CodeAnalysis.Classification;

namespace Codex.Integration.Tests;

public static class Validations
{
    public static void ShouldSequenceEqual(this ReadOnlyMemory<byte> actual, ReadOnlyMemory<byte> expected)
    {
        actual.Span.ShouldSequenceEqual(expected.Span);
    }

    public static void ShouldSequenceEqual(this Memory<byte> actual, ReadOnlyMemory<byte> expected)
    {
        actual.Span.ShouldSequenceEqual(expected.Span);
    }

    public static void ShouldSequenceEqual(this ReadOnlySpan<byte> actual, ReadOnlySpan<byte> expected)
    {
        actual.ToArray().Should().Equal(expected.ToArray());
    }
}