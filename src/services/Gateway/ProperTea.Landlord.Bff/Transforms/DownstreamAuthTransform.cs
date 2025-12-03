using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.AccessTokenManagement.OpenIdConnect;
using Yarp.ReverseProxy.Transforms;

namespace ProperTea.Landlord.Bff.Transforms;

public static class DownstreamAuthTransform
{
    public static async ValueTask TransformAsync(RequestTransformContext transformContext)
    {
        var token = await transformContext.HttpContext.GetUserAccessTokenAsync().ConfigureAwait(false);
        if (token.Token?.AccessToken is null)
            return;

        transformContext.ProxyRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token.AccessToken);

        var user = transformContext.HttpContext.User;

        if (transformContext.HttpContext.Request.RouteValues["orgId"] is string selectedOrgId)
        {
            var organizationsClaim = user.Claims.FirstOrDefault(c => c.Type == "organization");
            var isMember = false;

            if (organizationsClaim?.Value is not null)
            {
                var organizations =
                    JsonSerializer.Deserialize<Dictionary<string, Organization>>(organizationsClaim.Value);
                isMember = organizations?.Values.Any(org => org.Id == selectedOrgId) ?? false;
            }

            if (!isMember)
            {
                transformContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }
            transformContext.ProxyRequest.Headers.Add("X-Organization-Id", selectedOrgId);
        }
    }
}

public class Organization
{
    [JsonPropertyName("id")] public string Id { get; set; } = null!;
}
