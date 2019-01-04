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
      
        [HttpGet("claims")]      
        [SwaggerOperation(OperationId = "GetIdentityClaims3")]
        public ActionResult<ClaimInfo[]> GetClaims()
        {
            return User.Claims.Select(c => new ClaimInfo { Type = c.Type, Value = c.Value }).ToArray();
        }       
    }

    [Route("api/test4")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "test")]
    [AuthorizeAdmin]
    public class TestController4AuthAdmin : ControllerBase
    {
      
        [HttpGet("claims")]      
        [SwaggerOperation(OperationId = "GetIdentityClaims4")]
        public ActionResult<ClaimInfo[]> GetClaims()
        {
            return User.Claims.Select(c => new ClaimInfo { Type = c.Type, Value = c.Value }).ToArray();
        }       
    }
}