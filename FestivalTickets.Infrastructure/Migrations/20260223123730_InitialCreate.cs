using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FestivalTickets.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Festivals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Place = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Logo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BasicPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Festivals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FestivalId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packages_Festivals_FestivalId",
                        column: x => x.FestivalId,
                        principalTable: "Festivals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageItems",
                columns: table => new
                {
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageItems", x => new { x.PackageId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_PackageItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageItems_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Festivals",
                columns: new[] { "Id", "BasicPrice", "Description", "EndDate", "Logo", "Name", "Place", "StartDate" },
                values: new object[] { 1, 199.00m, "Three-day music festival.", new DateOnly(2025, 8, 24), "/img/logos/lowlands.svg", "Lowlands", "Biddinghuizen", new DateOnly(2025, 8, 22) });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "ItemType", "Name", "Price" },
                values: new object[,]
                {
                    { 1, 0, "Campingspot Small", 25m },
                    { 2, 0, "Campingspot Large", 40m },
                    { 3, 0, "Glamping Upgrade", 120m },
                    { 4, 1, "Meal Voucher", 12.5m },
                    { 5, 1, "Drink Pack", 15m },
                    { 6, 1, "Breakfast", 9.5m },
                    { 7, 2, "Parking Day", 10m },
                    { 8, 2, "Parking Weekend", 25m },
                    { 9, 2, "VIP Parking", 50m },
                    { 10, 3, "T-Shirt", 30m },
                    { 11, 3, "Hoodie", 55m },
                    { 12, 3, "Poster", 12m },
                    { 13, 4, "VIP Day", 80m },
                    { 14, 4, "VIP Weekend", 200m },
                    { 15, 4, "Backstage Tour", 150m },
                    { 16, 5, "Locker", 15m },
                    { 17, 5, "Powerbank Rental", 8m },
                    { 18, 5, "Rain Poncho", 5m }
                });

            migrationBuilder.InsertData(
                table: "Packages",
                columns: new[] { "Id", "FestivalId", "Name" },
                values: new object[,]
                {
                    { 1, 1, "Weekend Basic" },
                    { 2, 1, "Weekend Plus" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Festivals_Name",
                table: "Festivals",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemType_Name",
                table: "Items",
                columns: new[] { "ItemType", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageItems_ItemId",
                table: "PackageItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_FestivalId",
                table: "Packages",
                column: "FestivalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageItems");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "Festivals");
        }
    }
}
