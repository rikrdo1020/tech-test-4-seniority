using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Manually added this store procedure that insert a notification and returns the inserted row
            migrationBuilder.Sql(@"
                CREATE PROCEDURE dbo.sp_AddNotification
                    @RecipientUserId UNIQUEIDENTIFIER,
                    @RelatedTaskId UNIQUEIDENTIFIER = NULL,
                    @Title NVARCHAR(200),
                    @Message NVARCHAR(2000) = NULL,
                    @Type INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    INSERT INTO dbo.Notifications (RecipientUserId, RelatedTaskId, Title, Message, Type, IsRead, CreatedAt)
                    OUTPUT inserted.Id, inserted.RecipientUserId, inserted.RelatedTaskId, inserted.Title, inserted.Message, inserted.Type, inserted.IsRead, inserted.CreatedAt, inserted.ReadAt
                    VALUES (@RecipientUserId, @RelatedTaskId, @Title, @Message, @Type, 0, SYSUTCDATETIME());
                END
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_AddNotification;");
        }
    }
}
