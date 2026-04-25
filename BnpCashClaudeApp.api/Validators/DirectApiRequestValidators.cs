using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.api.Controllers.Log;
using BnpCashClaudeApp.api.Controllers.Navigation;
using FluentValidation;
using System;
using System.Linq;

namespace BnpCashClaudeApp.api.Validators
{
    public class LoginRequestDtoValidator : AbstractValidator<AuthController.LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty()
                .MaximumLength(128);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MaximumLength(256);
        }
    }

    public class ChangePasswordRequestDtoValidator : AbstractValidator<AuthController.ChangePasswordRequestDto>
    {
        public ChangePasswordRequestDtoValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(256);

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty()
                .Equal(x => x.NewPassword);
        }
    }

    public class MfaVerifyRequestDtoValidator : AbstractValidator<AuthController.MfaVerifyRequestDto>
    {
        public MfaVerifyRequestDtoValidator()
        {
            RuleFor(x => x.MfaToken)
                .NotEmpty()
                .MaximumLength(4096);

            RuleFor(x => x.Code)
                .NotEmpty()
                .Length(4, 32);

            RuleFor(x => x.CaptchaId)
                .MaximumLength(256);

            RuleFor(x => x.CaptchaCode)
                .MaximumLength(32);

            RuleFor(x => x.CaptchaCode)
                .NotEmpty()
                .When(x => !string.IsNullOrWhiteSpace(x.CaptchaId));

            RuleFor(x => x.CaptchaId)
                .NotEmpty()
                .When(x => !string.IsNullOrWhiteSpace(x.CaptchaCode));
        }
    }

    public class ForgotPasswordRequestDtoValidator : AbstractValidator<AuthController.ForgotPasswordRequestDto>
    {
        public ForgotPasswordRequestDtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty()
                .MaximumLength(128);
        }
    }

    public class VerifyPasswordResetOtpRequestDtoValidator : AbstractValidator<AuthController.VerifyPasswordResetOtpRequestDto>
    {
        public VerifyPasswordResetOtpRequestDtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty()
                .MaximumLength(128);

            RuleFor(x => x.OtpCode)
                .NotEmpty()
                .Length(4, 16);
        }
    }

    public class ResetPasswordWithTokenRequestDtoValidator : AbstractValidator<AuthController.ResetPasswordWithTokenRequestDto>
    {
        public ResetPasswordWithTokenRequestDtoValidator()
        {
            RuleFor(x => x.PasswordResetToken)
                .NotEmpty()
                .MaximumLength(4096);

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(256);

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty()
                .Equal(x => x.NewPassword);
        }
    }

    //public class CreateKeyRequestValidator : AbstractValidator<CreateKeyRequest>
    //{
    //    public CreateKeyRequestValidator(ICryptographicAlgorithmPolicyService cryptographicAlgorithmPolicyService)
    //    {
    //        var approvedManagedKeyLengths = cryptographicAlgorithmPolicyService
    //            .GetApprovedManagedKeyLengths()
    //            .ToArray();

    //        RuleFor(x => x.Purpose)
    //            .NotEmpty()
    //            .MaximumLength(100);

    //        RuleFor(x => x.KeyLengthBits)
    //            .Must(v => approvedManagedKeyLengths.Contains(v))
    //            .WithMessage($"KeyLengthBits must be one of: {string.Join(", ", approvedManagedKeyLengths)}.");
    //    }
    //}

    //public class RotateKeyRequestValidator : AbstractValidator<RotateKeyRequest>
    //{
    //    public RotateKeyRequestValidator(ICryptographicAlgorithmPolicyService cryptographicAlgorithmPolicyService)
    //    {
    //        var approvedManagedKeyLengths = cryptographicAlgorithmPolicyService
    //            .GetApprovedManagedKeyLengths()
    //            .ToArray();

    //        RuleFor(x => x.KeyLengthBits)
    //            .Must(v => approvedManagedKeyLengths.Contains(v))
    //            .WithMessage($"KeyLengthBits must be one of: {string.Join(", ", approvedManagedKeyLengths)}.");

    //        RuleFor(x => x.GracePeriodMinutes)
    //            .InclusiveBetween(0, 1440);
    //    }
    //}

    //public class DeactivateKeyRequestValidator : AbstractValidator<DeactivateKeyRequest>
    //{
    //    public DeactivateKeyRequestValidator()
    //    {
    //        RuleFor(x => x.Reason)
    //            .NotEmpty()
    //            .MaximumLength(500);
    //    }
    //}

    //public class DestroyKeyRequestValidator : AbstractValidator<DestroyKeyRequest>
    //{
    //    public DestroyKeyRequestValidator()
    //    {
    //        RuleFor(x => x.Reason)
    //            .NotEmpty()
    //            .MaximumLength(500);
    //    }
    //}

    //public class ValidateKeyRequestValidator : AbstractValidator<ValidateKeyRequest>
    //{
    //    public ValidateKeyRequestValidator()
    //    {
    //        RuleFor(x => x.KeyBase64)
    //            .NotEmpty()
    //            .MaximumLength(8192);

    //        RuleFor(x => x.MinimumBits)
    //            .InclusiveBetween(128, 4096)
    //            .Must(v => v % 8 == 0)
    //            .WithMessage("MinimumBits must be divisible by 8.");
    //    }
    //}

    public class GrantPermissionRequestValidator : AbstractValidator<PermissionController.GrantPermissionRequest>
    {
        public GrantPermissionRequestValidator()
        {
            RuleFor(x => x.GroupId).GreaterThan(0);
            RuleFor(x => x.PermissionId).GreaterThan(0);
        }
    }

    public class BulkGrantPermissionRequestValidator : AbstractValidator<PermissionController.BulkGrantPermissionRequest>
    {
        public BulkGrantPermissionRequestValidator()
        {
            RuleFor(x => x.GroupId).GreaterThan(0);

            RuleFor(x => x.PermissionIds)
                .NotNull()
                .Must(ids => ids.Count > 0)
                .WithMessage("At least one permission id is required.")
                .Must(ids => ids.Count <= 1000)
                .WithMessage("PermissionIds count exceeds allowed limit.");

            RuleForEach(x => x.PermissionIds).GreaterThan(0);
        }
    }

    public class MenuPermissionDetailDtoValidator : AbstractValidator<PermissionController.MenuPermissionDetailDto>
    {
        public MenuPermissionDetailDtoValidator()
        {
            RuleFor(x => x.PermissionId).GreaterThan(0);
            RuleFor(x => x.PermissionName).MaximumLength(200);
        }
    }

    public class AssignMenuPermissionRequestValidator : AbstractValidator<PermissionController.AssignMenuPermissionRequest>
    {
        public AssignMenuPermissionRequestValidator()
        {
            RuleFor(x => x.MenuId).GreaterThan(0);
            RuleFor(x => x.PermissionId).GreaterThan(0);
        }
    }

    public class BulkAssignMenuPermissionRequestValidator : AbstractValidator<PermissionController.BulkAssignMenuPermissionRequest>
    {
        public BulkAssignMenuPermissionRequestValidator()
        {
            RuleFor(x => x.MenuId).GreaterThan(0);

            RuleFor(x => x.Permissions)
                .NotNull()
                .Must(items => items.Count > 0)
                .WithMessage("At least one permission item is required.")
                .Must(items => items.Count <= 1000)
                .WithMessage("Permissions count exceeds allowed limit.");

            RuleForEach(x => x.Permissions).SetValidator(new MenuPermissionDetailDtoValidator());
        }
    }

    public class PermissionAccessItemValidator : AbstractValidator<PermissionController.PermissionAccessItem>
    {
        public PermissionAccessItemValidator()
        {
            RuleFor(x => x.PermissionId).GreaterThan(0);
        }
    }

    public class UpdateGroupAccessRequestValidator : AbstractValidator<PermissionController.UpdateGroupAccessRequest>
    {
        public UpdateGroupAccessRequestValidator()
        {
            RuleFor(x => x.AccessItems)
                .NotNull()
                .Must(items => items.Count <= 5000)
                .WithMessage("AccessItems count exceeds allowed limit.");

            RuleForEach(x => x.AccessItems).SetValidator(new PermissionAccessItemValidator());
        }
    }

    public class UpdateAuditLogProtectionSettingsDtoValidator : AbstractValidator<UpdateAuditLogProtectionSettingsDto>
    {
        public UpdateAuditLogProtectionSettingsDtoValidator()
        {
            RuleFor(x => x.MaxRetryAttempts).InclusiveBetween(1, 20);
            RuleFor(x => x.RetentionDays).InclusiveBetween(1, 36500);
            RuleFor(x => x.ArchiveAfterDays).InclusiveBetween(1, 36500);
            RuleFor(x => x.BackupIntervalHours).InclusiveBetween(1, 720);
            RuleFor(x => x.RetentionCheckIntervalHours).InclusiveBetween(1, 720);
            RuleFor(x => x.FallbackRecoveryIntervalMinutes).InclusiveBetween(1, 1440);
            RuleFor(x => x.HealthCheckIntervalMinutes).InclusiveBetween(1, 1440);

            RuleFor(x => x)
                .Must(x => x.ArchiveAfterDays <= x.RetentionDays)
                .WithMessage("ArchiveAfterDays must be less than or equal to RetentionDays.");

            RuleFor(x => x.AlertEmailAddresses).MaximumLength(4000);
            RuleFor(x => x.AlertSmsNumbers).MaximumLength(4000);
            RuleFor(x => x.FallbackDirectory).MaximumLength(2000);
            RuleFor(x => x.BackupDirectory).MaximumLength(2000);
            RuleFor(x => x.ArchiveDirectory).MaximumLength(2000);
        }
    }

    public class UpdateAccountLockoutSettingsDtoValidator : AbstractValidator<UpdateAccountLockoutSettingsDto>
    {
        public UpdateAccountLockoutSettingsDtoValidator()
        {
            RuleFor(x => x.MaxFailedAttempts).InclusiveBetween(1, 100);
            RuleFor(x => x.LockoutDurationMinutes).InclusiveBetween(1, 1440);
            RuleFor(x => x.PermanentLockoutThreshold).InclusiveBetween(1, 1000);
            RuleFor(x => x.FailedAttemptResetMinutes).InclusiveBetween(1, 1440);

            RuleFor(x => x)
                .Must(x => !x.EnablePermanentLockout || x.PermanentLockoutThreshold >= x.MaxFailedAttempts)
                .WithMessage("PermanentLockoutThreshold must be greater than or equal to MaxFailedAttempts.");
        }
    }

    public class UpdatePasswordPolicySettingsDtoValidator : AbstractValidator<UpdatePasswordPolicySettingsDto>
    {
        public UpdatePasswordPolicySettingsDtoValidator()
        {
            RuleFor(x => x.MinimumLength).InclusiveBetween(6, 256);
            RuleFor(x => x.MaximumLength).InclusiveBetween(6, 256);
            RuleFor(x => x.PasswordHistoryCount).InclusiveBetween(0, 50);
            RuleFor(x => x.PasswordExpirationDays).InclusiveBetween(0, 3650);
            RuleFor(x => x.SpecialCharacters).MaximumLength(128);

            RuleFor(x => x)
                .Must(x => x.MaximumLength >= x.MinimumLength)
                .WithMessage("MaximumLength must be greater than or equal to MinimumLength.");
        }
    }

    public class UpdateCaptchaSettingsDtoValidator : AbstractValidator<UpdateCaptchaSettingsDto>
    {
        public UpdateCaptchaSettingsDtoValidator()
        {
            RuleFor(x => x.CodeLength).InclusiveBetween(3, 10);
            RuleFor(x => x.ExpiryMinutes).InclusiveBetween(1, 60);
            RuleFor(x => x.NoiseLineCount).InclusiveBetween(0, 500);
            RuleFor(x => x.NoiseDotCount).InclusiveBetween(0, 10000);
            RuleFor(x => x.ImageWidth).InclusiveBetween(80, 2000);
            RuleFor(x => x.ImageHeight).InclusiveBetween(30, 1000);
        }
    }

    public class UpdateMfaSettingsDtoValidator : AbstractValidator<UpdateMfaSettingsDto>
    {
        public UpdateMfaSettingsDtoValidator()
        {
            RuleFor(x => x.OtpLength).InclusiveBetween(4, 10);
            RuleFor(x => x.OtpExpirySeconds).InclusiveBetween(30, 1800);
            RuleFor(x => x.RecoveryCodesCount).InclusiveBetween(0, 100);
            RuleFor(x => x.MaxFailedOtpAttempts).InclusiveBetween(1, 20);
            RuleFor(x => x.LockoutDurationMinutes).InclusiveBetween(1, 1440);
        }
    }

    public class DataExportSettingsValidator : AbstractValidator<DataExportSettings>
    {
        public DataExportSettingsValidator()
        {
            RuleFor(x => x.MaxRecordsPerExport).InclusiveBetween(1, 1_000_000);
            RuleFor(x => x.MaxExportSizeBytes).InclusiveBetween(1024, 2L * 1024 * 1024 * 1024);
            RuleFor(x => x.AllowedFormats)
                .NotNull()
                .Must(items => items.Count > 0)
                .WithMessage("At least one export format is required.")
                .Must(items => items.Count <= 20)
                .WithMessage("AllowedFormats count exceeds allowed limit.");
            RuleForEach(x => x.AllowedFormats)
                .NotEmpty()
                .MaximumLength(20);
            RuleFor(x => x.DefaultSensitivityLevel).NotEmpty().MaximumLength(64);
            RuleFor(x => x.SignatureAlgorithm).NotEmpty().MaximumLength(128);
            RuleFor(x => x.AuditLogRetentionDays).InclusiveBetween(1, 36500);
        }
    }

    public abstract class ExportRuleRequestValidatorBase<TRequest> : AbstractValidator<TRequest>
        where TRequest : CreateExportRuleRequest
    {
        protected ExportRuleRequestValidatorBase()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.RuleType).IsInEnum();
            RuleFor(x => x.EntityType).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Condition).MaximumLength(2000);
            RuleFor(x => x.Action).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Priority).InclusiveBetween(0, 10_000);
        }
    }

    public class CreateExportRuleRequestValidator : ExportRuleRequestValidatorBase<CreateExportRuleRequest>
    {
    }

    public class UpdateExportRuleRequestValidator : ExportRuleRequestValidatorBase<UpdateExportRuleRequest>
    {
    }

    public abstract class MaskingRuleRequestValidatorBase<TRequest> : AbstractValidator<TRequest>
        where TRequest : CreateMaskingRuleRequest
    {
        protected MaskingRuleRequestValidatorBase()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.EntityType).NotEmpty().MaximumLength(200);
            RuleFor(x => x.FieldName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MaskingType).IsInEnum();
            RuleFor(x => x.MaskPattern).NotEmpty().MaximumLength(128);
            RuleFor(x => x.VisibleCharsStart).InclusiveBetween(0, 64);
            RuleFor(x => x.VisibleCharsEnd).InclusiveBetween(0, 64);
            RuleFor(x => x.ExcludePermissions).MaximumLength(2000);
            RuleFor(x => x)
                .Must(x => x.VisibleCharsStart + x.VisibleCharsEnd <= 64)
                .WithMessage("VisibleCharsStart + VisibleCharsEnd must be less than or equal to 64.");
        }
    }

    public class CreateMaskingRuleRequestValidator : MaskingRuleRequestValidatorBase<CreateMaskingRuleRequest>
    {
    }

    public class UpdateMaskingRuleRequestValidator : MaskingRuleRequestValidatorBase<UpdateMaskingRuleRequest>
    {
    }

    public class SignatureVerificationRequestValidator : AbstractValidator<VersionController.SignatureVerificationRequest>
    {
        public SignatureVerificationRequestValidator()
        {
            RuleFor(x => x.Signature)
                .NotEmpty()
                .MaximumLength(20000);

            RuleFor(x => x)
                .Must(x =>
                    !string.IsNullOrWhiteSpace(x.FileContentBase64) ||
                    !string.IsNullOrWhiteSpace(x.FileHash))
                .WithMessage("Either FileContentBase64 or FileHash must be provided.");

            RuleFor(x => x.FileContentBase64).MaximumLength(100 * 1024 * 1024);
            RuleFor(x => x.FileHash).MaximumLength(2000);
        }
    }

    public class MetadataSignatureRequestValidator : AbstractValidator<VersionController.MetadataSignatureRequest>
    {
        public MetadataSignatureRequestValidator()
        {
            RuleFor(x => x.Version).NotEmpty().MaximumLength(64);
            RuleFor(x => x.BuildDate).MaximumLength(128);
            RuleFor(x => x.BuildNumber).MaximumLength(128);
            RuleFor(x => x.Signature).NotEmpty().MaximumLength(20000);
        }
    }

    public class ComputeHashRequestValidator : AbstractValidator<VersionController.ComputeHashRequest>
    {
        public ComputeHashRequestValidator()
        {
            RuleFor(x => x.FileContentBase64)
                .NotEmpty()
                .MaximumLength(100 * 1024 * 1024);
        }
    }

    public class CreateBackupRequestValidator : AbstractValidator<CreateBackupRequest>
    {
        public CreateBackupRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be less than or equal to ToDate.");
        }
    }

    public class ArchiveRequestValidator : AbstractValidator<ArchiveRequest>
    {
        public ArchiveRequestValidator()
        {
            RuleFor(x => x.OlderThanDays).InclusiveBetween(1, 36500);
        }
    }

    public class ContextAccessControlSettingsValidator : AbstractValidator<ContextAccessControlSettings>
    {
        public ContextAccessControlSettingsValidator()
        {
            RuleFor(x => x.IpRestrictionMode).IsInEnum();
            RuleFor(x => x.ConcurrentSessionAction).IsInEnum();

            RuleForEach(x => x.AllowedIpAddresses).MaximumLength(128);
            RuleForEach(x => x.BlockedIpAddresses).MaximumLength(128);
            RuleForEach(x => x.BlockedUserAgentPatterns).MaximumLength(512);

            RuleForEach(x => x.AllowedDaysOfWeek).InclusiveBetween(0, 6);

            RuleForEach(x => x.AllowedCountries)
                .Must(IsValidCountryCode)
                .WithMessage("Country code must be exactly 2 letters.");

            RuleForEach(x => x.BlockedCountries)
                .Must(IsValidCountryCode)
                .WithMessage("Country code must be exactly 2 letters.");

            When(x => x.EnableTimeRestriction, () =>
            {
                RuleFor(x => x.AllowedStartTime)
                    .Must(IsValidTime)
                    .WithMessage("AllowedStartTime must be in HH:mm format.");

                RuleFor(x => x.AllowedEndTime)
                    .Must(IsValidTime)
                    .WithMessage("AllowedEndTime must be in HH:mm format.");

                RuleFor(x => x.AllowedDaysOfWeek)
                    .NotNull()
                    .Must(days => days.Count > 0)
                    .WithMessage("At least one allowed day of week is required when time restriction is enabled.");

                RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(128);
            });

            RuleFor(x => x.MaxConcurrentSessions)
                .InclusiveBetween(1, 500)
                .When(x => x.EnableConcurrentSessionLimit);

            RuleFor(x => x.MaxAllowedRiskScore).InclusiveBetween(0, 100);

            RuleFor(x => x)
                .Must(x =>
                    !x.EnableDeviceRestriction ||
                    x.AllowMobileDevices ||
                    x.AllowDesktopDevices ||
                    x.AllowTabletDevices)
                .WithMessage("At least one device type must be allowed when device restriction is enabled.");
        }

        private static bool IsValidTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return TimeSpan.TryParse(value, out _);
        }

        private static bool IsValidCountryCode(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Length == 2 &&
                   value.All(char.IsLetter);
        }
    }

    public class AuditLogFilterDtoValidator : AbstractValidator<AuditLogController.AuditLogFilterDto>
    {
        public AuditLogFilterDtoValidator()
        {
            RuleFor(x => x.FromDate).MaximumLength(64);
            RuleFor(x => x.ToDate).MaximumLength(64);
            RuleFor(x => x.EventType).MaximumLength(128);
            RuleFor(x => x.UserName).MaximumLength(256);
            RuleFor(x => x.IpAddress).MaximumLength(128);
            RuleFor(x => x.EntityType).MaximumLength(128);

            RuleFor(x => x.PageNumber).InclusiveBetween(1, 1_000_000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 1000);
        }
    }

    public class ExportUsersFilterValidator : AbstractValidator<UsersController.ExportUsersFilter>
    {
        public ExportUsersFilterValidator()
        {
            RuleFor(x => x.UserName).MaximumLength(128);
        }
    }
}
