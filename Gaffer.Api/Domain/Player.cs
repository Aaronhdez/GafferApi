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
        throw new NotImplementedException();
    }

    public string GetLastName()
    {
        throw new NotImplementedException();
    }

    public void SetLastName(string commandFirstName)
    {
        throw new NotImplementedException();
    }

    public string GetSecondLastName()
    {
        throw new NotImplementedException();
    }

    public void SetSecondLastName(string commandFirstName)
    {
        throw new NotImplementedException();
    }

    public string GetBirthDateName()
    {
        throw new NotImplementedException();
    }

    public void SetBirthDateName(string commandFirstName)
    {
        throw new NotImplementedException();
    }

    public string GetGenderName()
    {
        throw new NotImplementedException();
    }

    public void SetGenderName(string commandFirstName)
    {
        throw new NotImplementedException();
    }
}