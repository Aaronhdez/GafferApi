namespace Gaffer.Api.Shared.Criteria;

// Non-generic view of IntervalCriteria<T>, so a single list can hold intervals of different T.
public interface IIntervalCriteria {
    string Name { get; }
    object Min { get; }
    object Max { get; }
}
