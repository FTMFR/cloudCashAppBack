using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Text.Json;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر مدیریت نسخه نرم‌افزار
    /// پیاده‌سازی الزام FPT_TUD_EXT.1.2 و FPT_TUD_EXT.1.3 از استاندارد ISO 15408
    /// 
    /// الزامات پیاده‌سازی شده:
    /// - FPT_TUD_EXT.1.2: بررسی نسخه فعلی نرم‌افزار
    /// - FPT_TUD_EXT.1.3: اعتبارسنجی امضای دیجیتال به‌روزرسانی
    /// 
    /// دسترسی: Health Check endpoint عمومی است، سایر endpoints نیاز به Permission دارند
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("ApiPolicy")]
    public class VersionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<VersionController> _logger;
        private readonly IUpdateSignatureVerificationService _signatureService;
        private readonly IDataExportService _dataExportService;
        private static VersionInfo? _cachedVersionInfo;

        public VersionController(
            IConfiguration configuration,
            IAuditLogService auditLogService,
            ILogger<VersionController> logger,
            IUpdateSignatureVerificationService signatureService,
            IDataExportService dataExportService)
        {
            _configuration = configuration;
            _auditLogService = auditLogService;
            _logger = logger;
            _signatureService = signatureService;
            _dataExportService = dataExportService;
        }

        /// <summary>
        /// Health Check endpoint برای Docker
        /// FPT_TUD_EXT.1.2: بررسی سلامت سیستم
        /// این endpoint عمومی است و نیاز به Authentication ندارد
        /// </summary>
        /// <returns>وضعیت سلامت سیستم</returns>
        [HttpGet("/health")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> HealthCheck(CancellationToken ct = default)
        {
            try
            {
                var healthStatus = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    version = GetVersionInfo().Version,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                };

                var protectedResponse = await ProtectReadPayloadAsync(healthStatus, "SystemHealth", ct: ct);
                return Ok(protectedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(503, new { status = "unhealthy", error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت نسخه فعلی نرم‌افزار
        /// FPT_TUD_EXT.1.2: بررسی نسخه فعلی
        /// </summary>
        /// <returns>اطلاعات نسخه فعلی</returns>
        [HttpGet("current")]
        [RequirePermission("System.Version.Read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetCurrentVersion()
        {
            try
            {
                var versionInfo = GetVersionInfo();
                var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
                var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
                var userName = User.Identity?.Name ?? "Unknown";
                var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");

                // ثبت در Audit Log
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _auditLogService.LogEventAsync(
                            eventType: "VersionInfoRequested",
                            entityType: "System",
                            entityId: "Version",
                            isSuccess: true,
                            ipAddress: ipAddress,
                            userName: userName,
                            userId: userId,
                            userAgent: userAgent,
                            description: $"Version information requested: {versionInfo.Version}",
                            ct: default);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to log version info request");
                    }
                });

                var protectedVersionInfo = await ProtectReadPayloadAsync(versionInfo, "SystemVersion");
                return Ok(protectedVersionInfo);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current version");
                return StatusCode(500, new { error = "خطا در دریافت اطلاعات نسخه" });
            }
        }

        /// <summary>
        /// بررسی وجود نسخه جدید
        /// FPT_TUD_EXT.1.2: مقایسه نسخه‌ها
        /// </summary>
        /// <param name="checkVersion">نسخه مورد بررسی (اختیاری)</param>
        /// <returns>نتیجه بررسی</returns>
        [HttpGet("check")]
        [RequirePermission("System.Version.Read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CheckVersion([FromQuery] string? checkVersion = null)
        {
            try
            {
                var currentVersion = GetVersionInfo();
                var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
                var userName = User.Identity?.Name ?? "Unknown";
                var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");

                var result = new
                {
                    currentVersion = currentVersion.Version,
                    checkVersion = checkVersion,
                    isUpToDate = string.IsNullOrEmpty(checkVersion) || currentVersion.Version == checkVersion,
                    buildDate = currentVersion.BuildDate,
                    buildNumber = currentVersion.BuildNumber,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                };

                // ثبت در Audit Log
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _auditLogService.LogEventAsync(
                            eventType: "VersionCheck",
                            entityType: "System",
                            entityId: "Version",
                            isSuccess: true,
                            ipAddress: ipAddress,
                            userName: userName,
                            userId: userId,
                            description: $"Version check: Current={currentVersion.Version}, Check={checkVersion ?? "N/A"}",
                            ct: default);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to log version check");
                    }
                });

                var protectedResult = await ProtectReadPayloadAsync(result, "SystemVersionCheck");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking version");
                return StatusCode(500, new { error = "خطا در بررسی نسخه" });
            }
        }

        /// <summary>
        /// دریافت اطلاعات کامل سیستم
        /// FPT_TUD_EXT.1.2: اطلاعات محیط و نسخه
        /// </summary>
        /// <returns>اطلاعات کامل سیستم</returns>
        [HttpGet("info")]
        [RequirePermission("System.Version.Read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSystemInfo()
        {
            try
            {
                var versionInfo = GetVersionInfo();
                var frontendVersionInfo = GetFrontendVersionInfo();
                var assembly = Assembly.GetExecutingAssembly();
                var fileInfo = new FileInfo(assembly.Location);

                var systemInfo = new
                {
                    backend = versionInfo,
                    frontend = frontendVersionInfo,
                    environment = new
                    {
                        name = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                        machineName = Environment.MachineName,
                        osVersion = Environment.OSVersion.ToString(),
                        dotNetVersion = Environment.Version.ToString(),
                        processorCount = Environment.ProcessorCount,
                        is64Bit = Environment.Is64BitOperatingSystem,
                        timeZone = TimeZoneInfo.Local.DisplayName
                    },
                    //application = new
                    //{
                    //    assemblyName = assembly.GetName().Name,
                    //    assemblyVersion = assembly.GetName().Version?.ToString(),
                    //    fileVersion = fileInfo.LastWriteTimeUtc,
                    //    workingDirectory = Environment.CurrentDirectory
                    //},
                    //docker = new
                    //{
                    //    containerId = Environment.GetEnvironmentVariable("HOSTNAME") ?? "Not in Docker",
                    //    isDocker = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HOSTNAME"))
                    //}
                };

                var protectedSystemInfo = await ProtectReadPayloadAsync(systemInfo, "SystemInfo");
                return Ok(protectedSystemInfo);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system info");
                return StatusCode(500, new { error = "خطا در دریافت اطلاعات سیستم" });
            }
        }

        #region Frontend Version Endpoints

        /// <summary>
        /// دریافت نسخه فعلی فرانت‌اند از بک‌اند
        /// FPT_TUD_EXT.1.2: بررسی نسخه فرانت‌اند
        /// </summary>
        /// <returns>اطلاعات نسخه فرانت‌اند</returns>
        [HttpGet("frontend/current")]
        [RequirePermission("System.Version.Read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetFrontendCurrentVersion()
        {
            try
            {
                var frontendVersionInfo = GetFrontendVersionInfo();
                var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
                var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
                var userName = User.Identity?.Name ?? "Unknown";
                var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");

                // ثبت در Audit Log
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _auditLogService.LogEventAsync(
                            eventType: "FrontendVersionInfoRequested",
                            entityType: "System",
                            entityId: "FrontendVersion",
                            isSuccess: true,
                            ipAddress: ipAddress,
                            userName: userName,
                            userId: userId,
                            userAgent: userAgent,
                            description: $"Frontend version information requested: {frontendVersionInfo.Version}",
                            ct: default);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to log frontend version info request");
                    }
                });

                var protectedFrontendVersionInfo = await ProtectReadPayloadAsync(frontendVersionInfo, "FrontendVersion");
                return Ok(protectedFrontendVersionInfo);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting frontend version");
                return StatusCode(500, new { error = "خطا در دریافت نسخه فرانت‌اند" });
            }
        }

        /// <summary>
        /// بررسی نسخه فرانت‌اند
        /// فرانت نسخه خودش را ارسال می‌کند و بک‌اند بررسی می‌کند
        /// FPT_TUD_EXT.1.2: مقایسه نسخه‌های فرانت‌اند
        /// </summary>
        /// <param name="frontendVersion">نسخه فعلی فرانت‌اند</param>
        /// <returns>نتیجه بررسی و اطلاعات نسخه جدید در صورت وجود</returns>
        [HttpGet("frontend/check")]
        [RequirePermission("System.Version.Read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CheckFrontendVersion([FromQuery] string? frontendVersion = null)
        {
            try
            {
                var frontendVersionInfo = GetFrontendVersionInfo();
                var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
                var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
                var userName = User.Identity?.Name ?? "Unknown";
                var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");

                // مقایسه نسخه‌ها
                var isUpToDate = string.IsNullOrEmpty(frontendVersion) ||
                                CompareVersions(frontendVersion, frontendVersionInfo.Version) >= 0;

                var result = new
                {
                    latestVersion = frontendVersionInfo.Version,
                    clientVersion = frontendVersion,
                    isUpToDate = isUpToDate,
                    updateAvailable = !isUpToDate,
                    updateInfo = !isUpToDate ? new
                    {
                        version = frontendVersionInfo.Version,
                        buildDate = frontendVersionInfo.BuildDate,
                        buildNumber = frontendVersionInfo.BuildNumber,
                        description = frontendVersionInfo.Description,
                        updateUrl = frontendVersionInfo.UpdateUrl,
                        changelog = frontendVersionInfo.Changelog
                    } : null,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                };

                // ثبت در Audit Log
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _auditLogService.LogEventAsync(
                            eventType: "FrontendVersionCheck",
                            entityType: "System",
                            entityId: "FrontendVersion",
                            isSuccess: true,
                            ipAddress: ipAddress,
                            userName: userName,
                            userId: userId,
                            userAgent: userAgent,
                            description: $"Frontend version check: Client={frontendVersion ?? "N/A"}, Latest={frontendVersionInfo.Version}, UpdateAvailable={!isUpToDate}",
                            ct: default);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to log frontend version check");
                    }
                });

                var protectedResult = await ProtectReadPayloadAsync(result, "FrontendVersionCheck");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking frontend version");
                return StatusCode(500, new { error = "خطا در بررسی نسخه فرانت‌اند" });
            }
        }

        /// <summary>
        /// مقایسه دو نسخه Semantic Versioning
        /// </summary>
        /// <param name="version1">نسخه اول</param>
        /// <param name="version2">نسخه دوم</param>
        /// <returns>مثبت اگر version1 > version2، منفی اگر version1 < version2، صفر اگر برابر</returns>
        private static int CompareVersions(string version1, string version2)
        {
            try
            {
                var v1Parts = version1.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
                var v2Parts = version2.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

                var maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

                for (int i = 0; i < maxLength; i++)
                {
                    var v1Part = i < v1Parts.Length ? v1Parts[i] : 0;
                    var v2Part = i < v2Parts.Length ? v2Parts[i] : 0;

                    if (v1Part > v2Part) return 1;
                    if (v1Part < v2Part) return -1;
                }

                return 0;
            }
            catch
            {
                // در صورت خطا، مقایسه رشته‌ای
                return string.Compare(version1, version2, StringComparison.Ordinal);
            }
        }

        #endregion

        /// <summary>
        /// دریافت اطلاعات نسخه بک‌اند از منابع مختلف
        /// اولویت: Environment Variable > version.json > Assembly Info
        /// </summary>
        private VersionInfo GetVersionInfo()
        {
            if (_cachedVersionInfo != null)
                return _cachedVersionInfo;

            var versionInfo = new VersionInfo();

            // 1. بررسی Environment Variable (اولویت اول)
            var envVersion = Environment.GetEnvironmentVariable("APP_VERSION");
            if (!string.IsNullOrEmpty(envVersion))
            {
                versionInfo.Version = envVersion;
            }

            // 2. بررسی version.json
            try
            {
                var versionJsonPath = Path.Combine(AppContext.BaseDirectory, "version.json");
                if (System.IO.File.Exists(versionJsonPath))
                {
                    var jsonContent = System.IO.File.ReadAllText(versionJsonPath);
                    var versionData = JsonSerializer.Deserialize<VersionJson>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (versionData != null)
                    {
                        // ساختار جدید (با Backend)
                        if (versionData.Backend != null)
                        {
                            if (string.IsNullOrEmpty(versionInfo.Version))
                                versionInfo.Version = versionData.Backend.Version ?? "1.0.0";

                            if (DateTime.TryParse(versionData.Backend.BuildDate, out var buildDate))
                                versionInfo.BuildDate = buildDate;

                            versionInfo.BuildNumber = versionData.Backend.BuildNumber;
                            versionInfo.Description = versionData.Backend.Description;
                        }
                        // ساختار قدیمی (سازگاری با نسخه قبلی)
                        else
                        {
                            if (string.IsNullOrEmpty(versionInfo.Version))
                                versionInfo.Version = versionData.Version ?? "1.0.0";

                            if (DateTime.TryParse(versionData.BuildDate, out var buildDate))
                                versionInfo.BuildDate = buildDate;

                            versionInfo.BuildNumber = versionData.BuildNumber;
                            versionInfo.Description = versionData.Description;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read version.json");
            }

            // 3. استفاده از Assembly Info (fallback)
            if (string.IsNullOrEmpty(versionInfo.Version))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var assemblyVersion = assembly.GetName().Version;
                versionInfo.Version = assemblyVersion?.ToString() ?? "1.0.0";
            }

            // 4. بررسی Environment Variables برای Build Date و Build Number
            var envBuildDate = Environment.GetEnvironmentVariable("BUILD_DATE");
            if (!string.IsNullOrEmpty(envBuildDate) && DateTime.TryParse(envBuildDate, out var parsedBuildDate))
            {
                versionInfo.BuildDate = parsedBuildDate;
            }
            else if (versionInfo.BuildDate == null)
            {
                // استفاده از تاریخ فایل Assembly
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                if (System.IO.File.Exists(assemblyPath))
                {
                    versionInfo.BuildDate = System.IO.File.GetLastWriteTimeUtc(assemblyPath);
                }
            }

            var envBuildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
            if (!string.IsNullOrEmpty(envBuildNumber))
            {
                versionInfo.BuildNumber = envBuildNumber;
            }

            _cachedVersionInfo = versionInfo;
            return versionInfo;
        }

        /// <summary>
        /// کش اطلاعات نسخه فرانت‌اند
        /// </summary>
        private static FrontendVersionInfo? _cachedFrontendVersionInfo;

        /// <summary>
        /// دریافت اطلاعات نسخه فرانت‌اند از منابع مختلف
        /// اولویت: Environment Variable > version.json > Configuration
        /// </summary>
        private FrontendVersionInfo GetFrontendVersionInfo()
        {
            if (_cachedFrontendVersionInfo != null)
                return _cachedFrontendVersionInfo;

            var frontendInfo = new FrontendVersionInfo();

            // 1. بررسی Environment Variable (اولویت اول)
            var envFrontendVersion = Environment.GetEnvironmentVariable("FRONTEND_VERSION");
            if (!string.IsNullOrEmpty(envFrontendVersion))
            {
                frontendInfo.Version = envFrontendVersion;
            }

            // 2. بررسی version.json
            try
            {
                var versionJsonPath = Path.Combine(AppContext.BaseDirectory, "version.json");
                if (System.IO.File.Exists(versionJsonPath))
                {
                    var jsonContent = System.IO.File.ReadAllText(versionJsonPath);
                    var versionData = JsonSerializer.Deserialize<VersionJson>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (versionData?.Frontend != null)
                    {
                        if (string.IsNullOrEmpty(frontendInfo.Version))
                            frontendInfo.Version = versionData.Frontend.Version ?? "1.0.0";

                        if (DateTime.TryParse(versionData.Frontend.BuildDate, out var buildDate))
                            frontendInfo.BuildDate = buildDate;

                        frontendInfo.BuildNumber = versionData.Frontend.BuildNumber;
                        frontendInfo.Description = versionData.Frontend.Description;
                        frontendInfo.UpdateUrl = versionData.Frontend.UpdateUrl;
                        frontendInfo.Changelog = versionData.Frontend.Changelog;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read frontend version from version.json");
            }

            // 3. بررسی Configuration (fallback)
            if (string.IsNullOrEmpty(frontendInfo.Version))
            {
                frontendInfo.Version = _configuration["Frontend:Version"] ?? "1.0.0";
                frontendInfo.UpdateUrl = _configuration["Frontend:UpdateUrl"];
            }

            // 4. بررسی Environment Variables
            var envFrontendBuildDate = Environment.GetEnvironmentVariable("FRONTEND_BUILD_DATE");
            if (!string.IsNullOrEmpty(envFrontendBuildDate) && DateTime.TryParse(envFrontendBuildDate, out var parsedBuildDate))
            {
                frontendInfo.BuildDate = parsedBuildDate;
            }

            var envFrontendBuildNumber = Environment.GetEnvironmentVariable("FRONTEND_BUILD_NUMBER");
            if (!string.IsNullOrEmpty(envFrontendBuildNumber))
            {
                frontendInfo.BuildNumber = envFrontendBuildNumber;
            }

            var envFrontendUpdateUrl = Environment.GetEnvironmentVariable("FRONTEND_UPDATE_URL");
            if (!string.IsNullOrEmpty(envFrontendUpdateUrl))
            {
                frontendInfo.UpdateUrl = envFrontendUpdateUrl;
            }

            _cachedFrontendVersionInfo = frontendInfo;
            return frontendInfo;
        }

        #region FPT_TUD_EXT.1.3 - Signature Verification Endpoints

        /// <summary>
        /// اعتبارسنجی امضای دیجیتال فایل به‌روزرسانی
        /// FPT_TUD_EXT.1.3: اعتبارسنجی امضای دیجیتال
        /// </summary>
        /// <param name="request">درخواست اعتبارسنجی شامل Hash فایل و امضا</param>
        /// <returns>نتیجه اعتبارسنجی</returns>
        [HttpPost("signature/verify")]
        [RequirePermission("System.Version.VerifySignature")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> VerifyUpdateSignature([FromBody] SignatureVerificationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "درخواست نامعتبر است" });
                }

                var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
                var userName = User.Identity?.Name ?? "Unknown";
                var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");

                SignatureVerificationResult result;

                // اگر محتوای فایل ارسال شده، از آن استفاده کن
                if (!string.IsNullOrEmpty(request.FileContentBase64))
                {
                    try
                    {
                        var fileContent = Convert.FromBase64String(request.FileContentBase64);
                        result = await _signatureService.VerifyUpdateSignatureAsync(fileContent, request.Signature);
                    }
                    catch (FormatException)
                    {
                        return BadRequest(new { error = "فرمت محتوای فایل نامعتبر است" });
                    }
                }
                // در غیر این صورت از Hash استفاده کن
                else if (!string.IsNullOrEmpty(request.FileHash))
                {
                    result = await _signatureService.VerifySignatureByHashAsync(request.FileHash, request.Signature);
                }
                else
                {
                    return BadRequest(new { error = "FileContentBase64 یا FileHash باید ارائه شود" });
                }

                // ثبت در Audit Log
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _auditLogService.LogEventAsync(
                            eventType: "UpdateSignatureVerification",
                            entityType: "System",
                            entityId: "Signature",
                            isSuccess: result.IsValid,
                            ipAddress: ipAddress,
                            userName: userName,
                            userId: userId,
                            description: $"Signature verification: {(result.IsValid ? "Valid" : "Invalid")} - {result.Message}",
                            ct: default);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to log signature verification");
                    }
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying update signature");
                return StatusCode(500, new { error = "خطا در اعتبارسنجی امضای دیجیتال" });
            }
        }

        /// <summary>
        /// اعتبارسنجی امضای متادیتای نسخه
        /// FPT_TUD_EXT.1.3: اعتبارسنجی امضای اطلاعات نسخه
        /// </summary>
        /// <param name="request">درخواست اعتبارسنجی متادیتا</param>
        /// <returns>نتیجه اعتبارسنجی</returns>
        [HttpPost("signature/verify-metadata")]
        [RequirePermission("System.Version.VerifySignature")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> VerifyVersionMetadataSignature([FromBody] MetadataSignatureRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Signature))
                {
                    return BadRequest(new { error = "درخواست نامعتبر است" });
                }

                var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
                var userName = User.Identity?.Name ?? "Unknown";
                var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");

                // ساخت متادیتای نسخه به فرمت استاندارد
                var metadata = $"{request.Version}|{request.BuildDate}|{request.BuildNumber}";
                
                var result = await _signatureService.VerifyVersionMetadataSignatureAsync(metadata, request.Signature);

                // ثبت در Audit Log
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _auditLogService.LogEventAsync(
                            eventType: "VersionMetadataSignatureVerification",
                            entityType: "System",
                            entityId: "VersionMetadata",
                            isSuccess: result.IsValid,
                            ipAddress: ipAddress,
                            userName: userName,
                            userId: userId,
                            description: $"Version metadata signature verification for v{request.Version}: {(result.IsValid ? "Valid" : "Invalid")}",
                            ct: default);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to log metadata signature verification");
                    }
                });

                return Ok(new
                {
                    result.IsValid,
                    result.Message,
                    result.ErrorCode,
                    result.VerifiedAt,
                    result.KeyFingerprint,
                    verifiedVersion = request.Version
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying version metadata signature");
                return StatusCode(500, new { error = "خطا در اعتبارسنجی امضای متادیتا" });
            }
        }

        /// <summary>
        /// محاسبه Hash فایل
        /// FPT_TUD_EXT.1.3: پشتیبانی از محاسبه Hash برای اعتبارسنجی
        /// </summary>
        /// <param name="request">محتوای فایل به صورت Base64</param>
        /// <returns>Hash SHA-256 فایل</returns>
        [HttpPost("signature/compute-hash")]
        [RequirePermission("System.Version.VerifySignature")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult ComputeFileHash([FromBody] ComputeHashRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.FileContentBase64))
                {
                    return BadRequest(new { error = "محتوای فایل ارائه نشده است" });
                }

                byte[] fileContent;
                try
                {
                    fileContent = Convert.FromBase64String(request.FileContentBase64);
                }
                catch (FormatException)
                {
                    return BadRequest(new { error = "فرمت محتوای فایل نامعتبر است" });
                }

                var hash = _signatureService.ComputeFileHash(fileContent);

                return Ok(new
                {
                    hash = hash,
                    algorithm = "SHA-256",
                    fileSize = fileContent.Length,
                    computedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing file hash");
                return StatusCode(500, new { error = "خطا در محاسبه Hash فایل" });
            }
        }

        /// <summary>
        /// دریافت اطلاعات کلید عمومی اعتبارسنجی
        /// FPT_TUD_EXT.1.3: نمایش اطلاعات کلید امضا
        /// </summary>
        /// <returns>اطلاعات کلید عمومی</returns>
        [HttpGet("signature/public-key-info")]
        [RequirePermission("System.Version.Read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPublicKeyInfo()
        {
            try
            {
                var keyInfo = _signatureService.GetPublicKeyInfo();

                var response = new
                {
                    keyInfo.IsLoaded,
                    keyInfo.Fingerprint,
                    keyInfo.KeySize,
                    keyInfo.Algorithm,
                    keyInfo.LoadedAt,
                    status = keyInfo.IsLoaded ? "ready" : "not_configured"
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "SignaturePublicKeyInfo");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public key info");
                return StatusCode(500, new { error = "خطا در دریافت اطلاعات کلید عمومی" });
            }
        }

        /// <summary>
        /// بررسی وضعیت آمادگی سیستم اعتبارسنجی امضا
        /// FPT_TUD_EXT.1.3: بررسی وضعیت سیستم
        /// </summary>
        /// <returns>وضعیت آمادگی</returns>
        [HttpGet("signature/status")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSignatureVerificationStatus(CancellationToken ct = default)
        {
            try
            {
                var isReady = _signatureService.IsPublicKeyLoaded();
                var keyInfo = _signatureService.GetPublicKeyInfo();

                var response = new
                {
                    isReady = isReady,
                    algorithm = "RSA-SHA256",
                    keyFingerprint = isReady ? keyInfo.Fingerprint.Substring(0, Math.Min(16, keyInfo.Fingerprint.Length)) + "..." : null,
                    message = isReady 
                        ? "سیستم اعتبارسنجی امضای دیجیتال آماده است" 
                        : "کلید عمومی پیکربندی نشده است"
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "SignatureVerificationStatus", ct: ct);
                return Ok(protectedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signature verification status");
                return StatusCode(500, new { error = "خطا در بررسی وضعیت سیستم" });
            }
        }

        private async Task<T> ProtectReadPayloadAsync<T>(
            T data,
            string entityType,
            string? entityId = null,
            CancellationToken ct = default) where T : class
        {
            var context = new ExportContext
            {
                EntityType = entityType,
                EntityId = entityId,
                UserId = GetUserId(),
                UserName = User.Identity?.Name ?? "Unknown",
                IpAddress = HttpContextHelper.GetIpAddress(HttpContext),
                UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                RequestPath = HttpContext.Request.Path,
                RequestedFormat = "JSON"
            };

            var secured = await _dataExportService.WrapWithSecurityAttributesAsync(data, context, ct);
            return secured.Data;
        }

        private long GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("UserId")?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }



        #endregion

        #region DTOs

        /// <summary>
        /// درخواست اعتبارسنجی امضای دیجیتال
        /// </summary>
        public class SignatureVerificationRequest
        {
            /// <summary>
            /// محتوای فایل به صورت Base64 (اختیاری - یا FileContentBase64 یا FileHash)
            /// </summary>
            public string? FileContentBase64 { get; set; }

            /// <summary>
            /// Hash SHA-256 فایل به صورت Base64 (اختیاری)
            /// </summary>
            public string? FileHash { get; set; }

            /// <summary>
            /// امضای دیجیتال به صورت Base64 (الزامی)
            /// </summary>
            public string Signature { get; set; } = string.Empty;
        }

        /// <summary>
        /// درخواست اعتبارسنجی امضای متادیتای نسخه
        /// </summary>
        public class MetadataSignatureRequest
        {
            /// <summary>
            /// شماره نسخه
            /// </summary>
            public string Version { get; set; } = string.Empty;

            /// <summary>
            /// تاریخ Build
            /// </summary>
            public string? BuildDate { get; set; }

            /// <summary>
            /// شماره Build
            /// </summary>
            public string? BuildNumber { get; set; }

            /// <summary>
            /// امضای دیجیتال
            /// </summary>
            public string Signature { get; set; } = string.Empty;
        }

        /// <summary>
        /// درخواست محاسبه Hash
        /// </summary>
        public class ComputeHashRequest
        {
            /// <summary>
            /// محتوای فایل به صورت Base64
            /// </summary>
            public string FileContentBase64 { get; set; } = string.Empty;
        }

        /// <summary>
        /// اطلاعات نسخه نرم‌افزار (بک‌اند)
        /// </summary>
        public class VersionInfo
        {
            public string Version { get; set; } = "1.0.0";
            public DateTime? BuildDate { get; set; }
            public string? BuildNumber { get; set; }
            public string? Description { get; set; }
        }

        /// <summary>
        /// اطلاعات نسخه فرانت‌اند
        /// </summary>
        public class FrontendVersionInfo
        {
            public string Version { get; set; } = "1.0.0";
            public DateTime? BuildDate { get; set; }
            public string? BuildNumber { get; set; }
            public string? Description { get; set; }
            public string? UpdateUrl { get; set; }
            public string? Changelog { get; set; }
        }

        /// <summary>
        /// ساختار version.json (جدید - با پشتیبانی از ساختار قدیمی)
        /// </summary>
        private class VersionJson
        {
            // ساختار جدید
            public BackendVersionJson? Backend { get; set; }
            public FrontendVersionJson? Frontend { get; set; }

            // ساختار قدیمی (برای سازگاری)
            public string? Version { get; set; }
            public string? BuildDate { get; set; }
            public string? BuildNumber { get; set; }
            public string? Description { get; set; }
        }

        private class BackendVersionJson
        {
            public string? Version { get; set; }
            public string? BuildDate { get; set; }
            public string? BuildNumber { get; set; }
            public string? Description { get; set; }
        }

        private class FrontendVersionJson
        {
            public string? Version { get; set; }
            public string? BuildDate { get; set; }
            public string? BuildNumber { get; set; }
            public string? Description { get; set; }
            public string? UpdateUrl { get; set; }
            public string? Changelog { get; set; }
        }

        #endregion
    }
}


