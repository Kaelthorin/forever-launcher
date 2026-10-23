using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ForeverLauncher;

public static class Updater
{
	public const string Repo = "Kittnz/forever-launcher";

	public const string ExeAsset = "ForeverLauncher.exe";

	public const string SigAsset = "ForeverLauncher.exe.sig";

	public const string UpdatedArg = "--updated";

	private const string PublicKey = "<RSAKeyValue><Modulus>6c444QSfzHlAX2gIlWxz3eRk0yG6m/ckt9+uN9CCZSFf68GJK/cKjusuOfovXcYN7lsnlK8gLpUtlMEtBpx6/x2CQQ9NUr1ATvz2vSuA1xVDVJAxPb+VY4PSimU6sKJiDv2YM2OLHURX0EnH2fDv5sVy1WE4Xp/2nNmjZRZHyLXd4ztQZnV/qjhyb9feK6NZsG7B2n+xHt+PDUGDuCxhJmmw0/8veT+1OmzlolUpUhTCMaermv7m5PXaHrd6SDTwMlm6BirHA3AbaN6Nm24lGtMTQlmApb/6LSgum8rLOXit02omFRzJugfiiBeOjoyyxjK8lQZoc1PI+xuUbltXOfO973j3ySX2R69R0PmjHyoW0DINzdnWJQU9hmzNxTAdiNR2YKbUDdr6ksOoYTZ6sjESQU42qD2MQvlpuwoegHCXkxTc9cnQ+tTfYsWNsczQ6tw+3iOnx5upQ76N/kgCX1gdE6C5xsDVorkRyUwIGoV2piHpOX3mqglqT2BCUfKh</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

	public static string ExePath => Path.GetFullPath(Assembly.GetExecutingAssembly().Location);

	private static string NewPath => ExePath + ".new";

	private static string OldPath => ExePath + ".old";

	public static bool AutoUpdate
	{
		get
		{
			try
			{
				return File.ReadAllText(Path.Combine(Settings.Dir, "autoupdate.txt")).Trim() != "0";
			}
			catch
			{
				return true;
			}
		}
	}

	private static WebClient Client()
	{
		WebClient webClient = new WebClient();
		webClient.Headers[HttpRequestHeader.UserAgent] = "ForeverLauncher/2.4.1";
		return webClient;
	}

	public static LauncherRelease Latest()
	{
		// Self-update disabled: the upstream release feed would replace this build.
		return null;
		try
		{
			string input;
			using (WebClient webClient = Client())
			{
				webClient.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
				input = webClient.DownloadString("https://api.github.com/repos/Kittnz/forever-launcher/releases/latest");
			}
			Dictionary<string, object> dictionary = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(input);
			LauncherRelease launcherRelease = new LauncherRelease();
			launcherRelease.Version = ((dictionary["tag_name"] as string) ?? "").TrimStart('v', 'V');
			launcherRelease.PageUrl = (dictionary.ContainsKey("html_url") ? (dictionary["html_url"] as string) : null);
			LauncherRelease launcherRelease2 = launcherRelease;
			ArrayList arrayList = (dictionary.ContainsKey("assets") ? (dictionary["assets"] as ArrayList) : null);
			if (arrayList != null)
			{
				foreach (Dictionary<string, object> item in arrayList)
				{
					string text = item["name"] as string;
					string text2 = item["browser_download_url"] as string;
					if (text == "ForeverLauncher.exe")
					{
						launcherRelease2.ExeUrl = text2;
					}
					else if (text == "ForeverLauncher.exe.sig")
					{
						launcherRelease2.SigUrl = text2;
					}
				}
			}
			return (launcherRelease2.ExeUrl != null && launcherRelease2.SigUrl != null) ? launcherRelease2 : null;
		}
		catch
		{
			return null;
		}
	}

	public static bool IsNewer(LauncherRelease r)
	{
		if (r != null)
		{
			return StatusClient.IsNewer(r.Version, "2.4.1");
		}
		return false;
	}

	public static bool Verify(byte[] data, byte[] signature)
	{
		try
		{
			using RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.FromXmlString("<RSAKeyValue><Modulus>6c444QSfzHlAX2gIlWxz3eRk0yG6m/ckt9+uN9CCZSFf68GJK/cKjusuOfovXcYN7lsnlK8gLpUtlMEtBpx6/x2CQQ9NUr1ATvz2vSuA1xVDVJAxPb+VY4PSimU6sKJiDv2YM2OLHURX0EnH2fDv5sVy1WE4Xp/2nNmjZRZHyLXd4ztQZnV/qjhyb9feK6NZsG7B2n+xHt+PDUGDuCxhJmmw0/8veT+1OmzlolUpUhTCMaermv7m5PXaHrd6SDTwMlm6BirHA3AbaN6Nm24lGtMTQlmApb/6LSgum8rLOXit02omFRzJugfiiBeOjoyyxjK8lQZoc1PI+xuUbltXOfO973j3ySX2R69R0PmjHyoW0DINzdnWJQU9hmzNxTAdiNR2YKbUDdr6ksOoYTZ6sjESQU42qD2MQvlpuwoegHCXkxTc9cnQ+tTfYsWNsczQ6tw+3iOnx5upQ76N/kgCX1gdE6C5xsDVorkRyUwIGoV2piHpOX3mqglqT2BCUfKh</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
			return rSACryptoServiceProvider.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}
		catch
		{
			return false;
		}
	}

	public static async Task Download(LauncherRelease r, Action<int> progress)
	{
		byte[] sig;
		using (WebClient c = Client())
		{
			sig = await c.DownloadDataTaskAsync(r.SigUrl);
		}
		byte[] exe;
		using (WebClient c2 = Client())
		{
			c2.DownloadProgressChanged += (object s, DownloadProgressChangedEventArgs e) =>
			{
				progress(e.ProgressPercentage);
			};
			exe = await c2.DownloadDataTaskAsync(r.ExeUrl);
		}
		if (!Verify(exe, sig))
		{
			throw new InvalidDataException("signature check failed");
		}
		File.WriteAllBytes(NewPath, exe);
	}

	public static void InstallAndRestart(string version)
	{
		string exePath = ExePath;
		try
		{
			if (File.Exists(OldPath))
			{
				File.Delete(OldPath);
			}
		}
		catch
		{
		}
		File.Move(exePath, OldPath);
		try
		{
			File.Move(NewPath, exePath);
		}
		catch
		{
			File.Move(OldPath, exePath);
			throw;
		}
		ProcessStartInfo processStartInfo = new ProcessStartInfo(exePath, "--updated " + version);
		processStartInfo.UseShellExecute = false;
		processStartInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
		Process.Start(processStartInfo);
	}

	public static void Cleanup()
	{
		Task.Run(async () =>
		{
			for (int i = 0; i < 20; i++)
			{
				try
				{
					if (File.Exists(OldPath))
					{
						File.Delete(OldPath);
					}
					if (File.Exists(NewPath))
					{
						File.Delete(NewPath);
					}
					break;
				}
				catch
				{
				}
				await Task.Delay(500);
			}
		});
	}

	public static bool CanWriteHere()
	{
		try
		{
			string path = Path.Combine(Path.GetDirectoryName(ExePath), ".update-probe");
			File.WriteAllText(path, "");
			File.Delete(path);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
