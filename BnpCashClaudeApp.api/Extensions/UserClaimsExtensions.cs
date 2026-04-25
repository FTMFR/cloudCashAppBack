using System.Security.Claims;

namespace BnpCashClaudeApp.api.Extensions
{
    /// <summary>
    /// متدهای الحاقی برای کار با Claims کاربر
    /// </summary>
    public static class UserClaimsExtensions
    {
        /// <summary>
        /// دریافت شناسه کاربر از Claims
        /// </summary>
        public static long? GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// دریافت نام کاربری از Claims
        /// </summary>
        public static string? GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value;
        }

        /// <summary>
        /// دریافت شناسه گروه کاربری برای Insert از Claims
        /// </summary>
        public static long? GetTblUserGrpIdInsert(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("TblUserGrpIdInsert");
            if (claim != null && long.TryParse(claim.Value, out long grpId))
            {
                return grpId;
            }
            return null;
        }

        /// <summary>
        /// دریافت شناسه گروه کاربری برای LastEdit از Claims
        /// </summary>
        public static long? GetTblUserGrpIdLastEdit(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("TblUserGrpIdLastEdit");
            if (claim != null && long.TryParse(claim.Value, out long grpId))
            {
                return grpId;
            }
            return null;
        }
    }
}
