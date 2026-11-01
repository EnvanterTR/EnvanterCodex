using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnvanterTR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoverableCompanyDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Companies_DeletedInactive",
                table: "Companies",
                sql: "\"DeletedAt\" IS NULL OR NOT \"IsActive\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // V1 forbids destructive migrations. Keep deletion markers and the access invariant.
            // Operational rollback uses the verified database/media/key/image backup procedure.
            throw new NotSupportedException("Recoverable company deletion cannot be rolled back by dropping its data column. Use the verified backup recovery procedure.");
        }
    }
}
