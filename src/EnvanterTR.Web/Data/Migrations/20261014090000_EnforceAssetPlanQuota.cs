using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using EnvanterTR.Web.Data;

namespace EnvanterTR.Web.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014090000_EnforceAssetPlanQuota")]
public sealed class EnforceAssetPlanQuota : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AssetCount",
            table: "Companies",
            type: "integer",
            nullable: false,
            defaultValue: 0);
        migrationBuilder.Sql("""
            UPDATE "Companies" AS company
            SET "AssetCount" = (
                SELECT COUNT(*)::integer FROM "Assets" AS asset WHERE asset."CompanyId" = company."Id"
            );
            ALTER TABLE "Companies" ADD CONSTRAINT "CK_Companies_AssetCount" CHECK ("AssetCount" >= 0);
            """);
        migrationBuilder.Sql("""
            CREATE FUNCTION "MaintainAssetPlanQuota"() RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            DECLARE
                v_max_assets integer;
                v_asset_count integer;
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    UPDATE "Companies" SET "AssetCount" = GREATEST("AssetCount" - 1, 0)
                    WHERE "Id" = OLD."CompanyId";
                    RETURN OLD;
                END IF;

                IF TG_OP = 'UPDATE' AND NEW."CompanyId" = OLD."CompanyId" THEN
                    RETURN NEW;
                END IF;

                IF TG_OP = 'UPDATE' THEN
                    UPDATE "Companies" SET "AssetCount" = GREATEST("AssetCount" - 1, 0)
                    WHERE "Id" = OLD."CompanyId";
                END IF;

                SELECT plan."MaxAssets" INTO v_max_assets
                FROM "TenantSubscriptions" AS subscription
                JOIN "SubscriptionPlans" AS plan ON plan."Id" = subscription."PlanId"
                WHERE subscription."CompanyId" = NEW."CompanyId"
                  AND subscription."Status" IN ('Active', 'Trial')
                  AND subscription."StartsAt" <= NOW()
                  AND subscription."EndsAt" > NOW();

                UPDATE "Companies" AS company
                SET "AssetCount" = company."AssetCount" + 1
                WHERE company."Id" = NEW."CompanyId"
                  AND (v_max_assets IS NULL OR company."AssetCount" < v_max_assets)
                RETURNING "AssetCount" INTO v_asset_count;

                IF NOT FOUND AND EXISTS (SELECT 1 FROM "Companies" WHERE "Id" = NEW."CompanyId") THEN
                    RAISE EXCEPTION USING
                        ERRCODE = 'P0001',
                        MESSAGE = 'asset_quota_exceeded',
                        DETAIL = format('maxAssets=%s', v_max_assets);
                END IF;

                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER "TR_Assets_EnforcePlanQuota"
            BEFORE INSERT OR DELETE ON "Assets"
            FOR EACH ROW EXECUTE FUNCTION "MaintainAssetPlanQuota"();

            CREATE TRIGGER "TR_Assets_MaintainPlanQuotaOnTenantMove"
            BEFORE UPDATE OF "CompanyId" ON "Assets"
            FOR EACH ROW EXECUTE FUNCTION "MaintainAssetPlanQuota"();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS "TR_Assets_EnforcePlanQuota" ON "Assets";
            DROP TRIGGER IF EXISTS "TR_Assets_MaintainPlanQuotaOnTenantMove" ON "Assets";
            DROP FUNCTION IF EXISTS "MaintainAssetPlanQuota"();
            """);
        migrationBuilder.DropCheckConstraint(name: "CK_Companies_AssetCount", table: "Companies");
        migrationBuilder.DropColumn(name: "AssetCount", table: "Companies");
    }
}
