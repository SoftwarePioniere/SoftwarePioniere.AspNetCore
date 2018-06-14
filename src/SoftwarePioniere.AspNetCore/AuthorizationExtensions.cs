using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
// ReSharper disable UnusedMember.Global

namespace SoftwarePioniere.AspNetCore
{
    /// <summary>
    /// Diverse Extensions
    /// </summary>
    public static class AuthorizationExtensions
    {

        /// <summary>
        /// Prüft ob der Benutzer mit dem Token die Admin Regel erfüllt
        /// </summary>
        /// <param name="authorizationService"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<bool> IsUserAdmin(this IAuthorizationService authorizationService, ClaimsPrincipal user)
        {
            var isAdmin = await authorizationService.AuthorizeAsync(user, "admin");
            return isAdmin.Succeeded;
        }
    }
}