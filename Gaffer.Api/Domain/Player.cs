using System.Globalization;
using Gaffer.Api.Shared;

namespace Gaffer.Api.Domain;

public class Player(
    Guid id,
    string firstName,
    string lastName,
    string secondLastName,
    Gender gender,
    DateTime birthdate)
{
    public Guid Id { get; set; } = id;
    public string FirstName { get; set; } = firstName;
    public string LastName { get; set; } = lastName;
    public string? SecondLastName { get; set; } = secondLastName;
    public DateTime Birthdate { get; set; } = birthdate;
    public Gender Gender { get; set; } = gender;

    public string GetFirstName()
    {
        return FirstName;
    }

    public void SetFirstName(string newFirstName)
    {
        FirstName = newFirstName;
    }

    public string GetLastName()
    {
        return LastName;
    }

    public void SetLastName(string commandFirstName)
    {
        LastName = commandFirstName;
    }

    public string GetSecondLastName()
    {
        return SecondLastName ?? string.Empty;
    }

    public void SetSecondLastName(string commandFirstName)
    {
        SecondLastName = commandFirstName;
    }

    public string GetBirthDateName()
    {
        return Birthdate.ToString(CultureInfo.InvariantCulture);
    }

    public void SetBirthDateName(string commandFirstName)
    {
        Birthdate = DateTime.Parse(commandFirstName, CultureInfo.InvariantCulture);
    }

    public string GetGenderName()
    {
        return Gender.ToString();
    }

    public void SetGender(string commandFirstName)
    {
        Gender = Enum.Parse<Gender>(commandFirstName, ignoreCase: true);
    }
}