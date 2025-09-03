using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

public class NotificationRepository : INotificationRepository
{
    private readonly DataContext _context;

    public NotificationRepository(DataContext db)
    {
        _context = db;
    }

    //public async Task<Notification> AddAsync(Notification notification)
    //{
    //    _context.Notifications.Add(notification);
    //    await _context.SaveChangesAsync();
    //    return notification;
    //}

    public async Task<Notification> AddAsync(Notification notification)
    {
        var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.sp_AddNotification";
        cmd.CommandType = CommandType.StoredProcedure;

        var pRecipient = cmd.CreateParameter();
        pRecipient.ParameterName = "@RecipientUserId";
        pRecipient.DbType = DbType.Guid;
        pRecipient.Value = notification.RecipientUserId;
        cmd.Parameters.Add(pRecipient);

        var pRelated = cmd.CreateParameter();
        pRelated.ParameterName = "@RelatedTaskId";
        pRelated.DbType = DbType.Guid;
        pRelated.Value = (object?)notification.RelatedTaskId ?? DBNull.Value;
        cmd.Parameters.Add(pRelated);

        var pTitle = cmd.CreateParameter();
        pTitle.ParameterName = "@Title";
        pTitle.DbType = DbType.String;
        pTitle.Value = notification.Title;
        cmd.Parameters.Add(pTitle);

        var pMessage = cmd.CreateParameter();
        pMessage.ParameterName = "@Message";
        pMessage.DbType = DbType.String;
        pMessage.Value = (object?)notification.Message ?? DBNull.Value;
        cmd.Parameters.Add(pMessage);

        var pType = cmd.CreateParameter();
        pType.ParameterName = "@Type";
        pType.DbType = DbType.Int32;
        pType.Value = (int)notification.Type;
        cmd.Parameters.Add(pType);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var created = new Notification
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                RecipientUserId = reader.GetGuid(reader.GetOrdinal("RecipientUserId")),
                RelatedTaskId = reader.IsDBNull(reader.GetOrdinal("RelatedTaskId"))
                    ? null
                    : reader.GetGuid(reader.GetOrdinal("RelatedTaskId")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Message = reader.IsDBNull(reader.GetOrdinal("Message")) ? null : reader.GetString(reader.GetOrdinal("Message")),
                Type = (NotificationType)reader.GetInt32(reader.GetOrdinal("Type")),
                IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                ReadAt = reader.IsDBNull(reader.GetOrdinal("ReadAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReadAt"))
            };

            return created;
        }

        throw new InvalidOperationException("The notification could not be created using the stored procedure.");
    }

    public async Task<PagedResult<Notification>> GetByUserAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Notification>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _context.Notifications.FindAsync(id);
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        var n = await _context.Notifications.FindAsync(id);
        if (n == null) return;
        if (!n.IsRead)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var items = await _context.Notifications.Where(n => n.RecipientUserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in items)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
}