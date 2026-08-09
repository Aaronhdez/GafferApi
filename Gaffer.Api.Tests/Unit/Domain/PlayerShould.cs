using AwesomeAssertions;
using Gaffer.Api.Domain;
using Gaffer.Api.Tests.TestShared;
using NUnit.Framework.Internal;

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
        var name = _player.FirstName;
        
        _player.GetFirstName().Should().Be(TestDefaultValues.DefaultFirstName);
    }    
    
    [Test]
    public void ReturnLastName()
    {
        var name = _player.LastName;
        
        _player.GetLastName().Should().Be(TestDefaultValues.DefaultLastName);
    }
}