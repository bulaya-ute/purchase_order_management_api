using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurchaseOrderManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeSourceQuotationLineItemIdRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear all rows that would violate the NOT NULL constraint being added.
            // These tables hold bid items (and anything that references them) that were seeded
            // without a source quotation line — authorised by the user as a one-time data reset.
            migrationBuilder.Sql(@"
                DELETE FROM ""PurchaseOrderLineItems"" WHERE ""SourceSupplierBidItemId"" IS NOT NULL;
                DELETE FROM ""SupplierBidItems"";
                DELETE FROM ""PurchaseOrderSupplierBids"";
                UPDATE ""PurchaseOrders"" SET ""AwardedSupplierBidId"" = NULL, ""AwardedAtUtc"" = NULL, ""AwardedByUserId"" = NULL WHERE ""AwardedSupplierBidId"" IS NOT NULL;
                DELETE FROM ""SupplierBids"";
            ");

            migrationBuilder.AlterColumn<int>(
                name: "SourceQuotationLineItemId",
                table: "SupplierBidItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SourceQuotationLineItemId",
                table: "SupplierBidItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
