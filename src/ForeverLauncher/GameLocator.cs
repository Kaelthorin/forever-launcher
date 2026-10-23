using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace ForeverLauncher;

internal static class GameLocator
{
	public static string Find()
	{
		List<string> list = new List<string>();
		string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		list.Add(directoryName);
		list.Add(Path.Combine(directoryName, "_classic_beta_"));
		list.Add(Path.Combine(Path.GetDirectoryName(directoryName) ?? directoryName, "_classic_beta_"));
		string[] array = new string[2] { "SOFTWARE\\WOW6432Node\\Blizzard Entertainment\\World of Warcraft", "SOFTWARE\\Blizzard Entertainment\\World of Warcraft" };
		foreach (string name in array)
		{
			try
			{
				using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name);
				if (registryKey == null)
				{
					continue;
				}
				string[] array2 = new string[2] { "InstallPath", "GamePath" };
				foreach (string name2 in array2)
				{
					string text = registryKey.GetValue(name2) as string;
					if (!string.IsNullOrEmpty(text))
					{
						if (File.Exists(text))
						{
							text = Path.GetDirectoryName(text);
						}
						text = text.TrimEnd('\\');
						string directoryName2 = Path.GetDirectoryName(text);
						if (directoryName2 != null)
						{
							list.Add(Path.Combine(directoryName2, "_classic_beta_"));
						}
						list.Add(Path.Combine(text, "_classic_beta_"));
					}
				}
			}
			catch
			{
			}
		}
		foreach (DriveInfo item in DriveInfo.GetDrives().Where((DriveInfo d) =>
		{
			try
			{
				return d.DriveType == DriveType.Fixed && d.IsReady;
			}
			catch
			{
				return false;
			}
		}))
		{
			string fullName = item.RootDirectory.FullName;
			string[] array3 = new string[8] { "World of Warcraft", "Program Files (x86)\\World of Warcraft", "Program Files\\World of Warcraft", "Games\\World of Warcraft", "Juegos\\World of Warcraft", "Battle.net\\World of Warcraft", "Blizzard\\World of Warcraft", "Program Files (x86)\\Battle.net\\World of Warcraft" };
			foreach (string path in array3)
			{
				list.Add(Path.Combine(fullName, path, "_classic_beta_"));
			}
		}
		foreach (string item2 in list)
		{
			try
			{
				if (File.Exists(Path.Combine(item2, "WowB.exe")))
				{
					return Path.GetFullPath(item2);
				}
			}
			catch
			{
			}
		}
		return null;
	}
}
