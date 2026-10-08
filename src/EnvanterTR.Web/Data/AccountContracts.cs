namespace EnvanterTR.Web.Data;

public sealed record InitialOwnerRequest(string CompanyName, string FullName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password, Guid? CompanyId, bool RememberMe = false, string? TwoFactorCode = null);
public sealed record PlatformBootstrapRequest(string FullName, string Email, string Password);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record TwoFactorSetupRequest(string CurrentPassword);
public sealed record TwoFactorCodeRequest(string Code);
public sealed record TwoFactorRecoveryRequest(string CurrentPassword, string Code);
public sealed record ChangeOwnPasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword, string? TwoFactorCode);
public sealed record ChangeMemberRoleRequest(string RoleKey);
public sealed record InviteCompanyMemberRequest(string FullName, string Email, string RoleKey);
