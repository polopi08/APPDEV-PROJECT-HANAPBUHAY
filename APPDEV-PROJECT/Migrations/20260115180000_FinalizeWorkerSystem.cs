using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APPDEV_PROJECT.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeWorkerSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration finalizes the Worker system model
            // The Workers table was created in AddWorkerTable migration
            // This migration ensures the model snapshot is up to date
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down operation needed for this finalization migration
        }
    }
}
