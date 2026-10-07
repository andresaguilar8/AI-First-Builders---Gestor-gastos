using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorGastos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "expenses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "expense_months",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_expenses_category_id",
                table: "expenses",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_months_category_id",
                table: "expense_months",
                column: "category_id");

            migrationBuilder.AddForeignKey(
                name: "fk_expense_months_categories_category_id",
                table: "expense_months",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_expenses_categories_category_id",
                table: "expenses",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expense_months_categories_category_id",
                table: "expense_months");

            migrationBuilder.DropForeignKey(
                name: "fk_expenses_categories_category_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "ix_expenses_category_id",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "ix_expense_months_category_id",
                table: "expense_months");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "expense_months");
        }
    }
}
