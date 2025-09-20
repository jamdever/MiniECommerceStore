using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mini_E_Commerce_Store.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "State",
                table: "Users",
                newName: "ShippingState");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "Users",
                newName: "ShippingPostalCode");

            migrationBuilder.RenameColumn(
                name: "Country",
                table: "Users",
                newName: "ShippingCountry");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "Users",
                newName: "ShippingCity");

            migrationBuilder.RenameColumn(
                name: "AddressLine2",
                table: "Users",
                newName: "ShippingAddressLine2");

            migrationBuilder.RenameColumn(
                name: "AddressLine1",
                table: "Users",
                newName: "ShippingAddressLine1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShippingState",
                table: "Users",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "ShippingPostalCode",
                table: "Users",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "ShippingCountry",
                table: "Users",
                newName: "Country");

            migrationBuilder.RenameColumn(
                name: "ShippingCity",
                table: "Users",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "ShippingAddressLine2",
                table: "Users",
                newName: "AddressLine2");

            migrationBuilder.RenameColumn(
                name: "ShippingAddressLine1",
                table: "Users",
                newName: "AddressLine1");
        }
    }
}
