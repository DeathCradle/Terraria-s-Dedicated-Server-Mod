﻿using FluentMigrator;
using OTA.Data.Dapper.Extensions;
using OTA.Data.Dapper.Mappers;

namespace TDSM.Core.Data.Models.Migrations
{
    public partial class CreateAndSeed : Migration
    {
        public void APIAccountRole_Up()
        {
            var table = this.Create.Table<APIAccountRole>();

            table.WithColumn("Id")
                .AsInt64()
                .Identity()
                .PrimaryKey()
                .NotNullable()
                .Unique();

            table.WithColumn("AccountId")
                .AsInt64()
                .NotNullable()
                .Unique()
                .ForeignKey(TableMapper.TypeToName<APIAccount>(), "Id");

            table.WithColumn("Type")
                .AsString(255)
                .NotNullable();

            table.WithColumn("Value")
                .AsString(255)
                .NotNullable();

            table.WithColumn("DateFrom")
                .AsDateTime()
                .NotNullable();

            table.WithColumn("DateTo")
                .AsDateTime()
                .Nullable();
        }

        public void APIAccountRole_Down()
        {
            this.Delete.Table<APIAccountRole>();
        }
    }
}
