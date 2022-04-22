﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NLog;
using Plus.Communication.Packets;
using Plus.Core;
using Plus.HabboHotel.Rooms.Chat.Commands;

namespace Plus;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var collection = new ServiceCollection();
        collection.Scan(scan => scan.FromAssemblies(typeof(Program).Assembly)
            .AddClasses()
            .AsMatchingInterface()
            .WithSingletonLifetime());
        collection.AddAssignableTo<IChatCommand>();
        collection.AddAssignableTo<IPacketEvent>();

        var projectSolutionPath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationData(projectSolutionPath + "//Config//config.ini");
        collection.AddSingleton(configuration);

        var serviceProvider = collection.BuildServiceProvider();

        //XmlConfigurator.Configure();
        LogManager.LoadConfiguration("Config/nlog.config");
        Console.ForegroundColor = ConsoleColor.White;
        Console.CursorVisible = false;
        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.UnhandledException += OnUnhandledException;
        var environment = serviceProvider.GetRequiredService<IPlusEnvironment>();
        var started = await environment.Start();
        if (!started)
        {
            Environment.Exit(1);
            return;
        }
        while (true)
        {
            if (Console.ReadKey(true).Key == ConsoleKey.Enter)
            {
                Console.Write("plus> ");
                var input = Console.ReadLine();
                if (input.Length > 0)
                {
                    var s = input.Split(' ')[0];
                    ConsoleCommands.InvokeCommand(s);
                }
            }
        }
    }

    public static IServiceCollection AddAssignableTo<T>(this IServiceCollection services) =>
        services.Scan(scan => scan.FromAssemblies(typeof(Program).Assembly)
            .AddClasses(classes => classes.Where(t => t.IsAssignableTo(typeof(T)) && !t.IsAbstract && !t.IsInterface))
            .As<T>().WithTransientLifetime());

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var e = (Exception)args.ExceptionObject;
        //Logger.LogCriticalException("SYSTEM CRITICAL EXCEPTION: " + e);
        PlusEnvironment.PerformShutDown();
    }
}