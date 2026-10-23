using System.Collections.Generic;

namespace ForeverLauncher;

public sealed class ServerStatus
{
	public bool LoginUp;

	public bool WorldUp;

	public List<RealmStatus> Realms = new List<RealmStatus>();

	public long LoginMs = -1L;

	public bool FeedOk;

	public bool FeedFresh;

	public bool FeedLoginUp;

	public bool FeedWorldUp;

	public bool Maintenance;

	public FeedEntry Root = new FeedEntry(null);

	public string LatestLauncher;

	public string LauncherUrl;

	public List<FeedEntry> News = new List<FeedEntry>();

	public List<FeedEntry> Links = new List<FeedEntry>();

	public List<string> ClientBuilds = new List<string>();
}
