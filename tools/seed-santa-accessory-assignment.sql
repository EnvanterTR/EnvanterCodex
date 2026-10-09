-- Add one representative open accessory assignment to an existing Santa demo tenant.
-- Safe to rerun: the fixture is identified by its note and target employee.
\set ON_ERROR_STOP on
BEGIN;

DO $$
DECLARE
    company_id uuid;
    accessory_id uuid;
    employee_id uuid;
    actor_id uuid;
    assignment_id uuid;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext('envantertr:santa:accessory-assignment:v1'));
    SELECT "Id" INTO company_id FROM "Companies" WHERE "Slug"='santa-demo';
    IF company_id IS NULL THEN
        RAISE EXCEPTION 'Santa demo tenant was not found; no changes were made.';
    END IF;
    SELECT "Id" INTO accessory_id FROM "Accessories"
      WHERE "CompanyId"=company_id AND "ItemNumber"='SNT-ACC-001' FOR UPDATE;
    SELECT "Id" INTO employee_id FROM "Employees"
      WHERE "CompanyId"=company_id AND "EmployeeNumber"='SNT-002' AND "IsActive";
    SELECT "UserId" INTO actor_id FROM "CompanyMemberships"
      WHERE "CompanyId"=company_id AND "RoleKey"='owner' LIMIT 1;
    IF accessory_id IS NULL OR employee_id IS NULL OR actor_id IS NULL THEN
        RAISE EXCEPTION 'Required Santa demo accessory, employee, or owner membership is missing; no changes were made.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM "AccessoryAssignments"
        WHERE "CompanyId"=company_id AND "AccessoryId"=accessory_id AND "EmployeeId"=employee_id
          AND "Notes"='Santa demo · örnek aksesuar zimmeti'
    ) THEN
        INSERT INTO "AccessoryAssignments" ("Id","CompanyId","AccessoryId","TargetType","EmployeeId","Quantity","ReturnedQuantity","Notes","CheckedOutAt")
        VALUES (gen_random_uuid(),company_id,accessory_id,'Employee',employee_id,1,0,'Santa demo · örnek aksesuar zimmeti',now())
        RETURNING "Id" INTO assignment_id;
        INSERT INTO "AccessoryMovements" ("Id","CompanyId","AccessoryId","AssignmentId","EmployeeId","ActorUserId","MovementType","Quantity","Notes","OccurredAt")
        VALUES (gen_random_uuid(),company_id,accessory_id,assignment_id,employee_id,actor_id,'Checkout',1,'Santa demo · Ece Yıldız için örnek zimmet',now());
        INSERT INTO "AuditEntries" ("Id","CompanyId","ActorUserId","TargetType","TargetId","Action","Details","OccurredAt")
        VALUES (gen_random_uuid(),company_id,actor_id,'Accessory',accessory_id,'checked_out','1 adet aksesuar çalışana zimmetlendi.',now());
        UPDATE "Accessories" SET "UpdatedAt"=now() WHERE "CompanyId"=company_id AND "Id"=accessory_id;
    END IF;
END $$;

COMMIT;
