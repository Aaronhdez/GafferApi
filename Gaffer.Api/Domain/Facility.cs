namespace Gaffer.Api.Domain;

public enum FacilityType
{
    Stadium,
    Arena,
    Pitch,
    Training
}

public class Address(
    Guid id,
    string street,
    string streetNumber,
    string postalCode,
    string city,
    string region,
    string country)
{
    public Guid Id { get; set; } = id;
    public string Street { get; set; } = street;
    public string StreetNumber { get; set; } = streetNumber;
    public string PostalCode { get; set; } = postalCode;
    public string City { get; set; } = city;
    public string Region { get; set; } = region;
    public string Country { get; set; } = country;
}

public class Stand(Guid id, string standName, int standCapacity)
{
    public Guid Id { get; set; } = id;
    public string StandName { get; set; } = standName;
    public int StandCapacity { get; set; } = standCapacity;
}

public class Facility(
    Guid id,
    string name,
    bool active,
    DateTime buildDate,
    FacilityType type,
    int capacity,
    Address address,
    List<Stand>? stands)
{
    public Guid Id { get; set; } = id;
    public string Name { get; set; } = name;
    public bool Active { get; set; } = active;
    public DateTime BuildDate { get; set; } = buildDate;
    public FacilityType Type { get; set; } = type;
    public int Capacity { get; set; } = capacity;
    public Address Address { get; set; } = address ?? throw new FacilityValidationException("Address is required");
    public List<Stand> Stands { get; set; } = ValidateStands(stands);

    private static List<Stand> ValidateStands(List<Stand>? stands)
    {
        var items = stands ?? [];
        if (items.Count > 4)
        {
            throw new FacilityValidationException("Stands cannot exceed 4");
        }

        return items;
    }
}

public class FacilityValidationException(string message) : Exception(message);