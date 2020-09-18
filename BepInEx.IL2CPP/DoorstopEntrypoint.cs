﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BepInEx.IL2CPP
{
	internal static class UnityPreloaderRunner
	{
		public static void PreloaderMain(string[] args)
		{
			string bepinPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath(EnvVars.DOORSTOP_INVOKE_DLL_PATH)));

			Paths.SetExecutablePath(EnvVars.DOORSTOP_PROCESS_PATH, bepinPath);
			Preloader.IL2CPPUnhollowedPath = Path.Combine(Paths.BepInExRootPath, "unhollowed");

			AppDomain.CurrentDomain.AssemblyResolve += LocalResolve;
			AppDomain.CurrentDomain.AssemblyResolve -= DoorstopEntrypoint.ResolveCurrentDirectory;

			File.WriteAllText("B:\\a.txt", "a");

			//AppDomain.CurrentDomain.TypeResolve += (sender, eventArgs) =>
			//{
			//	eventArgs.
			//}

			Preloader.Run();
		}

		internal static Assembly LocalResolve(object sender, ResolveEventArgs args)
		{
			var assemblyName = new AssemblyName(args.Name);

			var foundAssembly = AppDomain.CurrentDomain.GetAssemblies()
										 .FirstOrDefault(x => x.GetName().Name == assemblyName.Name);

			if (foundAssembly != null)
				return foundAssembly;

			if (Utility.TryResolveDllAssembly(assemblyName, Paths.BepInExAssemblyDirectory, out foundAssembly)
				|| Utility.TryResolveDllAssembly(assemblyName, Paths.PatcherPluginPath, out foundAssembly)
				|| Utility.TryResolveDllAssembly(assemblyName, Paths.PluginPath, out foundAssembly)
				|| Utility.TryResolveDllAssembly(assemblyName, Preloader.IL2CPPUnhollowedPath, out foundAssembly))
				return foundAssembly;

			return null;
		}
	}

	internal static class DoorstopEntrypoint
	{
		private static string preloaderPath;

		/// <summary>
		///     The main entrypoint of BepInEx, called from Doorstop.
		/// </summary>
		/// <param name="args">
		///     The arguments passed in from Doorstop. First argument is the path of the currently executing
		///     process.
		/// </param>
		public static void Main(string[] args)
		{
			// We set it to the current directory first as a fallback, but try to use the same location as the .exe file.
			string silentExceptionLog = $"preloader_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log";

			try
			{
				EnvVars.LoadVars();

				silentExceptionLog = Path.Combine(Path.GetDirectoryName(EnvVars.DOORSTOP_PROCESS_PATH), silentExceptionLog);

				// Get the path of this DLL via Doorstop env var because Assembly.Location mangles non-ASCII characters on some versions of Mono for unknown reasons
				preloaderPath = Path.GetDirectoryName(Path.GetFullPath(EnvVars.DOORSTOP_INVOKE_DLL_PATH));

				AppDomain.CurrentDomain.AssemblyResolve += ResolveCurrentDirectory;

				UnityPreloaderRunner.PreloaderMain(args);
			}
			catch (Exception ex)
			{
				File.WriteAllText(silentExceptionLog, ex.ToString());
			}
		}

		public static Assembly ResolveCurrentDirectory(object sender, ResolveEventArgs args)
		{
			var name = new AssemblyName(args.Name);

			try
			{
				return Assembly.LoadFile(Path.Combine(preloaderPath, $"{name.Name}.dll"));
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}