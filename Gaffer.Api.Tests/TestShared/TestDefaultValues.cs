using Gaffer.Api.Shared;

namespace Gaffer.Api.Tests.TestShared;

public static class TestDefaultValues {
    public static string TestConnectionString => "Server=127.0.0.1;Port=3306;Database=GafferDB_Test;Uid=root;Pwd=toor;";
    public const string DefaultFirstName = "John";
    public const string DefaultLastName = "Doe";
    public const string DefaultSecondLastName = "Doe";
    public static readonly DateTime DefaultBirthDate = new(1990, 1, 1);
    public const Gender DefaultGender = Gender.MALE;
    public const string PlayersTableName = "Players";
    public static readonly Guid EmptyPlayerId = Guid.Empty;
}