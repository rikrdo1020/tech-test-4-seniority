using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class NotificationsFunction
{
    private readonly INotificationService _service;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public NotificationsFunction(INotificationService service)
    {
        _service = service;
    }

    [Function("GetNotifications")]
    public async Task<HttpResponseData> GetNotifications(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "notifications")] HttpRequestData req)
    {
        var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        if (!Guid.TryParse(qs["userId"], out var userId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Missing or invalid userId query parameter");
            return bad;
        }

        var page = int.TryParse(qs["page"], out var p) ? Math.Max(1, p) : 1;
        var pageSize = int.TryParse(qs["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;

        var result = await _service.GetByUserAsync(userId, page, pageSize);
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteStringAsync(JsonSerializer.Serialize(result, _jsonOptions));
        return res;
    }

    [Function("CreateNotification")]
    public async Task<HttpResponseData> CreateNotification(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var dto = JsonSerializer.Deserialize<CreateNotificationDto>(body, _jsonOptions);
        if (dto == null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid payload");
            return bad;
        }

        var created = await _service.CreateAsync(dto);
        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteStringAsync(JsonSerializer.Serialize(created, _jsonOptions));
        return res;
    }

    [Function("MarkAsRead")]
    public async Task<HttpResponseData> MarkAsRead(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications/{id}/read")] HttpRequestData req,
        string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid id");
            return bad;
        }

        await _service.MarkAsReadAsync(guid);
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("MarkAllAsRead")]
    public async Task<HttpResponseData> MarkAllAsRead(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications/readAll")] HttpRequestData req)
    {
        var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        if (!Guid.TryParse(qs["userId"], out var userId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Missing or invalid userId query parameter");
            return bad;
        }

        await _service.MarkAllAsReadAsync(userId);
        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}