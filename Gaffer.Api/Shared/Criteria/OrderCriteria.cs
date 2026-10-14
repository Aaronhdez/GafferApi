namespace Gaffer.Api.Shared.Criteria;

public class OrderCriteria {
    public string Name { get; set; } = null!;
    public OrderDirection Direction { get; set; } = OrderDirection.ASC;
}

public enum OrderDirection {
    ASC,
    DESC
}
