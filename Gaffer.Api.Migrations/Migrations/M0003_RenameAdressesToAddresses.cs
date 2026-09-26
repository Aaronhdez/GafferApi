using System.Data;
using FluentMigrator;

namespace Gaffer.Api.Migrations.Migrations;

/// <summary>
/// Fixes the "Adresses" typo from M0002: renames the table (rows are kept), recreates the foreign key
/// with the corrected name and recreates the delete trigger, whose body references the table by name.
/// </summary>
[Migration(202609261200)]
public class M0003_RenameAdressesToAddresses : Migration
{
    private const string TriggerName = "TRG_Facilities_BeforeDelete";

    public override void Up()
    {
        Execute.Sql($"DROP TRIGGER IF EXISTS {TriggerName};");
        Delete.ForeignKey("FK_Facilities_Addresses_Adresses").OnTable("Facilities_Addresses");

        Rename.Table("Adresses").To("Addresses");

        Create.ForeignKey("FK_Facilities_Addresses_Addresses")
            .FromTable("Facilities_Addresses").ForeignColumn("AddressId")
            .ToTable("Addresses").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);
        Execute.Sql(CreateTriggerSql("Addresses"));
    }

    public override void Down()
    {
        Execute.Sql($"DROP TRIGGER IF EXISTS {TriggerName};");
        Delete.ForeignKey("FK_Facilities_Addresses_Addresses").OnTable("Facilities_Addresses");

        Rename.Table("Addresses").To("Adresses");

        Create.ForeignKey("FK_Facilities_Addresses_Adresses")
            .FromTable("Facilities_Addresses").ForeignColumn("AddressId")
            .ToTable("Adresses").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);
        Execute.Sql(CreateTriggerSql("Adresses"));
    }

    private static string CreateTriggerSql(string addressesTable) => $@"
            CREATE TRIGGER {TriggerName}
            BEFORE DELETE ON Facilities
            FOR EACH ROW
            BEGIN
                DELETE FROM Stands
                WHERE Id IN (
                    SELECT StandId FROM Facilities_Stands WHERE FacilityId = OLD.Id
                );

                DELETE FROM {addressesTable}
                WHERE Id IN (
                    SELECT AddressId FROM Facilities_Addresses WHERE FacilityId = OLD.Id
                );
            END;";
}
