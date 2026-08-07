using Gaffer.Api.Shared;

namespace Gaffer.Api.Domain;

public class Player(
    Guid playerId,
    string firstName,
    string lastName,
    string secondLastName,
    Gender gender,
    DateTime birthdate)
{
    public Guid PlayerId { get; set; } = playerId;
    public string FirstName { get; set; } = firstName;
    public string LastName { get; set; } = lastName;
    public string? SecondLastName { get; set; } = secondLastName;
    public DateTime Birthdate { get; set; } = birthdate;
    public Gender Gender { get; set; } = gender;
}