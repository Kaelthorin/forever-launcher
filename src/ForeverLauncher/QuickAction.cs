using System;

namespace ForeverLauncher;

internal sealed class QuickAction
{
	public string Key;

	public string[] Fields;

	public string[] Defaults;

	public Func<string[], string> Command;

	public bool Confirm;
}
