using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace ForeverLauncher;

public static class LocalConfigStore
{
	private const string FileName = "localservers.json";

	private const string HereToken = "{here}";

	private static string ExeDir => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

	private static string PortablePath => Path.Combine(ExeDir, "localservers.json");

	private static bool Portable => File.Exists(PortablePath);

	private static string FilePath
	{
		get
		{
			if (!Portable)
			{
				return Path.Combine(Settings.Dir, "localservers.json");
			}
			return PortablePath;
		}
	}

	public static string RepackDir
	{
		get
		{
			if (!Portable || !File.Exists(Path.Combine(ExeDir, "Setup.bat")))
			{
				return null;
			}
			return ExeDir;
		}
	}

	public static bool RepackSetupDone
	{
		get
		{
			string repackDir = RepackDir;
			if (repackDir != null)
			{
				if (File.Exists(Path.Combine(repackDir, "server\\forever.cert.pem")))
				{
					return Directory.Exists(Path.Combine(repackDir, "data\\maps"));
				}
				return false;
			}
			return true;
		}
	}

	private static string Expand(string s)
	{
		if (!Portable)
		{
			return s;
		}
		return s.Replace("{here}", ExeDir.TrimEnd('\\'));
	}

	private static string Collapse(string s)
	{
		string oldValue = ExeDir.TrimEnd('\\');
		if (!Portable || s.Length <= 0)
		{
			return s;
		}
		return s.Replace(oldValue, "{here}");
	}

	private static Dictionary<string, object> ReadAll()
	{
		try
		{
			return (new JavaScriptSerializer().DeserializeObject(File.ReadAllText(FilePath, Encoding.UTF8)) as Dictionary<string, object>) ?? new Dictionary<string, object>();
		}
		catch
		{
			return new Dictionary<string, object>();
		}
	}

	public static LocalConfig Load(string serverName)
	{
		LocalConfig localConfig = new LocalConfig();
		Dictionary<string, object> dictionary = (ReadAll().TryGetValue(serverName ?? "", out var value) ? (value as Dictionary<string, object>) : null);
		if (dictionary == null)
		{
			return localConfig;
		}
		localConfig.BinDir = Expand(Str(dictionary, "binDir"));
		localConfig.MysqlExe = Expand(Str(dictionary, "mysqlExe"));
		localConfig.MysqlArgs = Expand(Str(dictionary, "mysqlArgs"));
		localConfig.ConsoleUser = Str(dictionary, "consoleUser");
		localConfig.ConsolePassword = Str(dictionary, "consolePassword");
		localConfig.WebExe = Expand(Str(dictionary, "webExe"));
		localConfig.WebArgs = Expand(Str(dictionary, "webArgs"));
		if (int.TryParse(Str(dictionary, "webPort"), out var result) && result > 0)
		{
			localConfig.WebPort = result;
		}
		localConfig.BotExe = Expand(Str(dictionary, "botExe"));
		localConfig.BotArgs = Expand(Str(dictionary, "botArgs"));
		if (int.TryParse(Str(dictionary, "botPort"), out var result2) && result2 > 0)
		{
			localConfig.BotPort = result2;
		}
		object[] array = (dictionary.TryGetValue("realms", out var value2) ? (value2 as object[]) : null);
		if (array != null && array.Length > 0)
		{
			localConfig.Realms = new List<RealmServerConfig>();
			foreach (Dictionary<string, object> item in array.OfType<Dictionary<string, object>>())
			{
				localConfig.Realms.Add(new RealmServerConfig
				{
					Name = ((Str(item, "name").Length > 0) ? Str(item, "name") : "Realm"),
					Config = Str(item, "config"),
					Port = (int.TryParse(Str(item, "port"), out var result3) ? result3 : 8085),
					Enabled = (!bool.TryParse(Str(item, "enabled"), out var result4) || result4)
				});
			}
		}
		return localConfig;
	}

	public static void Save(string serverName, LocalConfig c)
	{
		Dictionary<string, object> dictionary = ReadAll();
		dictionary[serverName ?? ""] = new Dictionary<string, object>
		{
			{
				"binDir",
				Collapse(c.BinDir)
			},
			{
				"mysqlExe",
				Collapse(c.MysqlExe)
			},
			{
				"mysqlArgs",
				Collapse(c.MysqlArgs)
			},
			{
				"consoleUser",
				c.ConsoleUser ?? ""
			},
			{
				"consolePassword",
				c.ConsolePassword ?? ""
			},
			{
				"webExe",
				Collapse(c.WebExe ?? "")
			},
			{
				"webArgs",
				Collapse(c.WebArgs ?? "")
			},
			{ "webPort", c.WebPort },
			{
				"botExe",
				Collapse(c.BotExe ?? "")
			},
			{
				"botArgs",
				Collapse(c.BotArgs ?? "")
			},
			{ "botPort", c.BotPort },
			{
				"realms",
				((IEnumerable<RealmServerConfig>)c.Realms).Select((Func<RealmServerConfig, object>)((RealmServerConfig r) => new Dictionary<string, object>
				{
					{ "name", r.Name },
					{ "config", r.Config },
					{ "port", r.Port },
					{ "enabled", r.Enabled }
				})).ToArray()
			}
		};
		Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
		File.WriteAllText(FilePath, PrettyJson(new JavaScriptSerializer().Serialize(dictionary)), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	private static string Str(Dictionary<string, object> d, string k)
	{
		if (!d.TryGetValue(k, out var value) || value == null)
		{
			return "";
		}
		return value.ToString();
	}

	private static string PrettyJson(string json)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		bool flag = false;
		for (int i = 0; i < json.Length; i++)
		{
			char c = json[i];
			if (c == '"' && (i == 0 || json[i - 1] != '\\'))
			{
				flag = !flag;
			}
			if (flag)
			{
				stringBuilder.Append(c);
				continue;
			}
			switch (c)
			{
			case '[':
			case '{':
				stringBuilder.Append(c).Append('\n').Append(' ', ++num * 2);
				break;
			case ']':
			case '}':
				stringBuilder.Append('\n').Append(' ', --num * 2).Append(c);
				break;
			case ',':
				stringBuilder.Append(c).Append('\n').Append(' ', num * 2);
				break;
			case ':':
				stringBuilder.Append(": ");
				break;
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		return stringBuilder.ToString() + "\n";
	}
}
