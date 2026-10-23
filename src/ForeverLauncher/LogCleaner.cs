using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForeverLauncher;

public static class LogCleaner
{
	public static string Run(LocalServer local)
	{
		LocalConfig config = local.Config;
		long num = 0L;
		int num2 = 0;
		List<string> list = new List<string>();
		List<KeyValuePair<ServerProgram, string>> list2 = new List<KeyValuePair<ServerProgram, string>>();
		list2.Add(new KeyValuePair<ServerProgram, string>(local.Bnet, Path.Combine(config.BinDir, "bnetserver.conf")));
		foreach (ServerProgram world in local.Worlds)
		{
			RealmServerConfig realmServerConfig = local.WorldRealms[world];
			list2.Add(new KeyValuePair<ServerProgram, string>(world, Path.Combine(config.BinDir, string.IsNullOrEmpty(realmServerConfig.Config) ? "worldserver.conf" : realmServerConfig.Config)));
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<ServerProgram, string> item in list2)
		{
			if (!File.Exists(item.Value))
			{
				continue;
			}
			string text = ConfFile.Get(item.Value, "LogsDir", "");
			string fullPath = Path.GetFullPath(Path.IsPathRooted(text) ? text : Path.Combine(config.BinDir, text));
			if (item.Key.IsRunning)
			{
				list.Add(item.Key.Name);
			}
			else
			{
				if (!hashSet.Add(fullPath) || !Directory.Exists(fullPath))
				{
					continue;
				}
				foreach (string item2 in Directory.GetFiles(fullPath, "*.log").Concat(Directory.GetFiles(fullPath, "*.pkt")))
				{
					try
					{
						long length = new FileInfo(item2).Length;
						File.Delete(item2);
						num += length;
						num2++;
					}
					catch
					{
					}
				}
			}
		}
		string text2 = L.Get("con.logscleared", num2, ((double)num / 1048576.0).ToString("0.0"));
		if (list.Count > 0)
		{
			text2 = text2 + " " + L.Get("con.logsrunning", string.Join(", ", list.ToArray()));
		}
		return text2;
	}
}
