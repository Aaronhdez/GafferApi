using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Tests.TestShared;
using System.Globalization;

namespace Gaffer.Api.Tests.Unit.Domain;

public class PlayerShould
{
    private Player _player;

    [SetUp]
    public void Setup()
    {
        _player = new Player(TestDefaultValues.EmptyPlayerId,
            TestDefaultValues.DefaultFirstName,
            TestDefaultValues.DefaultLastName,
            TestDefaultValues.DefaultSecondLastName,
            TestDefaultValues.DefaultGender,
            TestDefaultValues.DefaultBirthDate);
    }

    [Test]
    public void ReturnFirstName()
    {
        _player.GetFirstName().Should().Be(TestDefaultValues.DefaultFirstName);
    }    
    
    [Test]
    public void ReturnLastName()
    {
        _player.GetLastName().Should().Be(TestDefaultValues.DefaultLastName);
    }

    [Test]
    public void ReturnSecondLastName()
    {
        _player.GetSecondLastName().Should().Be(TestDefaultValues.DefaultSecondLastName);
    }

    [Test]
    public void ReturnBirthDate()
    {
        _player.GetBirthDate().Should().Be(TestDefaultValues.DefaultBirthDate.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Test]
    public void ReturnGenderName()
    {
        _player.GetGenderName().Should().Be(TestDefaultValues.DefaultGender.ToString());
    }

    [Test]
    public void SetFirstName()
    {
        _player.SetFirstName("Marcos");

        _player.FirstName.Should().Be("Marcos");
    }

    [Test]
    public void SetLastName()
    {
        _player.SetLastName("Sanchez");

        _player.LastName.Should().Be("Sanchez");
    }

    [Test]
    public void SetSecondLastName()
    {
        _player.SetSecondLastName("Lopez");

        _player.SecondLastName.Should().Be("Lopez");
    }

    [Test]
    public void SetBirthDate()
    {
        var birthDate = new DateTime(1995, 5, 23);

        _player.SetBirthDate(birthDate.ToString(CultureInfo.InvariantCulture));

        _player.Birthdate.Should().Be(birthDate);
    }

    [Test]
    public void SetGender()
    {
        _player.SetGender("Female");

        _player.Gender.Should().Be(Gaffer.Api.Shared.Gender.FEMALE);
    }
}