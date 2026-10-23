using System;
using System.IO;

namespace ForeverLauncher;

internal static class Settings
{
	public static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForeverLauncher");

	private static string FilePath => Path.Combine(Dir, "gamedir.txt");

	private static string ServerFile => Path.Combine(Dir, "server.txt");

	private static string GameLangFile => Path.Combine(Dir, "gamelang.txt");

	private static string NoFogFile => Path.Combine(Dir, "nofog.txt");

	private static string SoftwareRenderFile => Path.Combine(Dir, "softwarerender.txt");

	public static string LoadServer()
	{
		try
		{
			return File.ReadAllText(ServerFile).Trim();
		}
		catch
		{
			return null;
		}
	}

	public static void SaveServer(string name)
	{
		try
		{
			Directory.CreateDirectory(Dir);
			File.WriteAllText(ServerFile, name ?? "");
		}
		catch
		{
		}
	}

	public static string LoadGameDir()
	{
		try
		{
			string text = File.ReadAllText(FilePath).Trim();
			return File.Exists(Path.Combine(text, "WowB.exe")) ? text : null;
		}
		catch
		{
			return null;
		}
	}

	public static void SaveGameDir(string dir)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
			File.WriteAllText(FilePath, dir);
		}
		catch
		{
		}
	}

	public static string LoadGameLang()
	{
		try
		{
			return File.ReadAllText(GameLangFile).Trim();
		}
		catch
		{
			return "";
		}
	}

	public static void SaveGameLang(string code)
	{
		try
		{
			Directory.CreateDirectory(Dir);
			File.WriteAllText(GameLangFile, code ?? "");
		}
		catch
		{
		}
	}

	public static bool LoadRemoveFog()
	{
		try
		{
			return File.ReadAllText(NoFogFile).Trim() == "1";
		}
		catch
		{
			return false;
		}
	}

	public static void SaveRemoveFog(bool on)
	{
		try
		{
			Directory.CreateDirectory(Dir);
			File.WriteAllText(NoFogFile, on ? "1" : "0");
		}
		catch
		{
		}
	}

	public static bool LoadSoftwareRendering()
	{
		try
		{
			return File.ReadAllText(SoftwareRenderFile).Trim() == "1";
		}
		catch
		{
			return false;
		}
	}

	public static void SaveSoftwareRendering()
	{
		try
		{
			Directory.CreateDirectory(Dir);
			File.WriteAllText(SoftwareRenderFile, "1");
		}
		catch
		{
		}
	}
}
