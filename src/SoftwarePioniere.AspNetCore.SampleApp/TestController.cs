using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SoftwarePioniere.AspNetCore.SampleApp
{
 
    [Route("api/test")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "test")]
    public class TestController : ControllerBase
    {

        /// <summary>
        /// Alle Claims des Benutzers auslesen
        /// </summary>
        /// <returns></returns>
        [HttpGet("claims")]
        [Authorize]
        [SwaggerOperation("GetIdentityClaims")]
        public ActionResult<ClaimInfo[]> GetClaims()
        {
            return User.Claims.Select(c => new ClaimInfo { Type = c.Type, Value = c.Value }).ToArray();
        }

        [HttpGet("claims/admin")]
        [Authorize(Policy = "admin")]
        [SwaggerOperation("GetIdentityClaimsAdmin")]
        public ActionResult<ClaimInfo[]> GetClaimsAdmin()
        {
            return User.Claims.Select(c => new ClaimInfo { Type = c.Type, Value = c.Value }).ToArray();
        }
    }
}
