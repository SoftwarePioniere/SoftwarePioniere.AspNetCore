using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace SoftwarePioniere.AspNetCore.SampleApp
{
    [Route("api/home")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "api")]
    public class HomeController : ControllerBase
    {
        /// <summary>
        /// Infos zur API auslesen
        /// </summary>
        /// <returns></returns>
        [HttpGet("info")]
        [SwaggerOperation(OperationId = "GetApiInfo")]
        public ActionResult<ApiInfo> GetApiInfo()
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