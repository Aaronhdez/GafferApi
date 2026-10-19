namespace Gaffer.Api.Shared.Criteria;

public class CriteriaSet {
    public List<ValueCriteria> ValueCriteria { get; set; } = [];
    public List<IIntervalCriteria> IntervalCriteria { get; set; } = [];
    public List<OrderCriteria> OrderCriteria { get; set; } = [];
}
