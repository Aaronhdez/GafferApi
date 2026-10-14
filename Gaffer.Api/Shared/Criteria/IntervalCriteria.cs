namespace Gaffer.Api.Shared.Criteria;

public class IntervalCriteria<T> : IIntervalCriteria where T : IComparable<T> {
    public IntervalCriteria(string name, T min, T max) {
        ArgumentNullException.ThrowIfNull(min);
        ArgumentNullException.ThrowIfNull(max);
        Name = name;
        Min = min;
        Max = max;
    }

    public string Name { get; }
    public T Min { get; }
    public T Max { get; }

    object IIntervalCriteria.Min => Min;
    object IIntervalCriteria.Max => Max;
}
