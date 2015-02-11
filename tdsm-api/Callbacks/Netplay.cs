﻿using System;
using System.IO;
using System.Reflection;
using System.Threading;
using tdsm.api.Plugin;
#if Full_API
using Terraria;
#endif

namespace tdsm.api.Callbacks
{
    public static class Netplay
    {
        public static void StartServer(object state)
        {
#if Full_API
            var ctx = new HookContext()
            {
                Sender = HookContext.ConsoleSender
            };
            var args = new HookArgs.StartDefaultServer();
            HookPoints.StartDefaultServer.Invoke(ref ctx, ref args);

            if (ctx.Result != HookResult.IGNORE)
            {
                Console.Write("Starting server...");
                ThreadPool.QueueUserWorkItem(new WaitCallback(Terraria.Netplay.ServerLoop), 1);
                Tools.WriteLine("Ok");
            }
#endif
        }
    }
}