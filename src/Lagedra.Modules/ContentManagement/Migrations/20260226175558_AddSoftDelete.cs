using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "content",
                table: "seo_pages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "content",
                table: "seo_pages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "content",
                table: "blog_posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "content",
                table: "blog_posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "content",
                table: "seo_pages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "content",
                table: "seo_pages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "content",
                table: "blog_posts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "content",
                table: "blog_posts");
        }
    }
}
