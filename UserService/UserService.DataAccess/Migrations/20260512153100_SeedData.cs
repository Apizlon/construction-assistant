using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.DataAccess.Migrations;

public partial class SeedData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "Users",
            columns: new[] { "Id", "Email", "PasswordHash", "RegistrationDate", "Role" },
            values: new object[]
            {
                new Guid("550e8400-e29b-41d4-a716-446655440000"),
                "admin@example.com",
                "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "Admin"
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "Users",
            keyColumn: "Id",
            keyValue: new Guid("550e8400-e29b-41d4-a716-446655440000"));
    }
}

