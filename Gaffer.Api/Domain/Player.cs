using System.Globalization;
using Gaffer.Api.Shared;

namespace Gaffer.Api.Domain;

/// <summary>
/// A player. The primary constructor rehydrates stored rows (Dapper) and does not validate, so players saved
/// before the current rules can still be read. New players go through <see cref="Create"/> and changes through
/// the <c>Set*</c> methods, which enforce the rules and throw <see cref="DomainValidationException"/>.
/// </summary>
public class Player(
    Guid id,
    string firstName,
    string lastName,
    string? secondLastName,
    Gender gender,
    DateTime birthdate)
{
    public const int MaxNameLength = 100;
    public const int MinimumAge = 16;
    public static readonly DateTime MinimumBirthDate = new(1900, 1, 1);

    private const string FirstNameField = "FirstName";
    private const string LastNameField = "LastName";
    private const string SecondLastNameField = "SecondLastName";
    private const string BirthDateField = "BirthDate";
    private const string GenderField = "Gender";

    public Guid Id { get; set; } = id;
    public string FirstName { get; set; } = firstName;
    public string LastName { get; set; } = lastName;
    public string? SecondLastName { get; set; } = secondLastName;
    public DateTime Birthdate { get; set; } = birthdate;
    public Gender Gender { get; set; } = gender;

    /// <summary>
    /// Creates a new player after checking every field, and reports all the invalid ones at once.
    /// <paramref name="todayUtc"/> is the reference date for the age rules (defaults to the current UTC date).
    /// </summary>
    public static Player Create(Guid id, string? firstName, string? lastName, string? secondLastName, Gender gender,
        DateTime birthdate, DateTime? todayUtc = null)
    {
        var errors = new DomainValidationErrors();
        CheckName(errors, FirstNameField, firstName);
        CheckName(errors, LastNameField, lastName);
        if (secondLastName is not null) CheckName(errors, SecondLastNameField, secondLastName);
        CheckBirthDate(errors, birthdate, todayUtc ?? DateTime.UtcNow.Date);
        CheckGender(errors, gender);
        errors.ThrowIfAny();
        return new Player(id, firstName!, lastName!, secondLastName, gender, birthdate);
    }

    public string GetFirstName()
    {
        return FirstName;
    }

    public void SetFirstName(string newFirstName)
    {
        Validate(errors => CheckName(errors, FirstNameField, newFirstName));
        FirstName = newFirstName;
    }

    public string GetLastName()
    {
        return LastName;
    }

    public void SetLastName(string newLastName)
    {
        Validate(errors => CheckName(errors, LastNameField, newLastName));
        LastName = newLastName;
    }

    public string GetSecondLastName()
    {
        return SecondLastName ?? string.Empty;
    }

    /// <summary>Replaces the second last name. It is optional on creation, but once sent it must be a valid name.</summary>
    public void SetSecondLastName(string newSecondLastName)
    {
        Validate(errors => CheckName(errors, SecondLastNameField, newSecondLastName));
        SecondLastName = newSecondLastName;
    }

    public string GetBirthDate()
    {
        return Birthdate.ToString(CultureInfo.InvariantCulture);
    }

    public void SetBirthDate(string newBirthDate)
    {
        SetBirthDate(DateTime.Parse(newBirthDate, CultureInfo.InvariantCulture));
    }

    public void SetBirthDate(DateTime newBirthDate, DateTime? todayUtc = null)
    {
        Validate(errors => CheckBirthDate(errors, newBirthDate, todayUtc ?? DateTime.UtcNow.Date));
        Birthdate = newBirthDate;
    }

    public string GetGenderName()
    {
        return Gender.ToString();
    }

    public void SetGender(string newGender)
    {
        SetGender(Enum.Parse<Gender>(newGender, ignoreCase: true));
    }

    public void SetGender(Gender gender)
    {
        Validate(errors => CheckGender(errors, gender));
        Gender = gender;
    }

    private static void Validate(Action<DomainValidationErrors> check)
    {
        var errors = new DomainValidationErrors();
        check(errors);
        errors.ThrowIfAny();
    }

    private static void CheckName(DomainValidationErrors errors, string field, string? value) =>
        errors.RequireText(field, value, MaxNameLength);

    // The age rule: the player turns MinimumAge on BirthDate.AddYears(MinimumAge) (29 February -> 28 February).
    private static void CheckBirthDate(DomainValidationErrors errors, DateTime birthDate, DateTime todayUtc)
    {
        var date = birthDate.Date;
        var today = todayUtc.Date;
        if (birthDate == default)
        {
            errors.Add(BirthDateField, $"{BirthDateField} is required.");
        }
        else if (date < MinimumBirthDate)
        {
            errors.Add(BirthDateField, $"{BirthDateField} must be on or after {MinimumBirthDate:yyyy-MM-dd}.");
        }
        else if (date > today)
        {
            errors.Add(BirthDateField, $"{BirthDateField} must not be in the future.");
        }
        else if (date.AddYears(MinimumAge) > today)
        {
            errors.Add(BirthDateField, $"The player must be at least {MinimumAge} years old.");
        }
    }

    private static void CheckGender(DomainValidationErrors errors, Gender gender)
    {
        if (!Enum.IsDefined(gender))
        {
            errors.Add(GenderField, $"{GenderField} must be one of the defined values.");
        }
    }
}
