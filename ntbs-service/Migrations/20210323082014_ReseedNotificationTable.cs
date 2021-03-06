﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace ntbs_service.Migrations
{
    public partial class ReseedNotificationTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF ((IDENT_CURRENT('Notification')) < 300000)
                    DBCC CHECKIDENT('Notification', RESEED, 300000);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do nothing
        }
    }
}
