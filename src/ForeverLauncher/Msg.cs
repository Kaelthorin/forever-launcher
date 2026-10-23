namespace ForeverLauncher;

public sealed class Msg
{
	public readonly string Key;

	public readonly object[] Args;

	public readonly MsgKind Kind;

	public Msg(MsgKind kind, string key, params object[] args)
	{
		Kind = kind;
		Key = key;
		Args = args;
	}

	public override string ToString()
	{
		return L.Get(Key, Args);
	}

	public string ToString(string lang)
	{
		return L.GetIn(lang, Key, Args);
	}
}
