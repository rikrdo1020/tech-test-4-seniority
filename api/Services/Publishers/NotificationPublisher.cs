namespace api.Services.Publishers
{
    public interface INotificationPublisher
    {
        Task PublishAsync(Notification notification);
    }

    public class NoOpNotificationPublisher : INotificationPublisher
    {
        public Task PublishAsync(Notification notification) => Task.CompletedTask;
    }
}
