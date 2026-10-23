using System;
using System.IO;
using System.Text;

namespace ForeverLauncher;

internal sealed class LogTail
{
	private readonly string path;

	private long position = -1L;

	public string Path => path;

	public LogTail(string path)
	{
		this.path = path;
	}

	public string ReadNew(int firstReadBytes)
	{
		try
		{
			if (!File.Exists(path))
			{
				position = 0L;
				return "";
			}
			using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			if (position < 0)
			{
				position = Math.Max(0L, fileStream.Length - firstReadBytes);
			}
			if (fileStream.Length < position)
			{
				position = 0L;
			}
			if (fileStream.Length == position)
			{
				return "";
			}
			fileStream.Position = position;
			byte[] array = new byte[Math.Min(fileStream.Length - position, 4194304L)];
			int num = fileStream.Read(array, 0, array.Length);
			position += num;
			return Encoding.UTF8.GetString(array, 0, num);
		}
		catch
		{
			return "";
		}
	}
}
