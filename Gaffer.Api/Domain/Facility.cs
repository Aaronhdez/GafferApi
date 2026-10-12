namespace Gaffer.Api.Domain;

public enum FacilityType
{
    Stadium,
    Arena,
    Pitch,
    Training
}

/// <summary>
/// The address of a facility. The primary constructor rehydrates stored rows and does not validate; new addresses
/// are checked through <see cref="Create"/> or <see cref="Facility.Create"/>. Field errors are named as in the
/// request (<c>Address.Street</c>, ...).
/// </summary>
public class Address(
    Guid id,
    string street,
    string streetNumber,
    string postalCode,
    string city,
    string region,
    string country)
{
    public const int MaxTextLength = 100;
    public const int MaxCodeLength = 20;
    public const string FieldPrefix = "Address.";

    public Guid Id { get; set; } = id;
    public string Street { get; set; } = street;
    public string StreetNumber { get; set; } = streetNumber;
    public string PostalCode { get; set; } = postalCode;
    public string City { get; set; } = city;
    public string Region { get; set; } = region;
    public string Country { get; set; } = country;

    /// <summary>Creates a new address after checking every field, and reports all the invalid ones at once.</summary>
    public static Address Create(Guid id, string? street, string? streetNumber, string? postalCode, string? city,
        string? region, string? country)
    {
        var address = new Address(id, street!, streetNumber!, postalCode!, city!, region!, country!);
        var errors = new DomainValidationErrors();
        address.CheckFields(errors);
        errors.ThrowIfAny();
        return address;
    }

    internal void CheckFields(DomainValidationErrors errors)
    {
        errors.RequireText(FieldPrefix + nameof(Street), Street, MaxTextLength);
        errors.RequireText(FieldPrefix + nameof(StreetNumber), StreetNumber, MaxCodeLength);
        errors.RequireText(FieldPrefix + nameof(PostalCode), PostalCode, MaxCodeLength);
        errors.RequireText(FieldPrefix + nameof(City), City, MaxTextLength);
        errors.RequireText(FieldPrefix + nameof(Region), Region, MaxTextLength);
        errors.RequireText(FieldPrefix + nameof(Country), Country, MaxTextLength);
    }
}

/// <summary>
/// A stand of a facility. The primary constructor rehydrates stored rows and does not validate; new or replaced
/// stands are checked through <see cref="Create"/> (sub-resource) or <see cref="CheckFields"/> (stands nested in a
/// facility request, where the caller names each stand by its index).
/// </summary>
public class Stand(Guid id, string standName, int standCapacity)
{
    public const int MaxNameLength = 100;

    public Guid Id { get; set; } = id;
    public string StandName { get; set; } = standName;
    public int StandCapacity { get; set; } = standCapacity;

    /// <summary>Creates a new stand after checking every field, and reports all the invalid ones at once.</summary>
    public static Stand Create(Guid id, string? standName, int standCapacity)
    {
        var errors = new DomainValidationErrors();
        CheckFields(errors, string.Empty, standName, standCapacity);
        errors.ThrowIfAny();
        return new Stand(id, standName!, standCapacity);
    }

    /// <summary>
    /// Adds the errors of the stand fields to <paramref name="errors"/>. <paramref name="fieldPrefix"/> names the stand
    /// in the request (e.g. <c>Stands[1].</c>); the stand does not know its position, so whoever builds the list sets it.
    /// </summary>
    public static void CheckFields(DomainValidationErrors errors, string fieldPrefix, string? standName, int standCapacity)
    {
        errors.RequireText(fieldPrefix + nameof(StandName), standName, MaxNameLength);
        if (standCapacity < 0)
        {
            errors.Add(fieldPrefix + nameof(StandCapacity), $"{fieldPrefix}{nameof(StandCapacity)} must not be negative.");
        }
    }
}

/// <summary>
/// A facility. The primary constructor rehydrates stored rows and only keeps its structural guards (address
/// present, at most four stands); new or replaced facilities go through <see cref="Create"/>, which enforces the
/// field rules of the facility and its address and throws <see cref="DomainValidationException"/>.
/// </summary>
public class Facility(
    Guid id,
    string name,
    bool active,
    DateTime buildDate,
    FacilityType type,
    int capacity,
    Address? address,
    List<Stand>? stands)
{
    public const int MaxNameLength = 100;
    public const int MaxStands = 4;
    public const string StandsField = "Stands";
    public const string MaxStandsMessage = "Stands cannot exceed 4.";
    public static readonly DateTime MinimumBuildDate = new(1800, 1, 1);

    public Guid Id { get; set; } = id;
    public string Name { get; set; } = name;
    public bool Active { get; set; } = active;
    public DateTime BuildDate { get; set; } = buildDate;
    public FacilityType Type { get; set; } = type;
    public int Capacity { get; set; } = capacity;
    public Address Address { get; set; } = address ?? throw new FacilityValidationException("Address is required");
    public List<Stand> Stands { get; set; } = ValidateStands(stands);

    /// <summary>
    /// Creates a facility (new, or the replacement of a stored one) after checking every field of the facility, its
    /// address and the number of stands (at most <see cref="MaxStands"/>), and reports all the invalid ones at once.
    /// The fields of the stands it receives are not checked here: the stands sent in a request are checked with
    /// <see cref="CheckRequestedStands"/>, and stored stands kept by an update are not revalidated (Q16).
    /// <paramref name="todayUtc"/> is the reference date for <see cref="BuildDate"/> (defaults to the current UTC date).
    /// </summary>
    public static Facility Create(Guid id, string? name, bool active, DateTime buildDate, FacilityType type, int capacity,
        Address? address, List<Stand>? stands, DateTime? todayUtc = null)
    {
        var errors = new DomainValidationErrors();
        errors.RequireText(nameof(Name), name, MaxNameLength);
        CheckBuildDate(errors, buildDate, todayUtc ?? DateTime.UtcNow.Date);
        if (!Enum.IsDefined(type))
        {
            errors.Add(nameof(Type), $"{nameof(Type)} must be one of the defined values.");
        }

        if (capacity < 0)
        {
            errors.Add(nameof(Capacity), $"{nameof(Capacity)} must not be negative.");
        }

        if (address is null)
        {
            errors.Add(nameof(Address), $"{nameof(Address)} is required.");
        }
        else
        {
            address.CheckFields(errors);
        }

        CheckStandCount(errors, stands?.Count ?? 0);
        errors.ThrowIfAny();
        return new Facility(id, name!, active, buildDate, type, capacity, address, stands);
    }

    /// <summary>
    /// Checks the stands sent in a facility request: their number and the fields of each one, named by its index in
    /// the request (<c>Stands[1].StandName</c>). A <c>null</c> stand is reported as <c>Stands[1]</c>.
    /// </summary>
    public static void CheckRequestedStands(DomainValidationErrors errors, IReadOnlyList<(string? StandName, int StandCapacity)?> requested)
    {
        CheckStandCount(errors, requested.Count);
        for (var index = 0; index < requested.Count; index++)
        {
            var field = $"{StandsField}[{index}]";
            if (requested[index] is not { } stand)
            {
                errors.Add(field, $"{field} is required.");
                continue;
            }

            Stand.CheckFields(errors, $"{field}.", stand.StandName, stand.StandCapacity);
        }
    }

    private static void CheckStandCount(DomainValidationErrors errors, int count)
    {
        if (count > MaxStands)
        {
            errors.Add(StandsField, MaxStandsMessage);
        }
    }

    private static void CheckBuildDate(DomainValidationErrors errors, DateTime buildDate, DateTime todayUtc)
    {
        if (buildDate == default)
        {
            errors.Add(nameof(BuildDate), $"{nameof(BuildDate)} is required.");
        }
        else if (buildDate.Date < MinimumBuildDate)
        {
            errors.Add(nameof(BuildDate), $"{nameof(BuildDate)} must be on or after {MinimumBuildDate:yyyy-MM-dd}.");
        }
        else if (buildDate.Date > todayUtc.Date)
        {
            errors.Add(nameof(BuildDate), $"{nameof(BuildDate)} must not be in the future.");
        }
    }

    private static List<Stand> ValidateStands(List<Stand>? stands)
    {
        var items = stands ?? [];
        if (items.Count > MaxStands)
        {
            throw new FacilityValidationException("Stands cannot exceed 4");
        }

        return items;
    }
}

public class FacilityValidationException(string message) : Exception(message);
