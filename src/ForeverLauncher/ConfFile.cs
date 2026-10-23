using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ForeverLauncher;

public static class ConfFile
{
	public static string Get(string path, string key, string fallback)
	{
		try
		{
			Regex regex = new Regex("^\\s*" + Regex.Escape(key) + "\\s*=\\s*(.*?)\\s*$");
			string[] array = File.ReadAllLines(path);
			foreach (string input in array)
			{
				Match match = regex.Match(input);
				if (match.Success)
				{
					return match.Groups[1].Value.Trim('"');
				}
			}
		}
		catch
		{
		}
		return fallback;
	}

	public static void Set(string path, IDictionary<string, string> values)
	{
		List<string> list = File.ReadAllLines(path).ToList();
		foreach (KeyValuePair<string, string> value in values)
		{
			Regex pattern = new Regex("^\\s*" + Regex.Escape(value.Key) + "\\s*=");
			int num = list.FindIndex((string l) => pattern.IsMatch(l));
			string text = value.Key + " = " + value.Value;
			if (num >= 0)
			{
				list[num] = text;
			}
			else
			{
				list.Add(text);
			}
		}
		File.WriteAllLines(path, list.ToArray(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}
}
