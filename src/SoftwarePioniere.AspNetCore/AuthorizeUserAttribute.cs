using Microsoft.AspNetCore.Authorization;

namespace SoftwarePioniere.AspNetCore
{
    public class AuthorizeUserAttribute : AuthorizeAttribute
    {
        public AuthorizeUserAttribute() : base(Constants.IsUserPolicy)
        {

        }
    }
}