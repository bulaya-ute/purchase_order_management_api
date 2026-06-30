using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PurchaseOrderManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class QuotationBidLibraryCurrencyAndPoTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_SupplierBids_SupplierBidId",
                table: "Quotations");

            migrationBuilder.RenameColumn(
                name: "SupplierBidId",
                table: "Quotations",
                newName: "SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_Quotations_SupplierBidId",
                table: "Quotations",
                newName: "IX_Quotations_SupplierId");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "PurchaseOrders",
                newName: "CurrencyCode");

            migrationBuilder.AlterColumn<int>(
                name: "PurchaseOrderId",
                table: "SupplierBids",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "SupplierBidItems",
                type: "char(3)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Quotations",
                type: "char(3)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderTypeId",
                table: "PurchaseOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetCompanyId",
                table: "PurchaseOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "PurchaseOrderLineItems",
                type: "char(3)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Code = table.Column<string>(type: "char(3)", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypes_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypes_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypes_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderCurrencyTotals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderCurrencyTotals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderCurrencyTotals_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderCurrencyTotals_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderTypeAllowedCreatorRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderTypeId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderTypeAllowedCreatorRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeAllowedCreatorRoles_PurchaseOrderTypes_Pur~",
                        column: x => x.PurchaseOrderTypeId,
                        principalTable: "PurchaseOrderTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeAllowedCreatorRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeAllowedCreatorRoles_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeAllowedCreatorRoles_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeAllowedCreatorRoles_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderTypeApprovalSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderTypeId = table.Column<int>(type: "integer", nullable: false),
                    RequiredRoleId = table.Column<int>(type: "integer", nullable: true),
                    RequiredUserId = table.Column<int>(type: "integer", nullable: true),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderTypeApprovalSteps", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrderTypeApprovalSteps_ExactlyOneRequiredRoleOrUser", "(\"RequiredRoleId\" IS NOT NULL AND \"RequiredUserId\" IS NULL) OR (\"RequiredRoleId\" IS NULL AND \"RequiredUserId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeApprovalSteps_PurchaseOrderTypes_PurchaseO~",
                        column: x => x.PurchaseOrderTypeId,
                        principalTable: "PurchaseOrderTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeApprovalSteps_Roles_RequiredRoleId",
                        column: x => x.RequiredRoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeApprovalSteps_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeApprovalSteps_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeApprovalSteps_Users_RequiredUserId",
                        column: x => x.RequiredUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTypeApprovalSteps_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierBidItems_CurrencyCode",
                table: "SupplierBidItems",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CurrencyCode",
                table: "Quotations",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CurrencyCode",
                table: "PurchaseOrders",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PurchaseOrderTypeId",
                table: "PurchaseOrders",
                column: "PurchaseOrderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TargetCompanyId",
                table: "PurchaseOrders",
                column: "TargetCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLineItems_CurrencyCode",
                table: "PurchaseOrderLineItems",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderCurrencyTotals_CurrencyCode",
                table: "PurchaseOrderCurrencyTotals",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderCurrencyTotals_PurchaseOrderId_CurrencyCode",
                table: "PurchaseOrderCurrencyTotals",
                columns: new[] { "PurchaseOrderId", "CurrencyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeAllowedCreatorRoles_CreatedByUserId",
                table: "PurchaseOrderTypeAllowedCreatorRoles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeAllowedCreatorRoles_DeletedByUserId",
                table: "PurchaseOrderTypeAllowedCreatorRoles",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeAllowedCreatorRoles_PurchaseOrderTypeId_Ro~",
                table: "PurchaseOrderTypeAllowedCreatorRoles",
                columns: new[] { "PurchaseOrderTypeId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeAllowedCreatorRoles_RoleId",
                table: "PurchaseOrderTypeAllowedCreatorRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeAllowedCreatorRoles_UpdatedByUserId",
                table: "PurchaseOrderTypeAllowedCreatorRoles",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeApprovalSteps_CreatedByUserId",
                table: "PurchaseOrderTypeApprovalSteps",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeApprovalSteps_DeletedByUserId",
                table: "PurchaseOrderTypeApprovalSteps",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeApprovalSteps_PurchaseOrderTypeId",
                table: "PurchaseOrderTypeApprovalSteps",
                column: "PurchaseOrderTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeApprovalSteps_RequiredRoleId",
                table: "PurchaseOrderTypeApprovalSteps",
                column: "RequiredRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeApprovalSteps_RequiredUserId",
                table: "PurchaseOrderTypeApprovalSteps",
                column: "RequiredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypeApprovalSteps_UpdatedByUserId",
                table: "PurchaseOrderTypeApprovalSteps",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypes_CreatedByUserId",
                table: "PurchaseOrderTypes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypes_DeletedByUserId",
                table: "PurchaseOrderTypes",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTypes_UpdatedByUserId",
                table: "PurchaseOrderTypes",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLineItems_Currencies_CurrencyCode",
                table: "PurchaseOrderLineItems",
                column: "CurrencyCode",
                principalTable: "Currencies",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Companies_TargetCompanyId",
                table: "PurchaseOrders",
                column: "TargetCompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Currencies_CurrencyCode",
                table: "PurchaseOrders",
                column: "CurrencyCode",
                principalTable: "Currencies",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_PurchaseOrderTypes_PurchaseOrderTypeId",
                table: "PurchaseOrders",
                column: "PurchaseOrderTypeId",
                principalTable: "PurchaseOrderTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_Currencies_CurrencyCode",
                table: "Quotations",
                column: "CurrencyCode",
                principalTable: "Currencies",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_Suppliers_SupplierId",
                table: "Quotations",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierBidItems_Currencies_CurrencyCode",
                table: "SupplierBidItems",
                column: "CurrencyCode",
                principalTable: "Currencies",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLineItems_Currencies_CurrencyCode",
                table: "PurchaseOrderLineItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Companies_TargetCompanyId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Currencies_CurrencyCode",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_PurchaseOrderTypes_PurchaseOrderTypeId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_Currencies_CurrencyCode",
                table: "Quotations");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_Suppliers_SupplierId",
                table: "Quotations");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierBidItems_Currencies_CurrencyCode",
                table: "SupplierBidItems");

            migrationBuilder.DropTable(
                name: "PurchaseOrderCurrencyTotals");

            migrationBuilder.DropTable(
                name: "PurchaseOrderTypeAllowedCreatorRoles");

            migrationBuilder.DropTable(
                name: "PurchaseOrderTypeApprovalSteps");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "PurchaseOrderTypes");

            migrationBuilder.DropIndex(
                name: "IX_SupplierBidItems_CurrencyCode",
                table: "SupplierBidItems");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_CurrencyCode",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CurrencyCode",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_PurchaseOrderTypeId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TargetCompanyId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderLineItems_CurrencyCode",
                table: "PurchaseOrderLineItems");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "SupplierBidItems");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderTypeId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TargetCompanyId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "PurchaseOrderLineItems");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "Quotations",
                newName: "SupplierBidId");

            migrationBuilder.RenameIndex(
                name: "IX_Quotations_SupplierId",
                table: "Quotations",
                newName: "IX_Quotations_SupplierBidId");

            migrationBuilder.RenameColumn(
                name: "CurrencyCode",
                table: "PurchaseOrders",
                newName: "Currency");

            migrationBuilder.AlterColumn<int>(
                name: "PurchaseOrderId",
                table: "SupplierBids",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_SupplierBids_SupplierBidId",
                table: "Quotations",
                column: "SupplierBidId",
                principalTable: "SupplierBids",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
