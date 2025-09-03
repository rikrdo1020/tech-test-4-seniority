using api.Models.Dtos;
using Azure.Identity;
using Microsoft.Graph;

namespace api.Services.Implementations
{
    public class GraphService : IGraphService
    {
        private readonly GraphServiceClient _graphClient;

        public GraphService()
        {
            var tenantId = Environment.GetEnvironmentVariable("TenantId");
            var clientId = Environment.GetEnvironmentVariable("AppRegistrationCliendId");
            var clientSecret = Environment.GetEnvironmentVariable("AppRegistrationClientSecret");

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                throw new InvalidOperationException("Missing Graph configuration in environment variables.");

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _graphClient = new GraphServiceClient(credential);
        }

        /// <summary>
        /// Retrieves user information from Microsoft Graph by external ID.
        /// </summary>
        /// <param name="externalId">External user ID (typically Azure AD ObjectId).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>UserDto containing Name and Email from Graph.</returns>
        /// <exception cref="ServiceException">Thrown if Graph API call fails.</exception>
        public async Task<UserDto> GetUserFromGraphAsync(Guid externalId, CancellationToken ct = default)
        {
            var userId = externalId.ToString();

            var user = await _graphClient.Users[userId]
                 .GetAsync(requestConfiguration =>
                 {
                     requestConfiguration.QueryParameters.Select = new[] { "displayName", "mail", "userPrincipalName" };
                 }, ct);

            return new UserDto
            {
                Name = user.DisplayName,
                Email = user.Mail ?? user.UserPrincipalName
            };
        }
    }
}
