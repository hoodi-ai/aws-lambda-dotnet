﻿using Amazon.Lambda.TestTool.Runtime;
using Amazon.Lambda.TestTool.SampleRequests;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Amazon.Lambda.TestTool
{
    public class TestToolStartup
    {
        public class RunConfiguration
        {
            public enum RunMode { Normal, Test };

            /// <summary>
            /// If this is set to Test then that disables any interactive activity or any calls to Environment.Exit which wouldn't work well during a test run.
            /// </summary>
            public RunMode Mode { get; set; } = RunMode.Normal;

            /// <summary>
            /// Allows you to capture the output for tests to example instead of just writing to the console windows.
            /// </summary>
            public TextWriter OutputWriter { get; set; } = Console.Out;
        }

        public static void Startup(string productName, Action<LocalLambdaOptions, bool> uiStartup, string[] args)
        {
            Startup(productName, uiStartup, args, new RunConfiguration());
        }

        public static void Startup(string productName, Action<LocalLambdaOptions, bool> uiStartup, string[] args, RunConfiguration runConfiguration)
        {
            try
            {
                Utils.PrintToolTitle(productName);

                var commandOptions = CommandLineOptions.Parse(args);
                if (commandOptions.ShowHelp)
                {
                    CommandLineOptions.PrintUsage();
                    return;
                }

                var localLambdaOptions = new LocalLambdaOptions()
                {
                    Host = commandOptions.Host,
                    Port = commandOptions.Port,
                    DefaultAWSProfile = commandOptions.AWSProfile,
                    DefaultAWSRegion = commandOptions.AWSRegion,
                    DefaultTemplateFile = commandOptions.Template
                };

                runConfiguration.OutputWriter.WriteLine($"Host : {localLambdaOptions.Host}");
                runConfiguration.OutputWriter.WriteLine($"Port : {localLambdaOptions.Port}");
                runConfiguration.OutputWriter.WriteLine($"DefaultAWSProfile : {localLambdaOptions.DefaultAWSProfile}");
                runConfiguration.OutputWriter.WriteLine($"DefaultAWSRegion : {localLambdaOptions.DefaultAWSRegion}");
                runConfiguration.OutputWriter.WriteLine($"DefaultTemplateFile : {localLambdaOptions.DefaultTemplateFile}");

                var lambdaAssemblyDirectory = commandOptions.Path ?? Directory.GetCurrentDirectory();

#if NETCOREAPP3_1
                var targetFramework = "netcoreapp3.1";
#elif NET5_0
                var targetFramework = "net5.0";
#elif NET6_0
                var targetFramework = "net6.0";
#endif

                // Check to see if running in debug mode from this project's directory which means the test tool is being debugged.
                // To make debugging easier pick one of the test Lambda projects.
                if (lambdaAssemblyDirectory.EndsWith("Amazon.Lambda.TestTool.WebTester21"))
                {
                    lambdaAssemblyDirectory = Path.Combine(lambdaAssemblyDirectory, $"../../tests/LambdaFunctions/netcore21/S3EventFunction/bin/Debug/{targetFramework}");
                }
                else if (lambdaAssemblyDirectory.EndsWith("Amazon.Lambda.TestTool.WebTester31"))
                {
                    lambdaAssemblyDirectory = Path.Combine(lambdaAssemblyDirectory, $"../../tests/LambdaFunctions/netcore31/S3EventFunction/bin/Debug/{targetFramework}");
                }
                // If running in the project directory select the build directory so the deps.json file can be found.
                else if (Utils.IsProjectDirectory(lambdaAssemblyDirectory))
                {
                    lambdaAssemblyDirectory = Path.Combine(lambdaAssemblyDirectory, $"bin/Debug/{targetFramework}");
                }

                localLambdaOptions.LambdaRuntime = LocalLambdaRuntime.Initialize(lambdaAssemblyDirectory);
                runConfiguration.OutputWriter.WriteLine($"Loaded local Lambda runtime from project output {lambdaAssemblyDirectory}");

                if (commandOptions.NoUI)
                {
                    runConfiguration.OutputWriter.WriteLine($"NoUI is not supported.");
                }
                else
                {
                    // Look for aws-lambda-tools-defaults.json or other config files.
                    var templateDirectory = commandOptions.ProjectDir ?? Directory.GetCurrentDirectory();
                    runConfiguration.OutputWriter.WriteLine($"SearchForTemplateFiles in {templateDirectory}");
                    localLambdaOptions.TemplateFiles = Utils.SearchForTemplateFiles(templateDirectory);

                    // Start the test tool web server.
                    uiStartup(localLambdaOptions, !commandOptions.NoLaunchWindow);
                }
            }
            catch (CommandLineParseException e)
            {
                runConfiguration.OutputWriter.WriteLine($"Invalid command line arguments: {e.Message}");
                runConfiguration.OutputWriter.WriteLine("Use the --help option to learn about the possible command line arguments");
                if (runConfiguration.Mode == RunConfiguration.RunMode.Normal)
                {
                    if (Debugger.IsAttached)
                    {
                        Console.WriteLine("Press any key to exit");
                        Console.ReadKey();
                    }
                    System.Environment.Exit(-1);
                }
            }
            catch (Exception e)
            {
                runConfiguration.OutputWriter.WriteLine($"Unknown error occurred causing process exit: {e.Message}");
                runConfiguration.OutputWriter.WriteLine(e.StackTrace);
                if (runConfiguration.Mode == RunConfiguration.RunMode.Normal)
                {
                    if (Debugger.IsAttached)
                    {
                        Console.WriteLine("Press any key to exit");
                        Console.ReadKey();
                    }
                    System.Environment.Exit(-2);
                }
            }
        }
    }
}
