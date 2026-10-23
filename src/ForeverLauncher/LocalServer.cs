using System;
using System.Collections.Generic;
using System.Linq;

namespace ForeverLauncher;

public sealed class LocalServer
{
	private LocalConfig config = new LocalConfig();

	public readonly ServerProgram Mysql;

	public readonly ServerProgram Bnet;

	public readonly ServerProgram Web;

	public readonly ServerProgram Bot;

	public readonly List<ServerProgram> Worlds = new List<ServerProgram>();

	public readonly Dictionary<ServerProgram, RealmServerConfig> WorldRealms = new Dictionary<ServerProgram, RealmServerConfig>();

	public LocalConfig Config
	{
		get
		{
			return config;
		}
		set
		{
			config = value ?? new LocalConfig();
			if (config.Realms == null || config.Realms.Count == 0)
			{
				config.Realms = new List<RealmServerConfig>
				{
					new RealmServerConfig()
				};
			}
			Worlds.Clear();
			WorldRealms.Clear();
			foreach (RealmServerConfig realm in config.Realms)
			{
				RealmServerConfig r = realm;
				ServerProgram serverProgram = new ServerProgram("worldserver (" + r.Name + ")", () => config.WorldExe, () => (!string.IsNullOrEmpty(r.Config)) ? ("-c \"" + r.Config + "\"") : "", 90, () => r.Port);
				Worlds.Add(serverProgram);
				WorldRealms[serverProgram] = r;
			}
		}
	}

	public IEnumerable<ServerProgram> All => new ServerProgram[2] { Mysql, Bnet }.Concat(Worlds).Concat(HasWeb ? new ServerProgram[1] { Web } : new ServerProgram[0]).Concat(HasBot ? new ServerProgram[1] { Bot } : new ServerProgram[0]);

	public bool HasWeb => !string.IsNullOrEmpty(config.WebExe);

	public bool HasBot => !string.IsNullOrEmpty(config.BotExe);

	public IEnumerable<ServerProgram> StartOrder => ((HasBot && Bot.Configured) ? new ServerProgram[1] { Bot } : new ServerProgram[0]).Concat(new ServerProgram[2] { Mysql, Bnet }.Where((ServerProgram p) => p.Configured)).Concat(Worlds.Where((ServerProgram w) => w.Configured && WorldRealms[w].Enabled)).Concat((HasWeb && Web.Configured) ? new ServerProgram[1] { Web } : new ServerProgram[0]);

	public IEnumerable<ServerProgram> StopOrder => new ServerProgram[1] { Web }.Concat(Worlds.AsEnumerable().Reverse()).Concat(new ServerProgram[3] { Bnet, Mysql, Bot });

	public LocalServer()
	{
		Func<string> exe = () => config.MysqlExe;
		Mysql = new ServerProgram("MySQL", exe, () => config.MysqlArgs, 60, () => config.MysqlPort);
		Bnet = new ServerProgram("bnetserver", () => config.BnetExe, () => "", 20);
		Web = new ServerProgram("Website", () => config.WebExe, () => config.WebArgs, 15, () => config.WebPort);
		Bot = new ServerProgram("Discord bot", () => config.BotExe, () => config.BotArgs, 20, () => config.BotPort);
		Config = config;
	}
}
