using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ForeverLauncher;

public sealed class RaClient : IDisposable
{
	private TcpClient tcp;

	private NetworkStream stream;

	private readonly StringBuilder pending = new StringBuilder();

	private static readonly List<RaClient> open = new List<RaClient>();

	public bool Alive
	{
		get
		{
			if (tcp != null)
			{
				return tcp.Connected;
			}
			return false;
		}
	}

	public static void CloseAll()
	{
		RaClient[] array;
		lock (open)
		{
			array = open.ToArray();
		}
		RaClient[] array2 = array;
		foreach (RaClient raClient in array2)
		{
			raClient.Dispose();
		}
	}

	public static RaClient Connect(int port, string user, string password)
	{
		RaClient raClient = new RaClient();
		raClient.tcp = new TcpClient();
		if (!raClient.tcp.ConnectAsync("127.0.0.1", port).Wait(3000) || !raClient.tcp.Connected)
		{
			throw new Exception(L.Get("con.noconnect", port));
		}
		raClient.stream = raClient.tcp.GetStream();
		lock (open)
		{
			open.Add(raClient);
		}
		raClient.ReadUntil(10000, "Username:");
		raClient.Write(user);
		raClient.ReadUntil(10000, "Password:");
		raClient.Write(password);
		string text = raClient.ReadUntil(10000, "TC>", "Authentication failed");
		if (!text.Contains("TC>"))
		{
			raClient.Dispose();
			throw new Exception(L.Get("con.loginfailed"));
		}
		return raClient;
	}

	private void Write(string text)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(text + "\r\n");
		stream.Write(bytes, 0, bytes.Length);
	}

	private string ReadUntil(int timeoutMs, params string[] markers)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		byte[] array = new byte[4096];
		while (stopwatch.ElapsedMilliseconds < timeoutMs)
		{
			string text = pending.ToString();
			foreach (string value in markers)
			{
				if (text.Contains(value))
				{
					pending.Clear();
					return text;
				}
			}
			if (stream.DataAvailable)
			{
				int num = stream.Read(array, 0, array.Length);
				if (num <= 0)
				{
					break;
				}
				pending.Append(Encoding.UTF8.GetString(array, 0, num));
			}
			else
			{
				Thread.Sleep(30);
			}
			if (!tcp.Connected)
			{
				break;
			}
		}
		string result = pending.ToString();
		pending.Clear();
		return result;
	}

	public string Run(string command)
	{
		Write(command);
		string text = ReadUntil(15000, "TC>");
		int num = text.LastIndexOf("TC>", StringComparison.Ordinal);
		return ((num >= 0) ? text.Substring(0, num) : text).TrimEnd();
	}

	public void Dispose()
	{
		lock (open)
		{
			open.Remove(this);
		}
		try
		{
			if (stream != null)
			{
				stream.Dispose();
			}
		}
		catch
		{
		}
		try
		{
			if (tcp != null)
			{
				tcp.Close();
			}
		}
		catch
		{
		}
	}
}
