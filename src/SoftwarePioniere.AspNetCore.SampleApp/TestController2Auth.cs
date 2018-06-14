using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SoftwarePioniere.AspNetCore.SampleApp
{
    [Route("api/test2")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "test")]
    [Authorize]
    public class TestController2Auth : ControllerBase
    {

        /// <summary>
        /// Alle Claims des Benutzers auslesen
        /// </summary>
        /// <returns></returns>
        [HttpGet("claims")]      
        [SwaggerOperation("GetIdentityClaims2")]
        public ActionResult<ClaimInfo[]> GetClaims()
        {
            return User.Claims.Select(c => new ClaimInfo { Type = c.Type, Value = c.Value }).ToArray();
        }       
    }
}