
namespace api.Models.Dtos
{
    public class UserSummaryDto
    {
        public Guid Id { get; set; }
        public Guid? ExternalId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
