namespace Gaffer.Api.Shared.Repositories;

public class SearchCriteria {
    public string Name { get; set; } = null!;
    public string Value { get; set; } = null!;
    public CriteriaType CriteriaType { get; set; } = CriteriaType.AND;
}

public enum CriteriaType {
    AND,
    OR
}
