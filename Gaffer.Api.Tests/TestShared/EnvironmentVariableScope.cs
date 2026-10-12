namespace Gaffer.Api.Tests.TestShared;

/// <summary>Sets a process environment variable and restores its previous value on dispose.</summary>
public sealed class EnvironmentVariableScope : IDisposable {
    private readonly string name;
    private readonly string? previousValue;

    public EnvironmentVariableScope(string name, string? value) {
        this.name = name;
        previousValue = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(name, previousValue);
}
