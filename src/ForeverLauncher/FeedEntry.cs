using System.Collections.Generic;

namespace ForeverLauncher;

public sealed class FeedEntry
{
	private readonly Dictionary<string, object> d;

	public FeedEntry(Dictionary<string, object> d)
	{
		this.d = d ?? new Dictionary<string, object>();
	}

	public string Get(string key)
	{
		if (d.TryGetValue(key + "_" + L.Lang, out var value) && value != null && value.ToString().Length > 0)
		{
			return value.ToString();
		}
		if (!d.TryGetValue(key, out value) || value == null)
		{
			return null;
		}
		return value.ToString();
	}
}
