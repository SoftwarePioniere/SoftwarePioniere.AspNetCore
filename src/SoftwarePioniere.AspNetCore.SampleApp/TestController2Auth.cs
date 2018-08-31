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
        

        /// <summary>
        /// Infos zur API auslesen
        /// </summary>
        /// <returns></returns>
        [HttpGet("info2")]
        [AllowAnonymous]
        [SwaggerOperation("GetApiInfo2")]
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