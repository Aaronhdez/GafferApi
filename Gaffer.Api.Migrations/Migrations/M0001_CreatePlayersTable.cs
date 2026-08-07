using FluentMigrator;

namespace Gaffer.Api.Migrations.Migrations;

[Migration(202608071730)]
public class M0001_CreatePlayersTable : Migration
{
    public override void Up()
    {
        Create.Table("Players")
            .WithColumn("PlayerId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("FirstName").AsString().NotNullable()
            .WithColumn("LastName").AsString().NotNullable()
            .WithColumn("SecondLastName").AsString().NotNullable()
            .WithColumn("Gender").AsInt32().NotNullable()
            .WithColumn("BirthDate").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Players");
    }
}