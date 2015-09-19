﻿using System;
using OTA.Command;
using System.Linq;
using OTA.Data;
using OTA.Data.Entity.Models;
using TDSM.Core.Data;
using OTA.Logging;
using TDSM.Core.ServerCharacters;
using TDSM.Core.ServerCharacters.Tables;

namespace TDSM.Core
{
    public partial class Entry
    {
        public const String Setting_Mana = "SSC_Mana";
        public const String Setting_MaxMana = "SSC_MaxMana";
        public const String Setting_Health = "SSC_Health";
        public const String Setting_MaxHealth = "SSC_MaxHealth";

        protected override void DatabaseInitialising(System.Data.Entity.DbModelBuilder builder)
        {
            base.DatabaseInitialising(builder);

            using (var dbc = new TDSM.Core.Data.TContext())
            {
                dbc.CreateModel(builder);
            }
        }

        protected override void DatabaseCreated()
        {
            base.DatabaseCreated();

            ProgramLog.Admin.Log("Creating default groups...");
            CreateDefaultGroups();
            ProgramLog.Admin.Log("Creating default SSC values...");
            DefaultLoadoutTable.PopulateDefaults(CharacterManager.StartingOutInfo);
        }

        public void CreateDefaultGroups()
        {
            var pc = CommandParser.GetAvailableCommands(AccessLevel.PLAYER);
            var ad = CommandParser.GetAvailableCommands(AccessLevel.OP);
            var op = CommandParser.GetAvailableCommands(AccessLevel.CONSOLE); //Funny how these have now changed

            using (var ctx = new OTAContext())
            {
                CreateGroup("Guest", true, null, 255, 255, 255, pc
                    .Where(x => !String.IsNullOrEmpty(x.Value.Node))
                    .Select(x => x.Value.Node)
                    .Distinct()
                    .ToArray(), ctx, "[Guest] ");
                CreateGroup("Admin", false, "Guest", 240, 131, 77, ad
                    .Where(x => !String.IsNullOrEmpty(x.Value.Node))
                    .Select(x => x.Value.Node)
                    .Distinct()
                    .ToArray(), ctx, "[Admin] ");
                CreateGroup("Operator", false, "Admin", 77, 166, 240, op
                    .Where(x => !String.IsNullOrEmpty(x.Value.Node))
                    .Select(x => x.Value.Node)
                    .Distinct()
                    .ToArray(), ctx, "[OP] ");
            }
        }

        static void CreateGroup(string name, bool guest, string parent, byte r, byte g, byte b, string[] nodes, OTAContext ctx,
                                string chatPrefix = null,
                                string chatSuffix = null)
        {
            var grp = new Group()
            {
                Name = name,
                ApplyToGuests = guest,
                Parent = parent,
                Chat_Red = r,
                Chat_Green = g,
                Chat_Blue = b,
                Chat_Prefix = chatPrefix,
                Chat_Suffix = chatSuffix
            };
            ctx.Groups.Add(grp);

            ctx.SaveChanges(); //Save to get the ID

            foreach (var nd in nodes)
            {
                var node = ctx.Nodes.SingleOrDefault(x => x.Node == nd && x.Permission == Permission.Permitted);
                if (node == null)
                {
                    ctx.Nodes.Add(node = new NodePermission()
                        {
                            Node = nd,
                            Permission = Permission.Permitted
                        });

                    ctx.SaveChanges();
                }

                ctx.GroupNodes.Add(new GroupNode()
                    {
                        GroupId = grp.Id,
                        NodeId = node.Id 
                    });
            }

            ctx.SaveChanges();
        }
    }
}

