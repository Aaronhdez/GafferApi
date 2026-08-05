using Gaffer.Api.Shared;

namespace Gaffer.Api.Domain;

public class Player(
    Guid id,
    string firstName,
    string lastName,
    string? secondLastName,
    DateTime birthdate,
    Gender gender)
{
    public Guid Id { get; set; } = id;
    public string FirstName { get; set; } = firstName;
    public string LastName { get; set; } = lastName;
    public string? SecondLastName { get; set; } = secondLastName;
    public DateTime Birthdate { get; set; } = birthdate;
    public Gender Gender { get; set; } = gender;
}