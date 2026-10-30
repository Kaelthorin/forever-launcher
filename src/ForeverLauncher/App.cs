using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace ForeverLauncher;

public static class App
{
	public const string Version = "2.4.2";

	public const string Title = "Forever Launcher";

	public const string ReleasesUrl = "https://github.com/Kittnz/forever-launcher/releases/latest";

	public const string DiscordUrl = "https://discord.gg/4tqq7EQjs9";

	public static string UpdatedTo;

	public static string CrashFile => Path.Combine(Settings.Dir, "crash.txt");

	[STAThread]
	public static void Main()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		int num = Array.IndexOf(commandLineArgs, "--updated");
		if (num > 0)
		{
			UpdatedTo = ((num + 1 < commandLineArgs.Length) ? commandLineArgs[num + 1] : "2.4.2");
		}
		bool createdNew;
		using Mutex mutex = new Mutex(initiallyOwned: true, "ForeverLauncher-single", out createdNew);
		if (!createdNew && UpdatedTo != null)
		{
			try
			{
				createdNew = mutex.WaitOne(TimeSpan.FromSeconds(30.0));
			}
			catch (AbandonedMutexException)
			{
				createdNew = true;
			}
		}
		if (!createdNew)
		{
			L.Load();
			MessageBox.Show(L.Get("app.already"), "Forever Launcher");
			return;
		}
		L.Load();
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		AppDomain.CurrentDomain.UnhandledException += (object s, UnhandledExceptionEventArgs e) =>
		{
			Crash(e.ExceptionObject as Exception);
		};
		if (Settings.LoadSoftwareRendering())
		{
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
		}
		Application app = new Application
		{
			ShutdownMode = ShutdownMode.OnExplicitShutdown
		};
		app.DispatcherUnhandledException += (object s, DispatcherUnhandledExceptionEventArgs e) =>
		{
			Crash(e.Exception);
			e.Handled = true;
			app.Shutdown();
		};
		LauncherWindow launcherWindow = new LauncherWindow(app);
		try
		{
			app.Run(launcherWindow.Window);
		}
		catch (Exception ex2)
		{
			if (!IsRenderFailure(ex2))
			{
				throw;
			}
			Log(ex2);
			Settings.SaveSoftwareRendering();
		}
	}

	private static bool IsRenderFailure(Exception ex)
	{
		for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
		{
			string text = ex2.StackTrace ?? "";
			if ((ex2 is OutOfMemoryException || ex2 is COMException) && text.Contains("System.Windows.Media.MediaContext"))
			{
				return true;
			}
			if (ex2.Message.Contains("UCEERR_RENDERTHREADFAILURE"))
			{
				return true;
			}
		}
		return false;
	}

	private static void Log(Exception ex)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(CrashFile));
			File.AppendAllText(CrashFile, string.Concat(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), " v2.4.2", Environment.NewLine, ex, Environment.NewLine, Environment.NewLine));
		}
		catch
		{
		}
	}

	private static void Crash(Exception ex)
	{
		Log(ex);
		try
		{
			if (IsRenderFailure(ex))
			{
				bool flag = Settings.LoadSoftwareRendering();
				Settings.SaveSoftwareRendering();
				MessageBox.Show(L.Get(flag ? "app.crash" : "app.renderfail", CrashFile), "Forever Launcher", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			else
			{
				MessageBox.Show(L.Get("app.crash", CrashFile), "Forever Launcher", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
		catch
		{
		}
	}
}
