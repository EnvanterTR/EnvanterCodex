# Named role scope audit — 27 October 2026

Requirement: Hedef7.6 explicitly names platform superadmin, tenantadmin, editor, reader, restricted and accounting. This is a source/assertion inspection, not a new executed test or role implementation.

Current CompanyRoleAuthorization.BuiltInRoles defines seven company roles: owner,admin,asset_manager,asset_operator,auditor,viewer,hr_operator. Platform administrators are separate PlatformAdministrators records, not a tenant role. The built-in permission vocabulary has six groups: assets.read,assets.write,people.read,people.manage,people.documents,company.settings. CompanyRoleEndpoints supports company-specific custom roles with these permissions and rejects company-location-restricted access to its role-management surface.

| Requested concept | Current source mapping | Gap |
|---|---|---|
| Platform superadmin | PlatformAdministrators + separate platform session/policies | Never treat owner as platform authority |
| Tenant admin | owner/admin | Representative API matrix exists; complete UI/per-feature scope remains open |
| Editor | asset_operator for assets/stocks; asset_manager also manages people | Existing names have specific grants; no claim that arbitrary editor meaning is fully met |
| Reader | viewer | Asset read only; auditor additionally reads people; these are different |
| Restricted | CompanyLocationAccess/custom-role infrastructure | Not a named built-in role; combined location/person/permission acceptance must be traced |
| Accounting | No named built-in accounting role in the inspected role catalog | Explicit requirement not fulfilled by a generic custom-role possibility. Financial/report permissions and UI scope need specification/implementation during the authorized parity phase |

The existing Company_roles_follow_the_asset_stock_employee_and_settings_policy_matrix test iterates seven roles and checks representative asset/stock/employee/private-file/settings/platform boundaries. Several allowed mutation assertions only require status !=403, using deliberately invalid payloads; this proves an authorization-gate distinction, not successful CRUD, complete fields or every endpoint/role combination. Every_api_route_policy_matches_the_company_and_platform_role_matrix inventories endpoint policy metadata and runtime denials separately; it is not all-field UI acceptance.

No speculative accounting permissions were granted, no existing role renamed and no customer membership changed. Hedef17.3 places full role/parity expansion inP2 afterP0/P1; this audit does not silently advance phases. P0 customer/operator acceptance stays open. The earlier generic role trace in FEATURE-MATRIX is now backed by this precise mismatch; full named-role requirement remains DOĞRULANMADI / incomplete.
