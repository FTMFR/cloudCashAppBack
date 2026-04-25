namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// Central resource/action policy validator for permission names.
    /// </summary>
    public interface IResourceAuthorizationPolicyService
    {
        ResourceAuthorizationPolicyResult ValidatePermission(string permission);
    }

    public sealed class ResourceAuthorizationPolicyResult
    {
        private ResourceAuthorizationPolicyResult(
            bool isAllowed,
            string? resource,
            string? action,
            string? denialReason)
        {
            IsAllowed = isAllowed;
            Resource = resource;
            Action = action;
            DenialReason = denialReason;
        }

        public bool IsAllowed { get; }
        public string? Resource { get; }
        public string? Action { get; }
        public string? DenialReason { get; }

        public static ResourceAuthorizationPolicyResult Allowed(string resource, string action)
            => new ResourceAuthorizationPolicyResult(
                isAllowed: true,
                resource: resource,
                action: action,
                denialReason: null);

        public static ResourceAuthorizationPolicyResult Denied(string denialReason)
            => new ResourceAuthorizationPolicyResult(
                isAllowed: false,
                resource: null,
                action: null,
                denialReason: denialReason);
    }
}
