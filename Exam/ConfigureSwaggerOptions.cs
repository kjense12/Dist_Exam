using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Exam;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) =>
        _provider = provider;
    
    public void Configure(SwaggerGenOptions options)
    {
        // add all possible api versions found
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo()
                {
                    Title = $"API {description.ApiVersion}",
                    Version = description.ApiVersion.ToString()
                    // Description, TermsOfServce, Contact, License, ...
                });
        }
        
        // include xml comments (enable creation in csproj file)
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        //options.IncludeXmlComments(xmlPath);
        
        // use FullName for schemaId - avoids conflicts between classes using the same name (which are in different namespaces)
        options.CustomSchemaIds(i => i.FullName);
    }
}