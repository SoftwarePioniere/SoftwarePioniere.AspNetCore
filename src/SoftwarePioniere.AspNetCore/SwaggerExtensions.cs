using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SoftwarePioniere.AspNetCore
{

    public static class SwaggerExtensions
    {

        public static void UseMySwagger(this IApplicationBuilder app, Action<MySwaggerOptions> setupAction)
        {
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);
            });

            var options = new MySwaggerOptions();
            setupAction(options);

            app.UseSwaggerUI(c =>
            {
                foreach (var doc in options.Docs)
                {
                    c.SwaggerEndpoint($"/swagger/{doc}/swagger.json", doc);
                }
           
                c.OAuthAdditionalQueryStringParams(options.OAuthAdditionalQueryStringParams);
                c.OAuthClientId(options.OAuthClientId);
                c.OAuthClientSecret(options.OAuthClientSecret);
            });
        }

        public static void AddMySwagger(this IServiceCollection services, Action<MySwaggerOptions> setupAction)
        {
            var options = new MySwaggerOptions();
            setupAction(options);

            services.AddSwaggerGen(c =>
            {
                foreach (var doc in options.Docs)
                {
                    c.SwaggerDoc(doc, new Info
                    {
                        Title = $"{options.ApiTitle}-{doc}",
                        Version = "v1"
                    });
                }

                foreach (var xmlFile in options.XmlFiles)
                {
                    IncludeXmlCommentsIfExist(c, xmlFile);
                }

                c.DescribeAllEnumsAsStrings();
                c.OperationFilter<FormFileOperationFilter>();
                c.OperationFilter<SummaryFromOperationFilter>();

                c.AddSecurityDefinition("oauth2", options.OAuth2Scheme);

                c.OperationFilter<SecurityRequirementsOperationFilter>();

                c.DocInclusionPredicate((s, description) =>
                {

                    if (options.Docs.Contains(s))
                        return description.GroupName == s;
                    
                    if (string.IsNullOrEmpty(description.GroupName))
                        return true;

                    return description.GroupName != s;
                });
            });
        }


        public static void IncludeXmlCommentsIfExist(this SwaggerGenOptions swaggerGenOptions, string fileName)
        {
            //var xmlFileName = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, fileName);
            var xmlFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            //var xmlFileName = fileName;
            var exist = File.Exists(xmlFileName);

            if (exist)
            {
                swaggerGenOptions.IncludeXmlComments(xmlFileName);
            }
        }

    }

    public class MySwaggerOptions
    {
        public string ApiTitle { get; set; }
        public string[] Docs { get; set; }
        public string[] XmlFiles { get; set; }
        public OAuth2Scheme OAuth2Scheme { get; set; }
        public Dictionary<string, string> OAuthAdditionalQueryStringParams { get; set; }
        public string OAuthClientId { get; set; }
        public string OAuthClientSecret { get; set; }
    }

    /// <inheritdoc />
    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(Operation operation, OperationFilterContext context)
        {
            // Policy names map to scopes
            var controllerScopes = context.ApiDescription.ControllerAttributes()
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy);

            var actionScopes = context.ApiDescription.ActionAttributes()
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy);

            var requiredScopes = controllerScopes.Union(actionScopes).Distinct().Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            if (requiredScopes.Any())
            {
                operation.Responses.Add("401", new Response { Description = "Unauthorized" });
                operation.Responses.Add("403", new Response { Description = "Forbidden" });

                operation.Security = new List<IDictionary<string, IEnumerable<string>>>
                {
                    new Dictionary<string, IEnumerable<string>>
                    {
                        {"oauth2", requiredScopes.Where(x => !string.IsNullOrEmpty(x))}
                    }
                };
            }
            else
            {
                var controllerAuts = context.ApiDescription.ControllerAttributes()
                    .OfType<AuthorizeAttribute>().ToArray();

                var actionAuts = context.ApiDescription.ActionAttributes()
                    .OfType<AuthorizeAttribute>().ToArray();

                if (controllerAuts.Length > 0 || actionAuts.Length > 0)
                {
                    operation.Responses.Add("401", new Response { Description = "Unauthorized" });
                    operation.Responses.Add("403", new Response { Description = "Forbidden" });

                    operation.Security = new List<IDictionary<string, IEnumerable<string>>>
                    {
                        new Dictionary<string, IEnumerable<string>>
                        {
                            {"oauth2", new List<string>()}
                        }
                    };
                }
            }
        }
    }

    /// <inheritdoc />
    public class FormFileOperationFilter : IOperationFilter
    {
        private const string FormDataMimeType = "multipart/form-data";
        private static readonly string[] FormFilePropertyNames =
            typeof(IFormFile).GetTypeInfo().DeclaredProperties.Select(x => x.Name).ToArray();

        /// <inheritdoc />
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation.OperationId == "bildupload")
            {
                Console.WriteLine("a");
            }
            if (context.ApiDescription.ParameterDescriptions.Any(x => x.ModelMetadata != null && x.ModelMetadata.ContainerType == typeof(IFormFile)))
            {
                var formFileParameters = operation
                    .Parameters
                    .OfType<NonBodyParameter>()
                    .Where(x => FormFilePropertyNames.Contains(x.Name))
                    .ToArray();
                var index = operation.Parameters.IndexOf(formFileParameters.First());
                foreach (var formFileParameter in formFileParameters)
                {
                    operation.Parameters.Remove(formFileParameter);
                }

                var formFileParameterName = context
                    .ApiDescription
                    .ActionDescriptor
                    .Parameters
                    .Where(x => x.ParameterType == typeof(IFormFile))
                    .Select(x => x.Name)
                    .First();
                var parameter = new NonBodyParameter()
                {
                    Name = formFileParameterName,
                    In = "formData",
                    Description = "The file to upload.",
                    Required = true,
                    Type = "file"
                };
                operation.Parameters.Insert(index, parameter);

                if (!operation.Consumes.Contains(FormDataMimeType))
                {
                    operation.Consumes.Add(FormDataMimeType);
                }
            }
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    /// <inheritdoc />
    public class SummaryFromOperationFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (string.IsNullOrEmpty(operation.Summary) && !string.IsNullOrEmpty(operation.OperationId))
            {
                operation.Summary = operation.OperationId;
            }
        }
    }
}
