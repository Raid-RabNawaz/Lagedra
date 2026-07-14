using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reviews.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reviews");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "partner_service_reviews",
                schema: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndorsementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallRating = table.Column<int>(type: "integer", nullable: false),
                    Responsiveness = table.Column<int>(type: "integer", nullable: false),
                    Reliability = table.Column<int>(type: "integer", nullable: false),
                    SupportQuality = table.Column<int>(type: "integer", nullable: false),
                    PublicComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_service_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stay_review_windows",
                schema: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    LandlordUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpensAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuestSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    HostSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stay_review_windows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stay_reviews",
                schema: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevieweeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OverallRating = table.Column<int>(type: "integer", nullable: false),
                    Cleanliness = table.Column<int>(type: "integer", nullable: true),
                    Accuracy = table.Column<int>(type: "integer", nullable: true),
                    Communication = table.Column<int>(type: "integer", nullable: true),
                    Location = table.Column<int>(type: "integer", nullable: true),
                    CheckIn = table.Column<int>(type: "integer", nullable: true),
                    Value = table.Column<int>(type: "integer", nullable: true),
                    RespectHouseRules = table.Column<int>(type: "integer", nullable: true),
                    PublicComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PrivateFeedbackToPlatform = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stay_reviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                schema: "reviews",
                table: "outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_partner_service_reviews_OrganizationId",
                schema: "reviews",
                table: "partner_service_reviews",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_partner_service_reviews_OrganizationId_ReviewerUserId",
                schema: "reviews",
                table: "partner_service_reviews",
                columns: new[] { "OrganizationId", "ReviewerUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stay_review_windows_ClosesAt",
                schema: "reviews",
                table: "stay_review_windows",
                column: "ClosesAt");

            migrationBuilder.CreateIndex(
                name: "IX_stay_review_windows_DealId",
                schema: "reviews",
                table: "stay_review_windows",
                column: "DealId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stay_review_windows_IsPublished",
                schema: "reviews",
                table: "stay_review_windows",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_DealId_Direction",
                schema: "reviews",
                table: "stay_reviews",
                columns: new[] { "DealId", "Direction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_ListingId",
                schema: "reviews",
                table: "stay_reviews",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_RevieweeUserId",
                schema: "reviews",
                table: "stay_reviews",
                column: "RevieweeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_Status",
                schema: "reviews",
                table: "stay_reviews",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "reviews");

            migrationBuilder.DropTable(
                name: "partner_service_reviews",
                schema: "reviews");

            migrationBuilder.DropTable(
                name: "stay_review_windows",
                schema: "reviews");

            migrationBuilder.DropTable(
                name: "stay_reviews",
                schema: "reviews");
        }
    }
}
