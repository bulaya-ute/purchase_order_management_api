using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PurchaseOrderManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationTaxDiscountAndPoSupplierBid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "Quotations",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "Quotations",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PurchaseOrderSupplierBids",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false),
                    SupplierBidId = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderSupplierBids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderSupplierBids_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderSupplierBids_SupplierBids_SupplierBidId",
                        column: x => x.SupplierBidId,
                        principalTable: "SupplierBids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderSupplierBids_PurchaseOrderId_SupplierBidId",
                table: "PurchaseOrderSupplierBids",
                columns: new[] { "PurchaseOrderId", "SupplierBidId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderSupplierBids_SupplierBidId",
                table: "PurchaseOrderSupplierBids",
                column: "SupplierBidId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseOrderSupplierBids");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "Quotations");
        }
    }
}
