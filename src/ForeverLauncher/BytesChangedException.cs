using System;

namespace ForeverLauncher;

internal sealed class BytesChangedException : Exception
{
	public BytesChangedException()
		: base("the bytes changed right before writing")
	{
	}
}
