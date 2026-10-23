using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ForeverLauncher;

public sealed class ServerProgram
{
	private static class Console
	{
		private static readonly object gate = new object();

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AttachConsole(uint pid);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool FreeConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetConsoleCtrlHandler(IntPtr handler, bool add);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GenerateConsoleCtrlEvent(uint ctrlEvent, uint group);

		public static bool SendCtrlC(int pid)
		{
			lock (gate)
			{
				FreeConsole();
				if (!AttachConsole((uint)pid))
				{
					return false;
				}
				SetConsoleCtrlHandler(IntPtr.Zero, add: true);
				bool result = GenerateConsoleCtrlEvent(0u, 0u);
				Thread.Sleep(500);
				FreeConsole();
				SetConsoleCtrlHandler(IntPtr.Zero, add: false);
				return result;
			}
		}
	}

	public readonly string Name;

	private readonly Func<string> exe;

	private readonly Func<string> args;

	public readonly int StopTimeoutSeconds;

	private readonly Func<int> port;

	public string ExePath => exe() ?? "";

	public int Port => port();

	public bool Configured
	{
		get
		{
			if (ExePath.Length > 0)
			{
				return File.Exists(ExePath);
			}
			return false;
		}
	}

	public bool IsRunning => Running().Count > 0;

	public ServerProgram(string name, Func<string> exe, Func<string> args, int stopTimeoutSeconds, Func<int> port = null)
	{
		Name = name;
		this.exe = exe;
		this.args = args;
		StopTimeoutSeconds = stopTimeoutSeconds;
		this.port = port ?? ((Func<int>)(() => 0));
	}

	public List<Process> Running()
	{
		List<Process> list = new List<Process>();
		string exePath = ExePath;
		if (exePath.Length == 0)
		{
			return list;
		}
		if (Port > 0)
		{
			foreach (int item in TcpListeners.Pids(Port))
			{
				try
				{
					list.Add(Process.GetProcessById(item));
				}
				catch
				{
				}
			}
			return list;
		}
		string fullPath = Path.GetFullPath(exePath);
		Process[] processesByName = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath));
		foreach (Process process in processesByName)
		{
			try
			{
				if (string.Equals(Path.GetFullPath(process.MainModule.FileName), fullPath, StringComparison.OrdinalIgnoreCase))
				{
					list.Add(process);
				}
			}
			catch
			{
			}
		}
		return list;
	}

	public void Start()
	{
		if (!Configured)
		{
			throw new InvalidOperationException(Name + ": " + ExePath + " not found");
		}
		if (!IsRunning)
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(ExePath, args() ?? "");
			processStartInfo.WorkingDirectory = Path.GetDirectoryName(ExePath);
			processStartInfo.UseShellExecute = true;
			processStartInfo.WindowStyle = ProcessWindowStyle.Minimized;
			Process.Start(processStartInfo);
		}
	}

	public List<string> Stop()
	{
		List<string> list = new List<string>();
		RaClient.CloseAll();
		foreach (Process item in Running())
		{
			if (!Console.SendCtrlC(item.Id))
			{
				list.Add(L.Get("local.noconsole", Name, item.Id));
			}
			else if (!item.WaitForExit(StopTimeoutSeconds * 1000))
			{
				try
				{
					item.Kill();
					item.WaitForExit(5000);
				}
				catch
				{
				}
				list.Add(L.Get("local.killed", Name));
			}
		}
		return list;
	}
}
