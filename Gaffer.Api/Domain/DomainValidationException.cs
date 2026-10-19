namespace Gaffer.Api.Domain;

public class DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more fields are not valid.") {
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class DomainValidationErrors {
    private readonly Dictionary<string, List<string>> errors = new(StringComparer.Ordinal);

    public bool HasErrors => errors.Count > 0;

    public void Add(string field, string message) {
        if (!errors.TryGetValue(field, out var messages)) {
            messages = [];
            errors[field] = messages;
        }

        if (!messages.Contains(message)) {
            messages.Add(message);
        }
    }

    public void RequireText(string field, string? value, int maxLength) {
        if (value is null) {
            Add(field, $"{field} is required.");
        } else if (string.IsNullOrWhiteSpace(value)) {
            Add(field, $"{field} must not be empty or contain only whitespace.");
        } else if (value.Length > maxLength) {
            Add(field, $"{field} must be at most {maxLength} characters long.");
        }
    }

    public void Collect(Action operation) {
        try {
            operation();
        } catch (DomainValidationException exception) {
            foreach (var (field, messages) in exception.Errors) {
                foreach (var message in messages) Add(field, message);
            }
        }
    }

    public void ThrowIfAny() {
        if (HasErrors) throw new DomainValidationException(ToDictionary());
    }

    private Dictionary<string, string[]> ToDictionary() =>
        errors.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray(), StringComparer.Ordinal);
}
