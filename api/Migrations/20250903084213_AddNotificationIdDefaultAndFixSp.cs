using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationIdDefaultAndFixSp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE dbo.Notifications
                ADD CONSTRAINT DF_Notifications_Id DEFAULT (NEWID()) FOR Id;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_AddNotification
                    @RecipientUserId UNIQUEIDENTIFIER,
                    @RelatedTaskId UNIQUEIDENTIFIER = NULL,
                    @Title NVARCHAR(200),
                    @Message NVARCHAR(2000) = NULL,
                    @Type INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @NewId UNIQUEIDENTIFIER = NEWID();

                    INSERT INTO dbo.Notifications (Id, RecipientUserId, RelatedTaskId, Title, Message, Type, IsRead, CreatedAt)
                    OUTPUT inserted.Id, inserted.RecipientUserId, inserted.RelatedTaskId, inserted.Title, inserted.Message, inserted.Type, inserted.IsRead, inserted.CreatedAt, inserted.ReadAt
                    VALUES (@NewId, @RecipientUserId, @RelatedTaskId, @Title, @Message, @Type, 0, SYSUTCDATETIME());
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE dbo.Notifications
                DROP CONSTRAINT DF_Notifications_Id;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE dbo.sp_AddNotification
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
    }
}
