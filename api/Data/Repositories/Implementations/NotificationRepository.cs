using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

public class NotificationRepository : INotificationRepository
{
    private readonly DataContext _context;

    public NotificationRepository(DataContext db)
    {
        _context = db;
    }

    /// <summary>
    /// Creates a new notification by calling the "dbo.sp_AddNotification" stored procedure.
    /// </summary>
    /// <param name="notification">The notification data to insert (RecipientUserId, RelatedTaskId, Title, Message, Type).</param>
    /// <returns>The created Notification entity with server-generated fields (Id, CreatedAt, etc).</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the stored procedure does not return a row with the created notification.
    /// </exception>
    /// <remarks>
    /// - Opens a raw database connection and executes a DbCommand (bypasses EF change-tracking).  
    /// - Null values are passed as DBNull to the stored procedure.  
    /// - Maps the first result row manually to a Notification entity.  
    /// - Ensure the stored procedure returns all required fields with correct names and types.
    /// </remarks>
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

    /// <summary>
    /// Retrieves a paged list of notifications for the specified recipient user,
    /// ordered by CreatedAt descending (newest first).
    /// </summary>
    /// <param name="userId">ID of the recipient user to fetch notifications for.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>PagedResult&lt;Notification&gt; containing Items, TotalCount, Page and PageSize.</returns>
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

    /// <summary>
    /// Gets the total count of unread notifications for a specific user.
    /// </summary>
    /// <param name="userId">The recipient user's GUID.</param>
    /// <returns>The number of unread notifications (where IsRead is false).</returns>
    /// <remarks>
    /// - Executes a COUNT query using EF Core with a predicate on RecipientUserId and IsRead.
    /// - For performance, ensure there is a composite index on (RecipientUserId, IsRead).
    /// </remarks>
    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
    }

    /// <summary>
    /// Retrieves a notification by its unique ID.
    /// </summary>
    /// <param name="id">The GUID of the notification to retrieve.</param>
    /// <returns>The Notification entity if found; otherwise null.</returns>
    /// <remarks>
    /// - Uses EF Core's FindAsync which may return from change tracker if already loaded.
    /// - Executes a primary key lookup (efficient if Id is the primary key).
    /// </remarks>
    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _context.Notifications.FindAsync(id);
    }

    /// <summary>
    /// Marks a notification as read by setting IsRead = true and ReadAt to current UTC time.
    /// </summary>
    /// <param name="id">The GUID of the notification to mark as read.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <remarks>
    /// - Fetches the notification by ID using FindAsync (may use change tracker).
    /// - Only updates if the notification exists and IsRead is currently false.
    /// - Calls SaveChangesAsync to persist the change.
    /// - No-op if notification is not found or already marked as read.
    /// </remarks>
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

    /// <summary>
    /// Marks all unread notifications for a user as read in a single batch operation.
    /// </summary>
    /// <param name="userId">The recipient user's GUID.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <remarks>
    /// - Fetches all unread notifications for the user in one query.
    /// - Updates each notification in memory (IsRead = true, ReadAt = UTC now).
    /// - Persists all changes with a single SaveChangesAsync call.
    /// - Efficient for small to moderate numbers of unread notifications.
    /// - For very large sets, consider a bulk update via raw SQL or stored procedure.
    /// </remarks>
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