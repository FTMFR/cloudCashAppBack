using BnpCashClaudeApp.Application.Interfaces;
using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// Explicit resource/action policy map with deny-by-default behavior.
    /// </summary>
    public class ResourceAuthorizationPolicyService : IResourceAuthorizationPolicyService
    {
        private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedResourceActions =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Dashboard"] = CreateSet("Read"),
                ["Users"] = CreateSet(
                    "Menu", "Read", "ReadById", "Create", "Update", "Delete", "Activate",
                    "ResetPassword", "Unlock", "LockoutStatus", "Export",
                    "UploadProfilePicture", "DeleteProfilePicture"),
                ["Groups"] = CreateSet("Menu", "Read", "Create", "Update", "Delete", "ManagePermissions"),
                ["Menus"] = CreateSet("Menu", "Read", "Create", "Update", "Delete"),
                ["Permissions"] = CreateSet("Menu", "Read", "Manage"),
                ["AuditLog"] = CreateSet(
                    "Menu", "Read", "Search", "Export", "Statistics",
                    "Today", "FailedLogins", "EventTypes", "UserLogs", "Admin"),
                ["Security"] = CreateSet(
                    "Menu", "Read", "Manage", "PasswordPolicy", "LockoutPolicy",
                    "TerminateSessions", "Write", "Delete", "UsersStatus",
                    "HealthCheck", "EnvironmentInfo", "KeyRead", "KeyCreate",
                    "KeyRotate", "KeyDestroy", "Admin", "DataIntegrity"),
                ["Sessions"] = CreateSet("Read", "ReadAll", "Revoke", "RevokeAll"),
                ["MFA"] = CreateSet("Read", "Manage", "Reset"),
                ["Auth"] = CreateSet("Login", "Logout", "LogoutAll", "ChangePassword"),
                ["System"] = CreateSet(
                    "Version.Menu", "Version.Read", "Version.BackendCheck",
                    "Version.FrontendCheck", "Version.Info", "Version.VerifySignature"),
                ["DataExport"] = CreateSet(
                    "Read", "Settings", "Rules", "Masking", "Audit", "SensitivityLevel", "Admin"),
                ["Shobes"] = CreateSet("Menu", "Read", "Create", "Update", "Delete", "Settings"),
                ["Management"] = CreateSet(
                    "Dashboard.Read",
                    "Software.Read", "Software.Create", "Software.Update", "Software.Delete",
                    "Plan.Read", "Plan.Create", "Plan.Update", "Plan.Delete",
                    "Customer.Read", "Customer.Create", "Customer.Update", "Customer.Delete",
                    "Subscription.Read", "Subscription.Create", "Subscription.Update", "Subscription.Delete",
                    "Database.Read", "Database.Create", "Database.Update", "Database.Delete"),
                ["TafsiliType"] = CreateSet("Read", "Create", "Update", "Delete"),
                ["AzaNoe"] = CreateSet("Read", "Create", "Update", "Delete"),
                ["Sarfasl"] = CreateSet("Read", "Create", "Update", "Delete")
            };

        public ResourceAuthorizationPolicyResult ValidatePermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                return ResourceAuthorizationPolicyResult.Denied("Permission is empty.");
            }

            var normalized = permission.Trim();
            var separatorIndex = normalized.IndexOf('.');

            if (separatorIndex <= 0 || separatorIndex >= normalized.Length - 1)
            {
                return ResourceAuthorizationPolicyResult.Denied(
                    $"Permission '{normalized}' has invalid format. Expected Resource.Action.");
            }

            var resource = normalized.Substring(0, separatorIndex);
            var action = normalized.Substring(separatorIndex + 1);

            if (!AllowedResourceActions.TryGetValue(resource, out var allowedActions))
            {
                return ResourceAuthorizationPolicyResult.Denied(
                    $"Resource '{resource}' is not defined in authorization policy.");
            }

            if (!allowedActions.Contains(action))
            {
                return ResourceAuthorizationPolicyResult.Denied(
                    $"Action '{action}' is not allowed for resource '{resource}'.");
            }

            return ResourceAuthorizationPolicyResult.Allowed(resource, action);
        }

        private static HashSet<string> CreateSet(params string[] actions)
            => new(actions, StringComparer.OrdinalIgnoreCase);
    }
}
