using Microsoft.AspNetCore.Authorization;

namespace SoftwarePioniere.AspNetCore
{
    public class AuthorizeAdminAttribute : AuthorizeAttribute
    {
        public AuthorizeAdminAttribute() : base(Constants.IsAdminPolicy)
        {

        }
    }
}