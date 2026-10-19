namespace Gaffer.Api.Shared.Criteria;

public interface IIntervalCriteria {
    string Name { get; }
    object Min { get; }
    object Max { get; }
}
