using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global

namespace SoftwarePioniere.AspNetCore
{

    public static class SwaggerExtensions
    {

        public static IApplicationBuilder UseMySwagger(this IApplicationBuilder app, Action<MySwaggerOptions> setupAction)
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

                if (options.OAuthAdditionalQueryStringParams != null)
                {
                    c.OAuthAdditionalQueryStringParams(options.OAuthAdditionalQueryStringParams);
                }
                c.OAuthClientId(options.OAuthClientId);
                c.OAuthClientSecret(options.OAuthClientSecret);
            });

            return app;
        }

        public static IServiceCollection AddMySwagger(this IServiceCollection services, Action<MySwaggerOptions> setupAction)
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

                c.EnableAnnotations();
                c.DescribeAllEnumsAsStrings();
                //    c.OperationFilter<FormFileOperationFilter>();
                c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
                c.OperationFilter<SummaryFromOperationFilter>();

                c.AddSecurityDefinition("oauth2", options.OAuth2Scheme);

                c.DocInclusionPredicate((s, description) =>
                {

                    if (options.Docs.Contains(s))
                        return description.GroupName == s;

                    if (string.IsNullOrEmpty(description.GroupName))
                        return true;

                    return description.GroupName != s;
                });
            });

            return services;
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

    ///// <inheritdoc />
    //public class FormFileOperationFilter : IOperationFilter
    //{
    //    private const string FormDataMimeType = "multipart/form-data";
    //    private static readonly string[] FormFilePropertyNames =
    //        typeof(IFormFile).GetTypeInfo().DeclaredProperties.Select(x => x.Name).ToArray();

    //    /// <inheritdoc />
    //    public void Apply(Operation operation, OperationFilterContext context)
    //    {
    //        if (operation.OperationId == "bildupload")
    //        {
    //            Console.WriteLine("a");
    //        }
    //        if (context.ApiDescription.ParameterDescriptions.Any(x => x.ModelMetadata != null && x.ModelMetadata.ContainerType == typeof(IFormFile)))
    //        {
    //            var formFileParameters = operation
    //                .Parameters
    //                .OfType<NonBodyParameter>()
    //                .Where(x => FormFilePropertyNames.Contains(x.Name))
    //                .ToArray();
    //            var index = operation.Parameters.IndexOf(formFileParameters.First());
    //            foreach (var formFileParameter in formFileParameters)
    //            {
    //                operation.Parameters.Remove(formFileParameter);
    //            }

    //            var formFileParameterName = context
    //                .ApiDescription
    //                .ActionDescriptor
    //                .Parameters
    //                .Where(x => x.ParameterType == typeof(IFormFile))
    //                .Select(x => x.Name)
    //                .First();
    //            var parameter = new NonBodyParameter()
    //            {
    //                Name = formFileParameterName,
    //                In = "formData",
    //                Description = "The file to upload.",
    //                Required = true,
    //                Type = "file"
    //            };
    //            operation.Parameters.Insert(index, parameter);

    //            if (!operation.Consumes.Contains(FormDataMimeType))
    //            {
    //                operation.Consumes.Add(FormDataMimeType);
    //            }
    //        }
    //    }
    //}

    // ReSharper disable once ClassNeverInstantiated.Global
    /// <inheritdoc />
    public class SummaryFromOperationFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(Operation operation, OperationFilterContext context)
        {
#if DEBUG
            Console.WriteLine($"OperationId: {operation.OperationId} , Summary: {operation.Summary}, Description: {operation.Description}");
#endif

            if (string.IsNullOrEmpty(operation.Summary) && !string.IsNullOrEmpty(operation.OperationId))
            {
                operation.Summary = operation.OperationId;
            }

            //            var swagOp = context.MethodInfo.GetCustomAttributes(true)
            //                .OfType<SwaggerOperationAttribute>()
            //                .FirstOrDefault();

            //#if DEBUG
            //            Console.WriteLine($"OperationId: {operation.OperationId} , Summary: {operation.Summary}, Description: {operation.Description}");

            //            if (swagOp != null)
            //            {
            //                Console.WriteLine($"OperationId: {operation.OperationId} , SwaggerOperationAttributeSummary: {swagOp.Summary}");
            //            }
            //#endif

            //            if (string.IsNullOrEmpty(operation.Summary) && swagOp != null && !string.IsNullOrEmpty(swagOp.Summary))
            //            {

            //                operation.Summary = swagOp.Summary;
            //            }
        }
    }
}
