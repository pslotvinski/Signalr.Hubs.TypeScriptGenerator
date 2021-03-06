﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CommandLine;

namespace GeniusSports.Signalr.Hubs.TypeScriptGenerator.Console
{
	public class Program
	{
		public static int Main(string[] args)
		{
			if (AppDomain.CurrentDomain.IsDefaultAppDomain())
			{
				return RunInNewAppDomainToAllowRazorEngineToCleanup(args);
			}

			try
			{
				var options = new CommandLineOptions();
				if (Parser.Default.ParseArguments(args, options))
				{
					Run(options);
					return 0;
				}

				System.Console.WriteLine("Error parsing command line options");
				System.Console.WriteLine(options.GetUsage());
				System.Console.ReadKey();
				return 1;
			}
			catch (Exception e)
			{
				System.Console.WriteLine("Error generating TypeScript");
				System.Console.WriteLine(e);
				return 1;
			}
		}

		private static int RunInNewAppDomainToAllowRazorEngineToCleanup(string[] args)
		{
			var appDomain = AppDomain.CreateDomain("RazorEngine", null, AppDomain.CurrentDomain.SetupInformation);
			var exitCode = appDomain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location, args);
			AppDomain.Unload(appDomain);
			return exitCode;
		}

		private static void Run(CommandLineOptions commandLineOptions)
		{
			LoadAssemblies(commandLineOptions.AssemblyPath);

			string outputPath = commandLineOptions.GetOutputPath();
			var exportsFilePath = GetExportsFilePath(outputPath);

			var generatorOptions = GetTypeScriptGeneratorOptions(commandLineOptions);

			var hubTypeScriptGenerator = new HubTypeScriptGenerator();
			var output = hubTypeScriptGenerator.Generate(generatorOptions);

			WriteOutput(output.Item1, outputPath);
			WriteOutput(output.Item2, exportsFilePath);
		}

		private static TypeScriptGeneratorOptions GetTypeScriptGeneratorOptions(
			CommandLineOptions commandLineOptions)
		{
			var options = TypeScriptGeneratorOptions.Default
				.WithOptionalMembers(commandLineOptions.GetOptionalMemberGenerationMode())
				.WithIncludedTypes(commandLineOptions.GetIncludedTypesDiscovery());

			if (commandLineOptions.StrictTypes)
				options = options.WithStrictTypes(commandLineOptions.GetNotNullableTypeDiscovery());

			if (!string.IsNullOrEmpty(commandLineOptions.References))
				options = options.WithReferencePaths(commandLineOptions.References);

			return options;

		}

		private static string GetExportsFilePath(string outputPath)
		{
			if (outputPath == null)
				return null;

			if (outputPath.Length == 0)
				return string.Empty;

			string[] extensions = {".d.ts", ".ts"};
			const string exportsSuffix = ".exports";

			foreach (var knownExtension in extensions)
			{
				if (outputPath.EndsWith(knownExtension, StringComparison.InvariantCultureIgnoreCase))
				{
					return string.Concat(
						outputPath.Substring(0, outputPath.Length - knownExtension.Length), 
						exportsSuffix, 
						extensions[1]);
				}
			}

			var extension = Path.GetExtension(outputPath);

			if (!string.IsNullOrEmpty(extension))
			{
				return Path.ChangeExtension(outputPath, exportsSuffix + extension);
			}
			else
			{
				return outputPath + exportsSuffix;
			}
		}

		private static void LoadAssemblies(string assemblyPath)
		{
			var assemblyLoader = new AssemblyLoader();
			assemblyLoader.LoadAssemblyIntoAppDomain(assemblyPath);
		}

		private static void WriteOutput(string content, string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				System.Console.WriteLine(content);
			}
			else
			{
				File.WriteAllText(filePath, content);
			}
		}
	}
}