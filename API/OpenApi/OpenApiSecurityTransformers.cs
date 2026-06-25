using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SimpleWebData.OpenApi
{
    /// <summary>
    /// Ubacuje "Bearer" (JWT) security scheme u OpenAPI dokument i postavlja osnovne meta podatke.
    /// Zahvaljujući ovome Scalar (i svaki drugi OpenAPI UI) prikaže "Authorize" gumb u koji se
    /// zalijepi access token.
    /// </summary>
    internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider schemeProvider)
        : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken)
        {
            var schemes = await schemeProvider.GetAllSchemesAsync();
            if (schemes.All(s => s.Name != "Bearer"))
            {
                return;
            }

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "JWT access token dobiven preko POST /api/auth/login. " +
                              "Upiši samo vrijednost tokena – UI sam dodaje 'Bearer ' prefiks."
            };

            document.Info ??= new OpenApiInfo();
            document.Info.Title = "SimpleWebData API";
            document.Info.Version = "v1";
            document.Info.Description =
                "REST API za SimpleWebData CMS. Autentikacija ide preko JWT Bearer tokena " +
                "(POST /api/auth/login → access token → \"Authorize\").";
        }
    }

    /// <summary>
    /// Dodaje "Bearer" security requirement samo na one operacije koje stvarno traže autorizaciju
    /// (imaju [Authorize]/RequireAuthorization, a nisu AllowAnonymous). Tako login i javni read
    /// endpointi ostaju bez lokota, a zaštićeni dobiju ispravan 401 prikaz u UI-ju.
    /// </summary>
    internal sealed class AuthorizationOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            var requiresAuth = metadata.OfType<IAuthorizeData>().Any()
                               && !metadata.OfType<IAllowAnonymous>().Any();

            if (!requiresAuth)
            {
                return Task.CompletedTask;
            }

            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document, null)] = new List<string>()
            });

            return Task.CompletedTask;
        }
    }
}
