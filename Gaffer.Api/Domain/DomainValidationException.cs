namespace Gaffer.Api.Domain;

/// <summary>
/// Thrown by a domain entity when the values used to create or change it break its rules.
/// <see cref="Errors"/> maps each invalid field (named as in the request) to its messages;
/// the messages never repeat the rejected value.
/// </summary>
public class DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more fields are not valid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

/// <summary>
/// Collects field errors so that every invalid field is reported at once, either from explicit checks
/// (<see cref="Add"/>) or from domain operations that already validate (<see cref="Collect"/>).
/// </summary>
public sealed class DomainValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    public bool HasErrors => _errors.Count > 0;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = [];
            _errors[field] = messages;
        }

        messages.Add(message);
    }

    /// <summary>Runs <paramref name="operation"/> and keeps its validation errors instead of failing at once.</summary>
    public void Collect(Action operation)
    {
        try
        {
            operation();
        }
        catch (DomainValidationException exception)
        {
            foreach (var (field, messages) in exception.Errors)
            {
                foreach (var message in messages) Add(field, message);
            }
        }
    }

    public void ThrowIfAny()
    {
        if (HasErrors) throw new DomainValidationException(ToDictionary());
    }

    private Dictionary<string, string[]> ToDictionary() =>
        _errors.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray(), StringComparer.Ordinal);
}
