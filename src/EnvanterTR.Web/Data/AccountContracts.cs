namespace EnvanterTR.Web.Data;

public sealed record InitialOwnerRequest(string CompanyName, string FullName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password, Guid? CompanyId, bool RememberMe = false);
public sealed record PlatformBootstrapRequest(string FullName, string Email, string Password);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record ChangeMemberRoleRequest(string RoleKey);
public sealed record InviteCompanyMemberRequest(string FullName, string Email, string RoleKey);
