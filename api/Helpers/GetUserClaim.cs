using Microsoft.Azure.Functions.Worker;
using System.Security.Claims;

namespace api.Helpers
{
    public class GetUserClaim
    {
        public static Guid GetUserExternalId(FunctionContext context)
        {
            var principal = context.Items["User"] as ClaimsPrincipal;
            var identity = principal?.Identity as ClaimsIdentity;
            var oid = identity?.FindFirst("oid")?.Value;

            if (string.IsNullOrWhiteSpace(oid))
                throw new UnauthorizedAccessException("User not authenticated or missing oid claim.");

            return Guid.Parse(oid);
        }
    }
}
