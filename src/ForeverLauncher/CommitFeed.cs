using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ForeverLauncher;

public static class CommitFeed
{
	public const string Repo = "advocaite/TrinityCore";

	public const string Branch = "forever";

	private const int Count = 10;

	private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(30.0);

	private static List<CommitInfo> cached;

	private static DateTime fetched = DateTime.MinValue;

	private static Task<List<CommitInfo>> pending;

	public static string BranchUrl => "https://github.com/advocaite/TrinityCore/commits/forever";

	public static Task<List<CommitInfo>> GetAsync()
	{
		if (cached != null && DateTime.UtcNow - fetched < CacheTime)
		{
			return Task.FromResult(cached);
		}
		if (pending == null)
		{
			pending = FetchAsync();
		}
		return pending;
	}

	private static async Task<List<CommitInfo>> FetchAsync()
	{
		int num = default;
		_ = num;
		_ = 0;
		try
		{
			_ = num;
			_ = 0;
			try
			{
				string json;
				using (WebClient wc = new WebClient())
				{
					wc.Encoding = Encoding.UTF8;
					wc.Headers[HttpRequestHeader.UserAgent] = "ForeverLauncher/2.4.2";
					wc.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
					json = await wc.DownloadStringTaskAsync("https://api.github.com/repos/advocaite/TrinityCore/commits?sha=forever&per_page=" + 10);
				}
				List<CommitInfo> list = new List<CommitInfo>();
				if (new JavaScriptSerializer
				{
					MaxJsonLength = 8388608
				}.DeserializeObject(json) is object[] items)
				{
					object[] array = items;
					foreach (object obj in array)
					{
						Dictionary<string, object> dictionary = obj as Dictionary<string, object>;
						Dictionary<string, object> dictionary2 = ((dictionary != null && dictionary.ContainsKey("commit")) ? (dictionary["commit"] as Dictionary<string, object>) : null);
						if (dictionary2 != null)
						{
							Dictionary<string, object> dictionary3 = (dictionary2.ContainsKey("committer") ? (dictionary2["committer"] as Dictionary<string, object>) : null);
							string message = (dictionary2.ContainsKey("message") ? ((dictionary2["message"] as string) ?? "") : "");
							CommitInfo commitInfo = new CommitInfo();
							commitInfo.Url = (dictionary.ContainsKey("html_url") ? (dictionary["html_url"] as string) : null);
							CommitInfo commitInfo2 = commitInfo;
							if (dictionary3 != null && dictionary3.ContainsKey("date") && DateTime.TryParse(dictionary3["date"] as string, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var result))
							{
								commitInfo2.Date = result.ToLocalTime();
							}
							Split(message, out commitInfo2.Title, out commitInfo2.Text);
							list.Add(commitInfo2);
						}
					}
				}
				cached = list;
				fetched = DateTime.UtcNow;
				return list;
			}
			catch
			{
				return cached;
			}
		}
		finally
		{
			pending = null;
		}
	}

	private static void Split(string message, out string title, out string text)
	{
		string[] array = message.Replace("\r", "").Split('\n');
		title = array[0].Trim();
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		for (int i = 1; i < array.Length; i++)
		{
			if (num >= 4)
			{
				break;
			}
			string text2 = array[i].Trim();
			if (text2.Length != 0)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append('\n');
				}
				stringBuilder.Append(text2);
				num++;
			}
		}
		text = ((stringBuilder.Length > 320) ? (stringBuilder.ToString(0, 317) + "...") : stringBuilder.ToString());
	}
}
