using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ForeverLauncher;

public static class StatusClient
{
	public static async Task<ServerStatus> FetchAsync(ServerEntry server)
	{
		ServerStatus s = new ServerStatus();
		Task<long> login = ProbeAsync(server.Portal, server.LoginPort);
		List<Task<long>> realms = new List<Task<long>>();
		foreach (RealmEntry realm in server.Realms)
		{
			realms.Add(ProbeAsync(server.Portal, realm.Port));
		}
		Task feed = (string.IsNullOrEmpty(server.StatusUrl) ? Task.FromResult(0) : FeedAsync(s, server.StatusUrl));
		s.LoginMs = await login;
		s.LoginUp = s.LoginMs >= 0;
		for (int i = 0; i < realms.Count; i++)
		{
			RealmStatus rs = new RealmStatus
			{
				Realm = server.Realms[i],
				Ms = await realms[i]
			};
			s.Realms.Add(rs);
			if (rs.Up)
			{
				s.WorldUp = true;
			}
		}
		await feed;
		return s;
	}

	private static async Task<long> ProbeAsync(string host, int port)
	{
		TcpClient c = new TcpClient();
		try
		{
			Stopwatch sw = Stopwatch.StartNew();
			Task connect = c.ConnectAsync(host, port);
			if (await Task.WhenAny(connect, Task.Delay(3000)) != connect)
			{
				return -1L;
			}
			await connect;
			return sw.ElapsedMilliseconds;
		}
		catch
		{
			return -1L;
		}
		finally
		{
			try
			{
				c.Close();
			}
			catch
			{
			}
		}
	}

	private static async Task FeedAsync(ServerStatus s, string feedUrl)
	{
		int num = default;
		_ = num;
		_ = 0;
		try
		{
			string json;
			using (WebClient wc = new WebClient())
			{
				wc.Encoding = Encoding.UTF8;
				wc.Headers[HttpRequestHeader.CacheControl] = "no-cache";
				json = await wc.DownloadStringTaskAsync(feedUrl + (feedUrl.Contains("?") ? "&t=" : "?t=") + DateTime.UtcNow.Ticks / 600000000);
			}
			if (!(new JavaScriptSerializer().DeserializeObject(json) is Dictionary<string, object> root))
			{
				return;
			}
			s.FeedOk = true;
			if (DateTime.TryParse(Str(root, "updated"), null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var updated))
			{
				s.FeedFresh = (DateTime.UtcNow - updated).TotalMinutes < 15.0;
			}
			Dictionary<string, object> realm = (root.ContainsKey("realm") ? (root["realm"] as Dictionary<string, object>) : null);
			if (realm != null && s.FeedFresh)
			{
				s.FeedLoginUp = Bool(realm, "login");
				s.FeedWorldUp = Bool(realm, "world");
			}
			s.Maintenance = Bool(root, "maintenance");
			s.Root = new FeedEntry(root);
			Dictionary<string, object> launcher = (root.ContainsKey("launcher") ? (root["launcher"] as Dictionary<string, object>) : null);
			if (launcher != null)
			{
				s.LatestLauncher = Str(launcher, "version");
				s.LauncherUrl = Str(launcher, "url");
			}
			foreach (object item in List(root, "news"))
			{
				if (item is Dictionary<string, object> d)
				{
					s.News.Add(new FeedEntry(d));
				}
			}
			foreach (object item2 in List(root, "links"))
			{
				if (item2 is Dictionary<string, object> d2 && Str(d2, "label") != null && Str(d2, "url") != null)
				{
					s.Links.Add(new FeedEntry(d2));
				}
			}
			foreach (object item3 in List(root, "clientBuilds"))
			{
				if (item3 is string text && Regex.IsMatch(text, "^1\\.60\\.\\d+\\.\\d{5}$"))
				{
					s.ClientBuilds.Add(text);
				}
			}
			if (s.ClientBuilds.Count > 0)
			{
				Patcher.FeedVersions = s.ClientBuilds.ToArray();
			}
		}
		catch
		{
		}
	}

	private static string Str(Dictionary<string, object> d, string k)
	{
		if (!d.TryGetValue(k, out var value) || value == null)
		{
			return null;
		}
		return value.ToString();
	}

	private static bool Bool(Dictionary<string, object> d, string k)
	{
		if (d.TryGetValue(k, out var value) && value is bool)
		{
			return (bool)value;
		}
		return false;
	}

	private static IEnumerable List(Dictionary<string, object> d, string k)
	{
		if (!d.TryGetValue(k, out var value) || !(value is IEnumerable) || value is string)
		{
			return new object[0];
		}
		return (IEnumerable)value;
	}

	public static bool IsNewer(string latest, string current)
	{
		if (latest != null && Version.TryParse(latest, out var result) && Version.TryParse(current, out var result2))
		{
			return result > result2;
		}
		return false;
	}
}
