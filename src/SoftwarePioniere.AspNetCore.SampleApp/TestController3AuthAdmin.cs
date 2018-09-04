using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace SoftwarePioniere.AspNetCore.SampleApp
{
    [Route("api/test3")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "test")]
    [Authorize(Policy = "admin")]
    public class TestController3AuthAdmin : ControllerBase
    {

        /// <summary>
        /// Alle Claims des Benutzers auslesen
        /// </summary>
        /// <returns></returns>
        [HttpGet("claims")]      
        [SwaggerOperation(OperationId = "GetIdentityClaims3")]
        public ActionResult<ClaimInfo[]> GetClaims()
        {
            return User.Claims.Select(c => new ClaimInfo { Type = c.Type, Value = c.Value }).ToArray();
        }       
    }
}