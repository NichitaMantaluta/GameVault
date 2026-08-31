using System.Text.Json;

namespace GameStore.Tests;

public static class TestJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
