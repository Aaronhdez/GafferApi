using System.Data;
using FluentMigrator;

namespace Gaffer.Api.Migrations.Migrations;

[Migration(202608131010)]
public class M0002_CreateFacilitiesSchema : Migration {
    public override void Up() {
        Create.Table("Facilities")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("Name").AsString().NotNullable()
            .WithColumn("Active").AsBoolean().NotNullable()
            .WithColumn("BuildDate").AsDateTime().NotNullable()
            .WithColumn("Type").AsString().NotNullable()
            .WithColumn("Capacity").AsInt32().NotNullable();

        Create.Table("Adresses")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("Street").AsString().NotNullable()
            .WithColumn("StreetNumber").AsString().NotNullable()
            .WithColumn("PostalCode").AsString().NotNullable()
            .WithColumn("City").AsString().NotNullable()
            .WithColumn("Region").AsString().NotNullable()
            .WithColumn("Country").AsString().NotNullable();

        Create.Table("Stands")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("StandName").AsString().NotNullable()
            .WithColumn("StandCapacity").AsInt32().NotNullable();

        Create.Table("Facilities_Addresses")
            .WithColumn("FacilityId").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("AddressId").AsGuid().NotNullable().Unique();

        Create.Table("Facilities_Stands")
            .WithColumn("FacilityId").AsGuid().NotNullable()
            .WithColumn("StandId").AsGuid().NotNullable().Unique();

        Create.PrimaryKey("PK_Facilities_Stands")
            .OnTable("Facilities_Stands")
            .Columns("FacilityId", "StandId");

        Create.ForeignKey("FK_Facilities_Addresses_Facilities")
            .FromTable("Facilities_Addresses").ForeignColumn("FacilityId")
            .ToTable("Facilities").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_Facilities_Addresses_Adresses")
            .FromTable("Facilities_Addresses").ForeignColumn("AddressId")
            .ToTable("Adresses").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_Facilities_Stands_Facilities")
            .FromTable("Facilities_Stands").ForeignColumn("FacilityId")
            .ToTable("Facilities").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_Facilities_Stands_Stands")
            .FromTable("Facilities_Stands").ForeignColumn("StandId")
            .ToTable("Stands").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Execute.Sql(@"
            CREATE TRIGGER TRG_Facilities_BeforeDelete
            BEFORE DELETE ON Facilities
            FOR EACH ROW
            BEGIN
                DELETE FROM Stands
                WHERE Id IN (
                    SELECT StandId FROM Facilities_Stands WHERE FacilityId = OLD.Id
                );

                DELETE FROM Adresses
                WHERE Id IN (
                    SELECT AddressId FROM Facilities_Addresses WHERE FacilityId = OLD.Id
                );
            END;");
    }

    public override void Down() {
        Execute.Sql("DROP TRIGGER IF EXISTS TRG_Facilities_BeforeDelete;");
        Delete.Table("Facilities_Stands");
        Delete.Table("Facilities_Addresses");
        Delete.Table("Stands");
        Delete.Table("Adresses");
        Delete.Table("Facilities");
    }
}
