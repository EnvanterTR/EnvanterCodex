using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EnvanterTR.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261024010000_AddPlatformImpersonationSessions")]
public sealed class AddPlatformImpersonationSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>("ImpersonatorUserId", "UserSessions", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>("ImpersonatorSessionId", "UserSessions", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>("ImpersonatorSecurityStampHash", "UserSessions", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>("ImpersonationReturnTokenHash", "UserSessions", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<bool>("ImpersonatorRememberMe", "UserSessions", type: "boolean", nullable: true);
        migrationBuilder.AddColumn<string>("ImpersonationReason", "UserSessions", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("ImpersonationEndedAt", "UserSessions", type: "timestamp with time zone", nullable: true);
        migrationBuilder.CreateIndex("IX_UserSessions_ImpersonationReturnTokenHash", "UserSessions", "ImpersonationReturnTokenHash",
            unique: true, filter: "\"ImpersonationReturnTokenHash\" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_UserSessions_ImpersonatorSessionId", "UserSessions", "ImpersonatorSessionId",
            unique: true, filter: "\"ImpersonatorSessionId\" IS NOT NULL AND \"ImpersonationEndedAt\" IS NULL AND \"RevokedAt\" IS NULL");
        migrationBuilder.AddCheckConstraint("CK_UserSessions_Impersonation", "UserSessions",
            "(\"ImpersonatorUserId\" IS NULL AND \"ImpersonatorSessionId\" IS NULL AND \"ImpersonatorSecurityStampHash\" IS NULL AND \"ImpersonationReturnTokenHash\" IS NULL AND \"ImpersonatorRememberMe\" IS NULL AND \"ImpersonationReason\" IS NULL AND \"ImpersonationEndedAt\" IS NULL) OR " +
            "(\"ImpersonatorUserId\" IS NOT NULL AND \"ImpersonatorSessionId\" IS NOT NULL AND \"ImpersonatorSecurityStampHash\" IS NOT NULL AND \"ImpersonatorRememberMe\" IS NOT NULL AND \"ImpersonationReason\" IS NOT NULL AND \"CompanyId\" IS NOT NULL AND NOT \"PlatformAdmin\" AND (\"ImpersonationReturnTokenHash\" IS NOT NULL OR \"ImpersonationEndedAt\" IS NOT NULL))");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_UserSessions_Impersonation", "UserSessions");
        migrationBuilder.DropIndex("IX_UserSessions_ImpersonationReturnTokenHash", "UserSessions");
        migrationBuilder.DropIndex("IX_UserSessions_ImpersonatorSessionId", "UserSessions");
        foreach (var column in new[] { "ImpersonatorUserId", "ImpersonatorSessionId", "ImpersonatorSecurityStampHash",
            "ImpersonationReturnTokenHash", "ImpersonatorRememberMe", "ImpersonationReason", "ImpersonationEndedAt" })
            migrationBuilder.DropColumn(column, "UserSessions");
    }
}
