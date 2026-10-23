namespace ForeverLauncher;

public sealed class RealmStatus
{
	public RealmEntry Realm;

	public long Ms = -1L;

	public bool Up => Ms >= 0;
}
