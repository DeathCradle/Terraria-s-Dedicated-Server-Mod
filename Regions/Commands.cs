﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria_Server.Commands;
using Terraria_Server;
using Terraria_Server.Misc;
using Regions.RegionWork;

namespace Regions
{
    public class Commands
    {
        public static void Region(Server server, ISender sender, ArgumentList args)
        {
            /* Meh [START] */
            String Command = args[0];
            try
            {
                if (args.TryPop("select"))
                {
                    SelectionToolToggle(server, sender, args);
                }
                else if (args.TryPop("create"))
                {
                    if (sender is Player)
                    {
                        Vector2[] selection = Selection.GetSelection(sender as Player);
                        if (    selection == null 
                            || (selection[0] == null || selection[0] == default(Vector2))
                            || (selection[1] == null || selection[1] == default(Vector2)))
                        {
                            sender.sendMessage("Please select a region first!", 255);
                            return;
                        }
                    }
                    Create(server, sender, args);
                }
                else if (args.TryPop("user"))
                {
                    Boolean add = args.TryPop("add");
                    Boolean remove = args.TryPop("remove");

                    if (add)
                        AddUser(server, sender, args);
                    else if (remove)
                        RemoveUser(server, sender, args);
                    else
                        throw new CommandError("Please review your command.");
                }
                else if (args.TryPop("list"))
                {
                    List(server, sender, args);
                }
                else if (args.TryPop("projectile"))
                {
                    Boolean add = args.TryPop("add");
                    Boolean remove = args.TryPop("remove");

                    if (add)
                        AddUser(server, sender, args);
                    else if (remove)
                        RemoveUser(server, sender, args);
                    else
                        throw new CommandError("Please review your command.");
                }
                else
                {
                    sender.sendMessage("Region Commands: select, create, user, list.", 255);
                }
            } catch(CommandError e) {
                switch (Command)
                {
                    //case "select":
                    //    break;
                    case "create":
                        sender.sendMessage("'region create': Creates a new Region.", 255);
                        sender.sendMessage("'region create' paremeters: -name <name> -desc <description>", 255);
                        sender.sendMessage("              (To restrict newplayers add -res)", 255);
                        sender.sendMessage("               (To restrict NPCs add -npcres)", 255);
                        break;
                    case "user":
                        sender.sendMessage("'region user': Adds or Removes users from a Region Slot.", 255);
                        sender.sendMessage("'region user' paremeters: -name <name> -slot <region slot>", 255);
                        sender.sendMessage("              (To allow by ip use -ip instead of -name)", 255);
                        break;
                    //case "list":
                    //    break;
                    default :
                        throw e; //Unknown Error or an command with no args
                }
            }
            /* Meh [END] */
        }

        public static void SelectionToolToggle(Server server, ISender sender, ArgumentList args)
        {
            if (sender is Player)
            {
                var player = sender as Player;
                if (Selection.isInSelectionlist(player))
                {
                    Selection.selectionPlayers.Remove(player.Name);
                    player.sendMessage("You have turned off the Selection Tool", ChatColor.Red);
                }
                else
                {
                    Selection.selectionPlayers.Add(player.Name, new Vector2[] { default(Vector2), default(Vector2) });
                    player.sendMessage("You have turned on the Selection Tool", ChatColor.Green);
                }
            }

        }

        public static void Create(Server server, ISender sender, ArgumentList args)
        {
            if (sender is Player)
            {
                String Name = "";
                String Desc = "";
                Boolean Restrict = args.TryPop("-res");
                Boolean RestrictNPC = args.TryPop("-npcres");
                
                if (args.TryParseTwo<String, String>("-name", out Name, "-desc", out Desc)
                    && Name.Trim().Length > 0)
                {
                    var player = sender as Player;
                    if (Selection.isInSelectionlist(player))
                    {
                        Vector2[] regionAxis = Selection.GetSelection(player);

                        Region rgn = new Region();
                        rgn.Name = Name;
                        rgn.Description = Desc;
                        rgn.Point1 = regionAxis[0];
                        rgn.Point2 = regionAxis[1];
                        rgn.Restricted = Restrict;
                        rgn.RestrictedNPCs = RestrictNPC;

                        if (rgn.IsValidRegion())
                        {
                            Regions.regionManager.Regions.Add(rgn);
                            if(Regions.regionManager.SaveRegion(rgn))
                                player.sendMessage("Region '" + Name + "' was successfully created.", ChatColor.Green);
                            else
                                player.sendMessage("There was an issue while saving the region", ChatColor.Red);
                        }
                        else
                        {
                            player.sendMessage("There was an issue while creating the region", ChatColor.Red);
                        }
                    }
                    else
                    {
                        player.sendMessage("You need to select a region first!", ChatColor.Red);
                    }
                }
                else
                {
                    throw new CommandError("You have not specified certain arguments");
                }
            }
        }

        public static void List(Server server, ISender sender, ArgumentList args)
        {
            for (int i = 0; i < Regions.regionManager.Regions.Count; i++)
            {
                sender.sendMessage(string.Format("Slot {0} : {1} [ {2} ] ({3},{4})", i, Regions.regionManager.Regions[i].Name, 
                    Regions.regionManager.Regions[i].Description,
                        Regions.regionManager.Regions[i].Point1.X, Regions.regionManager.Regions[i].Point1.Y), 
                        255, 255, 0, 0);
            }
        }

        public static void AddUser(Server server, ISender sender, ArgumentList args)
        {
            String User = "", IP = "";
            Int32 Slot;

            //args.TryParseOne<String>("-ip", out IP); //Optional

            //IP or name?
            if (args.TryParseTwo<String, Int32>("-name", out User, "-slot", out Slot) || 
                args.TryParseTwo<String, Int32>("-ip", out User, "-slot", out Slot))
            {
                String[] exceptions = new String[2];
                if (User.Length > 0)
                {
                    exceptions[0] = User;
                }
                if (IP.Length > 0)
                {
                    exceptions[1] = IP;
                }

                Region region = null;
                for (int i = 0; i < Regions.regionManager.Regions.Count; i++)
                {
                    if (Slot == i)
                    {
                        region = Regions.regionManager.Regions[i];
                        break;
                    }
                }

                if (region == null)
                    throw new CommandError("Specified Region Slot was incorrect.");

                int usersAdded = 0;
                foreach (String toInflate in exceptions)
                {
                    if (toInflate != null)
                        foreach (String inflatee in toInflate.Split(','))
                        {
                            region.UserList.Add(inflatee);
                            usersAdded++;
                        }
                }

                if (usersAdded > 0)
                {
                    sender.sendMessage(string.Format("{0} users were added to {1}", usersAdded, region.Name),
                        255, 0, 255, 0);
                    Regions.Log(sender.Name + " created region {0} with {1} user/s", region.Name, usersAdded);
                }
                else
                    throw new CommandError("A user was not able to be added to a Region.");
            }
            else
                throw new CommandError("Invalid arguments, Please review your command.");
        }

        public static void RemoveUser(Server server, ISender sender, ArgumentList args)
        {
            String User = "", IP = "";
            Int32 Slot;

            args.TryParseOne<String>("-ip", out IP); //Optional

            //IP or name?
            if (args.TryParseTwo<String, Int32>("-name", out User, "-slot", out Slot))
            {
                String[] exceptions = new String[2];
                if (User.Length > 0)
                {
                    exceptions[0] = User;
                }
                if (IP.Length > 0)
                {
                    exceptions[1] = IP;
                }

                Region region = null;
                for (int i = 0; i < Regions.regionManager.Regions.Count; i++)
                {
                    if (Slot == i)
                    {
                        region = Regions.regionManager.Regions[i];
                        break;
                    }
                }

                if (region == null)
                    throw new CommandError("Specified Region Slot was incorrect.");

                int usersRemoved = 0;
                foreach (String toInflate in exceptions)
                {
                    if (toInflate != null)
                        foreach (String inflatee in toInflate.Split(','))
                        {
                            if (region.UserList.Contains(inflatee))
                            {
                                region.UserList.Add(inflatee);
                                usersRemoved++;
                            }
                        }
                }

                if (usersRemoved > 0)
                    sender.sendMessage(string.Format("{0} users were added to {1}", usersRemoved, region.Name),
                        255, 0, 255, 0);
                else
                    throw new CommandError("A user was not able to be removed from a Region.");
            }
            else
                throw new CommandError("Invalid arguments, Please review your command.");
        }

        public static void AddProjectile(Server server, ISender sender, ArgumentList args)
        {
            String projectiles;
            Int32 Slot;

            if (args.TryParseTwo<String, Int32>("-proj", out projectiles, "-slot", out Slot))
            {
                Region region = null;
                for (int i = 0; i < Regions.regionManager.Regions.Count; i++)
                {
                    if (Slot == i)
                    {
                        region = Regions.regionManager.Regions[i];
                        break;
                    }
                }

                if (region == null)
                    throw new CommandError("Specified Region Slot was incorrect.");

                int Count = 0;
                foreach (String proj in projectiles.Split(','))
                {
                    if (proj.Trim().Length > 0)
                    {
                        region.ProjectileList.Add(proj.Trim().ToLower().Replace(" ", ""));
                        Count++;
                    }
                }

                if (Count > 0)
                    sender.sendMessage(string.Join("{0} projectiles were blocked in Region '{1}'",
                        ((projectiles.Equals("*")) ? "All" : Count.ToString()), region.Name),
                        255, 0, 255);
                else
                    sender.sendMessage(string.Join("No projectiles specified to add to Region '{0}'", region.Name));
            }
            else
                throw new CommandError("Please review your command");
        }

        public static void RemoveProjectile(Server server, ISender sender, ArgumentList args)
        {
            String projectiles;
            Int32 Slot;

            if (args.TryParseTwo<String, Int32>("-proj", out projectiles, "-slot", out Slot))
            {
                Region region = null;
                for (int i = 0; i < Regions.regionManager.Regions.Count; i++)
                {
                    if (Slot == i)
                    {
                        region = Regions.regionManager.Regions[i];
                        break;
                    }
                }

                if (region == null)
                    throw new CommandError("Specified Region Slot was incorrect.");

                int Count = 0;
                foreach (String proj in projectiles.Split(','))
                {
                    if (proj.Trim().Length > 0)
                    {
                        String projectile = proj.Trim().ToLower().Replace(" ", "");
                        while (region.ProjectileList.Contains(projectile))
                        {
                            if(region.ProjectileList.Remove(projectile))
                                Count++;
                        }
                    }
                }

                if (Count > 0)
                    sender.sendMessage(string.Join("{0} projectiles were unblocked in Region '{1}'", 
                        ((projectiles.Equals("*")) ? "All" : Count.ToString()), region.Name),
                        255, 0, 255);
                else
                    sender.sendMessage(string.Join("No projectiles specified to remove from Region '{0}'", region.Name));
            }
            else
                throw new CommandError("Please review your command");
        }
    }
}
