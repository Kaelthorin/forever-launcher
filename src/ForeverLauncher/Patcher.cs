using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ForeverLauncher;

public sealed class Patcher
{
	private enum StoreState
	{
		Invalid,
		Original,
		Patched
	}

	public const string ConfigName = "ForeverLauncher.wtf";

	public const string ExeName = "WowB.exe";

	public const string ExpectedVersion = "1.60.1.70450";

	private const int EntrySize = 40;

	private const int EntryCount = 12;

	private const int BlockSize = 480;

	private const int CertKeysFromBuild = 70425;

	private const string AllowSuffix = "([:/\\?].*|\\n|$)";

	private const string AllowPrefix = "https://wauth.teledit.com/Danal";

	private const string AllowEntryHost = "wauth\\.teledit\\.com/Danal";

	private const string NewestFogBuild = "1.60.1.70425";

	public static readonly string[] SupportedVersions = new string[15]
	{
		"1.60.1.69913", "1.60.1.69977", "1.60.1.70009", "1.60.1.70058", "1.60.1.70124", "1.60.1.70170", "1.60.1.70205", "1.60.1.70235", "1.60.1.70245", "1.60.1.70291",
		"1.60.1.70334", "1.60.1.70338", "1.60.1.70374", "1.60.1.70425", "1.60.1.70450"
	};

	public static volatile string[] FeedVersions = new string[0];

	private static readonly string[] KnownKeys = new string[12]
	{
		"9B0671C815DFF513BFD4A2B26AE1F84EC9106841B2FB620DB65F6ADE7C21AD06", "112451B9843C2799E4750AA3D9B9AFABF536A645B863C4ADB2078B932B354E04", "A6B858485748BF38BE193517AB90F8DE169EFF0989EA9360DB346A378B0FFE15", "50FFDEB807F8FF73A299A16000AA6575C5945BE875ADFC8795374ED3415249AC", "9F842D078755647C008E5FE3E12B839A981DE02A1F520CB2545CC04CD2323E0E", "B8CC75864D8EF461D8DD6AA7425E2C63C24D3982066B8D773A1549BFE24E05EE", "76BB7BBDD9F34E124A4573C3AA227E3C87CC603506697D054F6FDEC342E56EBF", "1FD6DD8FA0EC30D39E3F72E755B8A045BDE0F70449DC71008B767C2EAA89FB9F", "15D618BD7DB577BD9A8D45769C59E4FC631633BF447398A4B489B4C26FBC03AD", "B34EC592D2AC4993F5FFC85B15C3DA9078694051CB224345592AF7136D796C99",
		"9E91586DD4B115AB0568B21959E181F6545910BC5E2F30B8975CA09D7BC3EFCE", "769C824A1DADD19AEEFA420414D1732DAF44537F98AA229FC5477BCA55F9D59A"
	};

	private static readonly byte[] ExpectedFlags = new byte[12]
	{
		1, 1, 1, 1, 1, 1, 1, 1, 0, 1,
		1, 1
	};

	private readonly HashSet<long> certCopies = new HashSet<long>();

	private static readonly string[][] HelpUrls = new string[3][]
	{
		new string[2] { "https://%s.battle.net/support/%s/help/home", "/kb" },
		new string[2] { "https://%s.battle.net/support/%s/ticket/submit?loc", "/help/ticket?loc" },
		new string[2] { "https://%s.battle.net/support/%s/ticket/status?loc", "/help/tickets?loc" }
	};

	public static volatile bool RemoveFog;

	private static readonly Dictionary<string, string> FogDensityPallets = new Dictionary<string, string>
	{
		{ "1.60.1.70205", "000014420000A142000008420000000000000042CDCCCC3D0000A0410AD7233E0000003F0AD7A33CCDCC4C3D9A99193F9A99993F8FC2F53C000000400000803F0AD7A33FA4703D3FCDCC4C3F0AD7233D0000003ECDCC4C3E0AD7233CCDCCCC3ECDCCCC3C9A99193E3333333F9A99993E8EBEAC3EFEBAA43EB95F973E36788E3E0000803E6666E63ECDCC0C3F0AD7A33D3333333E295C0F3E0000C03F295C8F3D8FC2753D00004040CDCC8C3F0000C04000000041000060406666663E000020403333B33E0000403F0000A040BCBBBB3E00008040CDCC8C3EABAAAA3E2ED8823E0000A03F000070419A99593F00002041333333406666A63FF628DC3E9A99993D8FC2F53EB81E053E14AE473EEC51383EA4703D3E022B073E0000D03F713DAA3F5555B53FC771DC3F5655E53FEC51B83D9A99493E3333433E9A99393ECDCC2C3E0000403E1111513E6666563ECDCC9C3E3333A33ECDCCAC3E3433B33E9A99B93E6666A63E5655953E0000903EE32B3E3E843A283E00008241EFEEAE3E610BB63E0000C03E5655D53EDEDDDD3E5555D53E8988C83E9A99193DBCBB3B3DEFEE6E3D8988883D388EE33CA8AAAA3C8FC2F53DDDDDDD3DE27A143E000040413BA8033E532EE53DAE47E13D6666263F7B142E3EF6285C3FC3F5283F6666E63F6EDBB63EE22B3E3F295C8F3E3333F33F9A99D93FCDCCCC3F3333D33F5C8F423E" },
		{ "1.60.1.70235", null },
		{ "1.60.1.70245", null },
		{ "1.60.1.70291", "000014420000A142000008420000000000000042CDCCCC3D0000A0410AD7233E0000003F0AD7A33CCDCC4C3D9A99193F9A99993F8FC2F53C000000400000803F0AD7A33FA4703D3FCDCC4C3F0AD7233D0000003ECDCC4C3E0AD7233CCDCCCC3ECDCCCC3C9A99193E3333333F9A99993E8EBEAC3EFEBAA43EB95F973E36788E3E0000803E6666E63ECDCC0C3F0AD7A33D3333333E295C0F3E0000C03F295C8F3D8FC2753D00004040CDCC8C3F0000C04000000041000060406666663E000020403333B33E0000403F0000A040BCBBBB3E00008040CDCC8C3EABAAAA3E2ED8823E0000A03F000070419A99593F00002041333333406666A63FF628DC3E9A99993D8FC2F53EB81E053E14AE473E8FC2F53DEC51B83DEC51383EA4703D3E022B073E0000D03F713DAA3F5555B53FC771DC3F5655E53F9A99493E3333433E9A99393ECDCC2C3E0000403E1111513E6666563ECDCC9C3E3333A33ECDCCAC3E3433B33E9A99B93E6666A63E5655953E0000903EE32B3E3E843A283E00008241EFEEAE3E610BB63E0000C03E5655D53EDEDDDD3E5555D53E8988C83E9A99193DBCBB3B3DEFEE6E3D8988883D388EE33CA8AAAA3CDDDDDD3DE27A143E000040413BA8033E532EE53DAE47E13D6666263F7B142E3EF6285C3FC3F5283F6666E63F6EDBB63EE22B3E3F295C8F3E3333F33F9A99D93FCDCCCC3F3333D33F5C8F423E5839B43CA69BC43D" },
		{ "1.60.1.70334", "=1.60.1.70291" },
		{ "1.60.1.70338", "=1.60.1.70291" },
		{ "1.60.1.70374", "=1.60.1.70291" },
		{ "1.60.1.70425", "000014420000A142000008420000000000000042CDCCCC3D0000A0410AD7233E0000003F0AD7A33CCDCC4C3D9A99193F9A99993F8FC2F53C000000400000803F0AD7A33FA4703D3FCDCC4C3F0AD7233D0000003ECDCC4C3E0AD7233CCDCCCC3ECDCCCC3C9A99193E3333333F9A99993E8EBEAC3EFEBAA43EB95F973E36788E3E0000803E6666E63ECDCC0C3F0AD7A33D3333333E295C0F3E0000C03F295C8F3D8FC2753D00004040CDCC8C3F8FC2F53D0000C04000000041000060406666663E000020403333B33E0000403F0000A040BCBBBB3E00008040CDCC8C3EABAAAA3E2ED8823E0000A03F000070419A99593F00002041333333406666A63FF628DC3E9A99993D8FC2F53EB81E053E14AE473EEC51B83DEC51383EA4703D3E022B073E0000D03F713DAA3F5555B53FC771DC3F5655E53F9A99493E3333433E9A99393ECDCC2C3E0000403E1111513E6666563ECDCC9C3E3333A33ECDCCAC3E3433B33E9A99B93E6666A63E5655953E0000903EE32B3E3E843A283E00008241EFEEAE3E610BB63E0000C03E5655D53EDEDDDD3E5555D53E8988C83E9A99193DBCBB3B3DEFEE6E3D8988883D388EE33CA8AAAA3CDDDDDD3DE27A143E000040413BA8033E532EE53DAE47E13D6666263F7B142E3EC3F5283F6666E63FF6285C3F6EDBB63EE22B3E3F295C8F3E3333F33F9A99D93FCDCCCC3F3333D33F5C8F423E5839B43CA69BC43D25491240DCB62D4000001C4066662640713D0A3E0000503F5555453F5655353FAAAA2A3F0000203F5555353FC7715C3F5655653F6666663F" },
		{ "1.60.1.70450", "=1.60.1.70425" }
	};

	private readonly string gameDir;

	private readonly string exePath;

	private readonly string logFile;

	private readonly string Portal;

	private readonly string NewKeyHex;

	private readonly string ServerName;

	private readonly string SupportHost;

	private readonly int TargetGroup;

	private volatile bool stopRequested;

	private string lastScan = "";

	private bool supportDone;

	private string clientVersion;

	private readonly List<long> fogPatched = new List<long>();

	private readonly Stopwatch fogClock = new Stopwatch();

	private bool fogMissingLogged;

	public bool StartNew;

	private static readonly object logLock = new object();

	public static int SupportHostMaxLength => "wauth\\.teledit\\.com/Danal".Length;

	public string LogFile => logFile;

	public event Action<Msg> Message;

	public event Action<PatchPhase> PhaseChanged;

	public static bool IsSupported(string version)
	{
		if (Array.IndexOf(SupportedVersions, version) < 0)
		{
			return Array.IndexOf(FeedVersions, version) >= 0;
		}
		return true;
	}

	public static string SupportedList()
	{
		List<string> list = new List<string>(SupportedVersions);
		string[] feedVersions = FeedVersions;
		foreach (string item in feedVersions)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		return string.Join(", ", list.ToArray());
	}

	private static bool UsesCertificates(string version)
	{
		string[] array = (version ?? "").Split('.');
		if (int.TryParse(array[array.Length - 1], out var result))
		{
			return result >= 70425;
		}
		return false;
	}

	private static string PrefixFor(string host)
	{
		int num = "https://wauth.teledit.com/Danal".Length - "https://".Length;
		if (host.Length <= num)
		{
			return "https://" + new string('*', num - host.Length) + host;
		}
		return "https://*" + host.Substring(host.Length - (num - 1));
	}

	private static List<string[]> SupportPatches(string host)
	{
		List<string[]> list = new List<string[]>();
		string[][] helpUrls = HelpUrls;
		foreach (string[] array in helpUrls)
		{
			list.Add(new string[2]
			{
				array[0],
				"https://" + host + array[1]
			});
		}
		string text = PrefixFor(host);
		if (text != null)
		{
			list.Add(new string[2] { "https://wauth.teledit.com/Danal", text });
		}
		return list;
	}

	private static string[] SupportRegexPatch(string host)
	{
		string text = "";
		int num = "wauth\\.teledit\\.com/Danal".Length - host.Length;
		if (num % 2 == 1)
		{
			text = "(|)";
			num -= 3;
		}
		while (num > 0)
		{
			text += "()";
			num -= 2;
		}
		return new string[2]
		{
			"^https://wauth\\.teledit\\.com/Danal([:/\\?].*|\\n|$)",
			"^https://" + host + text + "([:/\\?].*|\\n|$)"
		};
	}

	public Patcher(string gameDir, ServerEntry server)
	{
		this.gameDir = gameDir;
		Portal = server.Portal;
		NewKeyHex = server.PublicKey.ToUpperInvariant();
		TargetGroup = server.RegionGroup;
		ServerName = server.Name;
		SupportHost = server.SupportHost;
		exePath = Path.GetFullPath(Path.Combine(gameDir, "WowB.exe"));
		logFile = Path.Combine(gameDir, "Logs", "launcher.log");
	}

	public static Msg CheckGameDir(string dir, out string version)
	{
		version = null;
		if (string.IsNullOrEmpty(dir))
		{
			return new Msg(MsgKind.Warn, "dir.choose");
		}
		string text = Path.Combine(dir, "WowB.exe");
		if (!File.Exists(text))
		{
			return new Msg(MsgKind.Warn, "dir.noexe");
		}
		version = FileVersionInfo.GetVersionInfo(text).FileVersion;
		return null;
	}

	public static Msg UntestedNotice(string version)
	{
		if (!string.IsNullOrEmpty(version) && !IsSupported(version))
		{
			return new Msg(MsgKind.Warn, "dir.untested", version);
		}
		return null;
	}

	public void Stop()
	{
		stopRequested = true;
	}

	private void Log(string text)
	{
		try
		{
			lock (logLock)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(logFile));
				File.AppendAllText(logFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + text + Environment.NewLine);
			}
		}
		catch
		{
		}
	}

	private void Say(MsgKind kind, string key, params object[] args)
	{
		Msg msg = new Msg(kind, key, args);
		Log(msg.ToString("en"));
		Message?.Invoke(msg);
	}

	private void Say(Msg m)
	{
		Log(m.ToString("en"));
		Message?.Invoke(m);
	}

	private void Phase(PatchPhase p)
	{
		PhaseChanged?.Invoke(p);
	}

	public void EnsureConfig()
	{
		string text = Path.Combine(gameDir, "WTF");
		string text2 = Path.Combine(text, "ForeverLauncher.wtf");
		Directory.CreateDirectory(text);
		string text3 = Path.Combine(text, "Config.wtf");
		if (!File.Exists(text2) && File.Exists(text3))
		{
			File.Copy(text3, text2);
			Say(MsgKind.Dim, "p.wtfcreated", "ForeverLauncher.wtf");
		}
		List<string> list = (File.Exists(text2) ? File.ReadAllLines(text2).ToList() : new List<string>());
		string wanted = "SET portal \"" + Portal + "\"";
		bool flag = false;
		if (!list.Any((string l) => l.StartsWith("SET portal ")))
		{
			list.Add(wanted);
			flag = true;
		}
		else if (!list.Contains(wanted))
		{
			list = list.Select((string l) => (!l.StartsWith("SET portal ")) ? l : wanted).ToList();
			flag = true;
		}
		Tuple<List<string>, List<string>> tuple = InstalledLocales(gameDir);
		string text4 = Settings.LoadGameLang();
		bool flag2 = text4.Length > 0 && tuple.Item1.Contains(text4);
		string text5 = (flag2 ? text4 : PickLocale(L.Lang, tuple.Item1));
		string text6;
		if (flag2)
		{
			text6 = (tuple.Item2.Contains(text4) ? text4 : null);
		}
		else
		{
			text6 = PickLocale(L.Lang, tuple.Item2);
		}
		if (tuple.Item1.Count == 0)
		{
			if (!list.Any((string l) => l.StartsWith("SET textLocale ")))
			{
				list.Add("SET textLocale \"enUS\"");
				flag = true;
			}
		}
		else
		{
			if (text5 == null)
			{
				text5 = ((tuple.Item1.Contains("enUS") || tuple.Item1.Count == 0) ? "enUS" : tuple.Item1[0]);
				if (L.Lang != "en")
				{
					Say(MsgKind.Warn, "p.nolocale", LocaleCodes(L.Lang)[0]);
				}
			}
			if (text6 == null)
			{
				if (tuple.Item2.Contains(text5))
				{
					text6 = text5;
				}
				else if (tuple.Item2.Contains("enUS"))
				{
					text6 = "enUS";
				}
				else
				{
					text6 = ((tuple.Item2.Count > 0) ? tuple.Item2[0] : text5);
				}
			}
			flag |= SetLine(list, "textLocale", text5);
			flag |= SetLine(list, "audioLocale", text6);
		}
		if (!list.Any((string l) => l.StartsWith("SET scriptErrors ")))
		{
			list.Add("SET scriptErrors \"0\"");
			flag = true;
		}
		if (flag)
		{
			File.WriteAllLines(text2, list);
			Say(MsgKind.Dim, "p.wtfupdated", "ForeverLauncher.wtf", Portal);
		}
	}

	private static bool SetLine(List<string> lines, string cvar, string value)
	{
		string text = "SET " + cvar + " \"" + value + "\"";
		int num = lines.FindIndex((string l) => l.StartsWith("SET " + cvar + " "));
		if (num >= 0 && lines[num] == text)
		{
			return false;
		}
		if (num >= 0)
		{
			lines[num] = text;
		}
		else
		{
			lines.Add(text);
		}
		return true;
	}

	private static string[] LocaleCodes(string lang)
	{
		return lang switch
		{
			"es" => new string[2] { "esES", "esMX" }, 
			"ru" => new string[1] { "ruRU" }, 
			_ => new string[2] { "enUS", "enGB" }, 
		};
	}

	private static string PickLocale(string lang, List<string> installed)
	{
		string[] array = LocaleCodes(lang);
		foreach (string text in array)
		{
			if (installed.Contains(text))
			{
				return text;
			}
		}
		return null;
	}

	public static string LocaleName(string code)
	{
		return code switch
		{
			"enUS" => "English (US)", 
			"enGB" => "English (UK)", 
			"deDE" => "Deutsch", 
			"frFR" => "Français", 
			"esES" => "Español (España)", 
			"esMX" => "Español (México)", 
			"itIT" => "Italiano", 
			"ptBR" => "Português (Brasil)", 
			"ptPT" => "Português (Portugal)", 
			"ruRU" => "Русский", 
			"koKR" => "한국어", 
			"zhTW" => "繁體中文", 
			"zhCN" => "简体中文", 
			_ => code, 
		};
	}

	public static Tuple<List<string>, List<string>> InstalledLocales(string gameDir)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		try
		{
			string text = (File.Exists(Path.Combine(gameDir, ".flavor.info")) ? File.ReadAllLines(Path.Combine(gameDir, ".flavor.info")).LastOrDefault((string l) => l.Trim().Length > 0 && !l.Contains("!")) : null);
			string path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(gameDir).TrimEnd('\\')), ".build.info");
			string[] array = File.ReadAllLines(path);
			string[] array2 = array[0].Split('|');
			int num = Array.FindIndex(array2, (string h) => h.StartsWith("Tags!"));
			int num2 = Array.FindIndex(array2, (string h) => h.StartsWith("Product!"));
			foreach (string item in array.Skip(1))
			{
				string[] array3 = item.Split('|');
				if (num < 0 || num >= array3.Length || (num2 >= 0 && num2 < array3.Length && array3[num2] != (text ?? "wow_classic_beta")))
				{
					continue;
				}
				foreach (Match item2 in Regex.Matches(array3[num], "\\b([a-z]{2}[A-Z]{2}) (text|speech)\\?").Cast<Match>())
				{
					((item2.Groups[2].Value == "text") ? list : list2).Add(item2.Groups[1].Value);
				}
			}
		}
		catch
		{
		}
		return Tuple.Create(list, list2);
	}

	public void EnsureAddons()
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		foreach (string item in from n in executingAssembly.GetManifestResourceNames()
			where n.StartsWith("Addon.")
			select n)
		{
			string text = item.Substring("Addon.".Length);
			int num = text.IndexOf('.');
			if (num <= 0)
			{
				continue;
			}
			string text2 = Path.Combine(gameDir, "Interface", "AddOns", text.Substring(0, num));
			string path = Path.Combine(text2, text.Substring(num + 1));
			byte[] array;
			using (Stream stream = executingAssembly.GetManifestResourceStream(item))
			{
				using MemoryStream memoryStream = new MemoryStream();
				stream.CopyTo(memoryStream);
				array = memoryStream.ToArray();
			}
			try
			{
				if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(array))
				{
					Directory.CreateDirectory(text2);
					File.WriteAllBytes(path, array);
					Say(MsgKind.Dim, "p.addon", text.Substring(0, num));
				}
			}
			catch (Exception ex)
			{
				Log("addon " + item + ": " + ex.Message);
			}
		}
	}

	public void Run()
	{
		try
		{
			RunCore();
		}
		catch (Exception ex)
		{
			Say(MsgKind.Error, "p.error", ex.Message);
			Log("----- end (error) " + ex);
			Phase(PatchPhase.Failed);
		}
	}

	private List<Process> FindClients()
	{
		List<Process> list = new List<Process>();
		Process[] processesByName = Process.GetProcessesByName(Path.GetFileNameWithoutExtension("WowB.exe"));
		foreach (Process process in processesByName)
		{
			try
			{
				if (string.Equals(Path.GetFullPath(process.MainModule.FileName), exePath, StringComparison.OrdinalIgnoreCase))
				{
					list.Add(process);
				}
			}
			catch
			{
			}
		}
		return list;
	}

	private static bool Alive(int pid)
	{
		try
		{
			return !Process.GetProcessById(pid).HasExited;
		}
		catch
		{
			return false;
		}
	}

	private void RunCore()
	{
		Log("----- start (launcher 2.4.1, server " + ServerName + ", portal " + Portal + ", slot " + TargetGroup + ")");
		Msg msg = CheckGameDir(gameDir, out var version);
		if (msg != null)
		{
			Say(new Msg(MsgKind.Error, msg.Key, msg.Args));
			Phase(PatchPhase.Failed);
			return;
		}
		if (version != "1.60.1.70450")
		{
			Log("WARNING: build " + version + "; the key store was measured on 1.60.1.70450.");
		}
		Msg msg2 = UntestedNotice(version);
		if (msg2 != null)
		{
			Log("build " + version + " is not in this launcher's list (" + SupportedList() + "): trying it anyway");
			Say(msg2);
		}
		clientVersion = version;
		Phase(PatchPhase.Starting);
		EnsureConfig();
		EnsureAddons();
		List<Process> list = FindClients();
		if (!StartNew && list.Count > 1)
		{
			Say(MsgKind.Error, "p.multi");
			Phase(PatchPhase.Failed);
			return;
		}
		Process process;
		if (!StartNew && list.Count == 1)
		{
			process = list[0];
			Say(MsgKind.Info, "p.attached");
		}
		else
		{
			HashSet<int> before = new HashSet<int>(list.Select((Process c) => c.Id));
			Say(MsgKind.Info, StartNew ? "p.openinganother" : "p.opening");
			ProcessStartInfo processStartInfo = new ProcessStartInfo(exePath, "-config ForeverLauncher.wtf");
			processStartInfo.WorkingDirectory = gameDir;
			processStartInfo.UseShellExecute = false;
			ProcessStartInfo startInfo = processStartInfo;
			Process.Start(startInfo);
			process = null;
			for (int num = 0; num < 60; num++)
			{
				if (process != null)
				{
					break;
				}
				if (stopRequested)
				{
					break;
				}
				Thread.Sleep(1000);
				list = (from c in FindClients()
					where !before.Contains(c.Id)
					select c).ToList();
				if (list.Count < 1)
				{
					continue;
				}
				process = list.OrderBy((Process c) =>
				{
					try
					{
						return c.StartTime;
					}
					catch
					{
						return DateTime.MinValue;
					}
				}).Last();
			}
			if (process == null)
			{
				Say(MsgKind.Error, "p.nostart");
				Phase(PatchPhase.Failed);
				return;
			}
		}
		int id = process.Id;
		Log("PID " + id);
		Phase(PatchPhase.WaitingLogin);
		HashSet<long> hashSet = new HashSet<long>();
		bool flag = UsesCertificates(version);
		Log(flag ? "key mode: certificates (every copy of the 12 keys)" : ("key mode: key store (region group " + TargetGroup + ")"));
		bool everPatched = false;
		bool flag2 = false;
		bool flag3 = false;
		DateTime dateTime = DateTime.MinValue;
		HashSet<string> connections = Native.TcpConnections(id);
		DateTime dateTime2 = DateTime.MinValue;
		Stopwatch stopwatch = Stopwatch.StartNew();
		Stopwatch stopwatch2 = new Stopwatch();
		while (!stopRequested)
		{
			if (!Alive(id))
			{
				Say(MsgKind.Dim, "p.closed");
				Log("----- end (game closed)");
				Phase(PatchPhase.Closed);
				return;
			}
			HashSet<string> hashSet2 = Native.TcpConnections(id);
			bool flag4 = hashSet2.Any((string c) => !connections.Contains(c));
			connections = hashSet2;
			if (flag4)
			{
				if (!everPatched && dateTime == DateTime.MinValue)
				{
					dateTime = DateTime.UtcNow;
				}
				dateTime2 = DateTime.UtcNow.AddSeconds(15.0);
				if (everPatched)
				{
					Log("new connection: scanning for a new key store");
				}
			}
			bool flag5 = DateTime.UtcNow < dateTime2;
			bool flag6 = false;
			if (!everPatched || flag5 || stopwatch.Elapsed.TotalSeconds >= 30.0)
			{
				stopwatch.Restart();
				if (flag)
				{
					try
					{
						flag6 = CertPass(id, ref everPatched);
					}
					catch (Exception ex)
					{
						Say(MsgKind.Warn, "p.patchfail", ex.Message);
					}
				}
				else
				{
					foreach (KeyValuePair<long, StoreState> item in FindStores(id))
					{
						long key = item.Key;
						bool flag7 = hashSet.Add(key);
						if (flag7)
						{
							Log($"key store at 0x{key:X} ({item.Value}); {lastScan}");
						}
						if (item.Value == StoreState.Patched)
						{
							if (flag7 && !everPatched)
							{
								Say(MsgKind.Good, "p.already");
							}
							everPatched = true;
							continue;
						}
						Say(MsgKind.Info, everPatched ? "p.restored" : "p.found");
						try
						{
							Patch(id, key);
							flag6 = true;
							everPatched = true;
						}
						catch (Exception ex2)
						{
							Say(MsgKind.Warn, "p.patchfail", ex2.Message);
						}
					}
				}
				if (flag5 && flag6 && !flag)
				{
					dateTime2 = DateTime.UtcNow.AddSeconds(2.0);
				}
			}
			else if (flag)
			{
				try
				{
					flag6 = CertCheck(id);
				}
				catch (Exception ex3)
				{
					Say(MsgKind.Warn, "p.patchfail", ex3.Message);
				}
			}
			else
			{
				long[] array = hashSet.ToArray();
				foreach (long num3 in array)
				{
					switch (GetState(id, num3))
					{
					case StoreState.Invalid:
						hashSet.Remove(num3);
						break;
					case StoreState.Original:
						Say(MsgKind.Info, "p.restored");
						try
						{
							Patch(id, num3);
							flag6 = true;
						}
						catch (Exception ex4)
						{
							Say(MsgKind.Warn, "p.patchfail", ex4.Message);
						}
						break;
					}
				}
			}
			if (everPatched && !string.IsNullOrEmpty(SupportHost) && (!stopwatch2.IsRunning || flag4 || stopwatch2.Elapsed.TotalSeconds >= (double)(supportDone ? 60 : 5)))
			{
				stopwatch2.Restart();
				EnsureSupport(id);
			}
			if (!fogClock.IsRunning || fogClock.Elapsed.TotalSeconds >= (double)((RemoveFog && fogPatched.Count > 0) ? 30 : 5))
			{
				fogClock.Restart();
				try
				{
					EnsureFog(id);
				}
				catch (Exception ex5)
				{
					Log("fog: " + ex5.Message);
				}
			}
			if (msg2 != null && !everPatched && !flag3 && dateTime != DateTime.MinValue && (DateTime.UtcNow - dateTime).TotalSeconds >= 20.0)
			{
				Say(MsgKind.Error, "p.nostore", version);
				Log("no key store found 20 s after the first connection of untested build " + version + "; " + lastScan);
				flag3 = true;
			}
			if (flag6 || (everPatched && !flag2))
			{
				Ready();
				flag2 = true;
			}
			else if (!everPatched && !flag2)
			{
				Say(MsgKind.Dim, "p.waiting");
				Log("scan: " + lastScan);
				flag2 = true;
			}
			Thread.Sleep((!everPatched || flag5) ? 100 : 250);
		}
		Log("----- end (launcher closed)");
	}

	private void Ready()
	{
		Say(MsgKind.Good, "p.ready");
		Phase(PatchPhase.Ready);
	}

	private StoreState GetState(int pid, long array)
	{
		byte[] array2;
		try
		{
			array2 = Native.ReadHeapBlock(pid, array, 480);
		}
		catch
		{
			return StoreState.Invalid;
		}
		if (array2 == null)
		{
			return StoreState.Invalid;
		}
		for (int num = 0; num < 12; num++)
		{
			int num2 = num * 40;
			if (BitConverter.ToInt32(array2, num2) != num + 1)
			{
				return StoreState.Invalid;
			}
			string text = Hex(array2, num2 + 4, 32);
			bool num3;
			if (num != TargetGroup - 1)
			{
				num3 = text == KnownKeys[num];
			}
			else
			{
				if (text == KnownKeys[num])
				{
					goto IL_0083;
				}
				num3 = text == NewKeyHex;
			}
			if (!num3)
			{
				return StoreState.Invalid;
			}
			goto IL_0083;
			IL_0083:
			if (array2[num2 + 36] != ExpectedFlags[num])
			{
				return StoreState.Invalid;
			}
			if (array2[num2 + 37] != 127 || array2[num2 + 38] != 0 || array2[num2 + 39] != 0)
			{
				return StoreState.Invalid;
			}
		}
		string text2 = Hex(array2, (TargetGroup - 1) * 40 + 4, 32);
		if (!(text2 == NewKeyHex))
		{
			return StoreState.Original;
		}
		return StoreState.Patched;
	}

	private List<KeyValuePair<long, StoreState>> FindStores(int pid)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		long[] array = Native.Scan(pid, FromHex(KnownKeys[0]));
		List<KeyValuePair<long, StoreState>> list = new List<KeyValuePair<long, StoreState>>();
		long[] array2 = array;
		foreach (long num in array2)
		{
			long num2 = num - 4;
			StoreState state = GetState(pid, num2);
			if (state != StoreState.Invalid)
			{
				list.Add(new KeyValuePair<long, StoreState>(num2, state));
			}
		}
		lastScan = $"{Native.LastBytesScanned / 1048576:N0} MB in {Native.LastRegionsScanned} regions, {stopwatch.Elapsed.TotalSeconds:N1} s, {array.Length} copies of the anchor key, {list.Count} valid store(s)";
		return list;
	}

	private void Patch(int pid, long array)
	{
		long num = array + (TargetGroup - 1) * 40 + 4;
		int num2 = 1;
		while (true)
		{
			try
			{
				Native.Write32(pid, num, FromHex(KnownKeys[TargetGroup - 1]), FromHex(NewKeyHex));
			}
			catch (BytesChangedException)
			{
				if (num2 >= 20)
				{
					throw;
				}
				Thread.Sleep(300);
				goto IL_004f;
			}
			break;
			IL_004f:
			num2++;
		}
		if (GetState(pid, array) != StoreState.Patched)
		{
			throw new Exception("after writing, the key store does not validate as patched");
		}
		Log($"region group {TargetGroup} key written at 0x{num:X} (store 0x{array:X})");
	}

	private bool CertPass(int pid, ref bool everPatched)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		byte[][] array = new byte[KnownKeys.Length + 1][];
		for (int i = 0; i < KnownKeys.Length; i++)
		{
			array[i] = FromHex(KnownKeys[i]);
		}
		array[KnownKeys.Length] = FromHex(NewKeyHex);
		List<KeyValuePair<long, int>> list = Native.ScanAny(pid, array, 1);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (KeyValuePair<long, int> item in list)
		{
			if (item.Value == KnownKeys.Length)
			{
				num2++;
				certCopies.Add(item.Key);
				continue;
			}
			num++;
			if (WriteCertKey(pid, item.Key, item.Value))
			{
				num3++;
			}
		}
		lastScan = $"{Native.LastBytesScanned / 1048576:N0} MB in {Native.LastRegionsScanned} regions, {stopwatch.Elapsed.TotalSeconds:N1} s, {num} original key copies, {num2} of ours";
		if (num3 > 0)
		{
			if (!everPatched)
			{
				Say(MsgKind.Info, "p.found");
			}
			Log($"{num3} certificate key copies written; {lastScan}");
			everPatched = true;
			return true;
		}
		if (!everPatched && num2 > 0 && num == 0)
		{
			Say(MsgKind.Good, "p.already");
			Log("certificate keys already ours; " + lastScan);
			everPatched = true;
		}
		return false;
	}

	private bool CertCheck(int pid)
	{
		bool result = false;
		long[] array = certCopies.ToArray();
		foreach (long num in array)
		{
			byte[] array2;
			try
			{
				array2 = Native.ReadHeapBlock(pid, num, 32);
			}
			catch
			{
				array2 = null;
			}
			if (array2 == null)
			{
				certCopies.Remove(num);
				continue;
			}
			string text = Hex(array2, 0, 32);
			if (!(text == NewKeyHex))
			{
				int num2 = Array.IndexOf(KnownKeys, text);
				if (num2 < 0)
				{
					certCopies.Remove(num);
				}
				else if (WriteCertKey(pid, num, num2))
				{
					Log($"certificate key {num2 + 1} restored at 0x{num:X}: written again");
					result = true;
				}
			}
		}
		return result;
	}

	private bool WriteCertKey(int pid, long a, int key)
	{
		try
		{
			Native.WriteHeap(pid, a, FromHex(KnownKeys[key]), FromHex(NewKeyHex));
		}
		catch (BytesChangedException)
		{
			return false;
		}
		certCopies.Add(a);
		return true;
	}

	private void EnsureSupport(int pid)
	{
		int num = 0;
		List<string[]> list = SupportPatches(SupportHost);
		foreach (string[] item in list)
		{
			if (ReplaceHeapString(pid, item[0], item[1], !supportDone))
			{
				num++;
			}
		}
		if (!supportDone && num == list.Count)
		{
			supportDone = true;
			Say(MsgKind.Good, "p.support", "https://" + SupportHost);
		}
		string[] array = SupportRegexPatch(SupportHost);
		ReplaceHeapString(pid, array[0], array[1], checkAlready: false);
	}

	private bool ReplaceHeapString(int pid, string original, string replacement, bool checkAlready)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(original + "\0");
		byte[] array = new byte[bytes.Length];
		Encoding.ASCII.GetBytes(replacement, 0, replacement.Length, array, 0);
		long[] array2 = Native.Scan(pid, bytes, 1);
		long[] array3 = array2;
		for (int i = 0; i < array3.Length; i++)
		{
			long num = array3[i];
			try
			{
				Native.WriteHeap(pid, num, bytes, array);
				Log($"support: 0x{num:X} {original} -> {replacement}");
			}
			catch (Exception ex)
			{
				Log("support: 0x" + num.ToString("X") + " " + ex.Message);
			}
		}
		if (array2.Length <= 0)
		{
			if (checkAlready)
			{
				return Native.Scan(pid, Encoding.ASCII.GetBytes(replacement + "\0"), 1).Length > 0;
			}
			return false;
		}
		return true;
	}

	private void EnsureFog(int pid)
	{
		if (!FogDensityPallets.TryGetValue(clientVersion ?? "", out var value))
		{
			value = "=1.60.1.70425";
		}
		else if (value == null)
		{
			value = FogDensityPallets["1.60.1.70205"];
		}
		while (value != null && value.StartsWith("="))
		{
			value = FogDensityPallets[value.Substring(1)];
		}
		if (value == null)
		{
			if (RemoveFog && !fogMissingLogged)
			{
				Log("fog: no fog data for client build " + clientVersion + ", fog left as it is");
			}
			fogMissingLogged = true;
			return;
		}
		byte[] array = FromHex(value);
		byte[] array2 = new byte[array.Length];
		if (RemoveFog)
		{
			long[] array3 = Native.Scan(pid, array);
			foreach (long num in array3)
			{
				Native.WriteHeap(pid, num, array, array2);
				if (!fogPatched.Contains(num))
				{
					fogPatched.Add(num);
				}
				Log($"fog: density list at 0x{num:X} set to 0");
			}
			return;
		}
		long[] array4 = fogPatched.ToArray();
		for (int j = 0; j < array4.Length; j++)
		{
			long num2 = array4[j];
			try
			{
				Native.WriteHeap(pid, num2, array2, array);
				Log($"fog: density list at 0x{num2:X} restored");
			}
			catch (Exception ex)
			{
				Log("fog: 0x" + num2.ToString("X") + " not restored: " + ex.Message);
			}
			fogPatched.Remove(num2);
		}
	}

	private static string Hex(byte[] b, int offset, int count)
	{
		return BitConverter.ToString(b, offset, count).Replace("-", "");
	}

	private static byte[] FromHex(string h)
	{
		byte[] array = new byte[h.Length / 2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
		}
		return array;
	}
}
