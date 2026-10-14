namespace Gaffer.Api.Shared.Criteria;

public class ValueCriteria {
    public string Name { get; set; } = null!;
    public List<string> Values { get; set; } = [];
    public ValueCriteriaType Type { get; set; } = ValueCriteriaType.AND;
}


public enum ValueCriteriaType {
    AND,
    OR,
    LIKE
}
