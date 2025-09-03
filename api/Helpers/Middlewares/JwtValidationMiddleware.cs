using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.IO;

namespace api.Helpers.Middlewares
{
    public class JwtValidationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<JwtValidationMiddleware> _logger;
        private readonly string _tenantId;
        private readonly string _audience;

        public JwtValidationMiddleware(ILogger<JwtValidationMiddleware> logger)
        {
            _logger = logger;
            _tenantId = Environment.GetEnvironmentVariable("TenantId") ?? "";
            _audience = Environment.GetEnvironmentVariable("AppRegistrationCliendId") ?? "";
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var httpRequest = await context.GetHttpRequestDataAsync();

            var path = httpRequest?.Url.AbsolutePath;
            if (path != null && PublicPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
            {
                await next(context);
                return;
            }

            var authorizationHeader = httpRequest?.Headers.FirstOrDefault(h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).Value;

            var token = authorizationHeader?.FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("Token is missing");
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[] { $"https://{_tenantId}.ciamlogin.com/{_tenantId}/v2.0" },
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = await GetSigningKeysAsync()
            };

            var handler = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };
            var principal = handler.ValidateToken(token, validationParameters, out _);
            context.Items["User"] = principal;

            await next(context);
        }


        private async Task<IEnumerable<SecurityKey>> GetSigningKeysAsync()
        {
            var httpClient = new HttpClient();
            var discoveryEndpoint = $"https://{_tenantId}.ciamlogin.com/{_tenantId}/v2.0/.well-known/openid-configuration";
            var discoveryResponse = await httpClient.GetStringAsync(discoveryEndpoint);

            var discoveryJson = System.Text.Json.JsonDocument.Parse(discoveryResponse);
            var jwksUri = discoveryJson.RootElement.GetProperty("jwks_uri").GetString();

            var jwksResponse = await httpClient.GetStringAsync(jwksUri);
            var jwksJson = System.Text.Json.JsonDocument.Parse(jwksResponse);

            var keys = new List<SecurityKey>();

            foreach (var key in jwksJson.RootElement.GetProperty("keys").EnumerateArray())
            {
                var e = key.GetProperty("e").GetString();
                var n = key.GetProperty("n").GetString();

                var rsa = new System.Security.Cryptography.RSAParameters
                {
                    Exponent = Base64UrlEncoder.DecodeBytes(e),
                    Modulus = Base64UrlEncoder.DecodeBytes(n)
                };

                keys.Add(new RsaSecurityKey(rsa));
            }

            return keys;
        }

        private static readonly string[] PublicPaths = new[]
        {
            "/api/swagger/ui",
            "/api/swagger.json",
            "/api/openapi/v1.json",
            "/api/openapi/v1.yaml"
        };


    }
}
