using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace SoftwarePioniere.AspNetCore.SampleApp
{
    [Route("api/test2")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "test")]
    [Authorize]
    public class TestController2Auth : ControllerBase
    {
        
        [HttpGet("claims")]      
        [SwaggerOperation(OperationId = "GetIdentityClaims2")]
        public ActionResult<ClaimInfo[]> GetClaims()
        {
            return User.Claims.Select(c => new ClaimInfo { Type = c.Type, Value = c.Value }).ToArray();
        }    
        
    
        [HttpGet("info2")]
        [AllowAnonymous]
        [SwaggerOperation(OperationId = "GetApiInfo2")]
        public ActionResult<ApiInfo> GetApiInfo2()
        {
            var assembly = Assembly.GetEntryAssembly();

            return new ApiInfo
            {
                Title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? assembly.GetName().Name,
                Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,               
            };
        }
    }
}