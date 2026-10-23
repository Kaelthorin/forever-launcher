using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ForeverLauncher;

public sealed class LocalConfig
{
	public string BinDir = "";

	public string MysqlExe = "";

	public string MysqlArgs = "";

	public string ConsoleUser = "";

	public string ConsolePassword = "";

	public List<RealmServerConfig> Realms = new List<RealmServerConfig>
	{
		new RealmServerConfig()
	};

	public string WebExe = "";

	public string WebArgs = "";

	public int WebPort = 443;

	public string BotExe = "";

	public string BotArgs = "";

	public int BotPort = 8190;

	public int MysqlPort
	{
		get
		{
			Match match = Regex.Match(MysqlArgs ?? "", "--port[= ](\\d+)");
			if (!match.Success)
			{
				return 3306;
			}
			return int.Parse(match.Groups[1].Value);
		}
	}

	public string BnetExe
	{
		get
		{
			if (!string.IsNullOrEmpty(BinDir))
			{
				return Path.Combine(BinDir, "bnetserver.exe");
			}
			return "";
		}
	}

	public string WorldExe
	{
		get
		{
			if (!string.IsNullOrEmpty(BinDir))
			{
				return Path.Combine(BinDir, "worldserver.exe");
			}
			return "";
		}
	}
}
