using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ForeverLauncher;

internal static class Native
{
	private struct Region
	{
		public IntPtr BaseAddress;

		public IntPtr AllocationBase;

		public uint AllocationProtect;

		public UIntPtr RegionSize;

		public uint State;

		public uint Protect;

		public uint Type;
	}

	private const uint MEM_COMMIT = 4096u;

	private const uint MEM_PRIVATE = 131072u;

	private const uint PAGE_READWRITE = 4u;

	private const uint PROCESS_QUERY_INFORMATION = 1024u;

	private const uint PROCESS_VM_READ = 16u;

	private const uint PROCESS_VM_WRITE = 32u;

	private const uint PROCESS_VM_OPERATION = 8u;

	public static long LastBytesScanned;

	public static int LastRegionsScanned;

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool ReadProcessMemory(IntPtr p, IntPtr a, byte[] d, UIntPtr n, out UIntPtr r);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool WriteProcessMemory(IntPtr p, IntPtr a, byte[] d, UIntPtr n, out UIntPtr w);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern UIntPtr VirtualQueryEx(IntPtr p, IntPtr a, out Region r, UIntPtr n);

	[DllImport("kernel32.dll")]
	private static extern bool CloseHandle(IntPtr p);

	[DllImport("iphlpapi.dll", SetLastError = true)]
	private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool sort, int ipVersion, int tableClass, uint reserved);

	public static HashSet<string> TcpConnections(int pid)
	{
		HashSet<string> hashSet = new HashSet<string>();
		int size = 0;
		GetExtendedTcpTable(IntPtr.Zero, ref size, sort: false, 2, 4, 0u);
		if (size <= 0)
		{
			return hashSet;
		}
		IntPtr intPtr = Marshal.AllocHGlobal(size);
		try
		{
			if (GetExtendedTcpTable(intPtr, ref size, sort: false, 2, 4, 0u) != 0)
			{
				return hashSet;
			}
			int num = Marshal.ReadInt32(intPtr);
			for (int i = 0; i < num; i++)
			{
				IntPtr ptr = intPtr + 4 + i * 24;
				if (Marshal.ReadInt32(ptr, 20) == pid)
				{
					uint num2 = (uint)Marshal.ReadInt32(ptr, 12);
					int num3 = (Marshal.ReadByte(ptr, 16) << 8) | Marshal.ReadByte(ptr, 17);
					int num4 = (Marshal.ReadByte(ptr, 8) << 8) | Marshal.ReadByte(ptr, 9);
					hashSet.Add($"{num2:X8}:{num3}/{num4}");
				}
			}
			return hashSet;
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
	}

	private static IntPtr Open(int pid, bool forWrite)
	{
		uint access = (uint)(0x410 | (forWrite ? 40 : 0));
		IntPtr intPtr = OpenProcess(access, inherit: false, pid);
		if (intPtr == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcess");
		}
		return intPtr;
	}

	private static Region Query(IntPtr p, long a)
	{
		if (VirtualQueryEx(p, new IntPtr(a), out var r, new UIntPtr((uint)Marshal.SizeOf(typeof(Region)))) == UIntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "VirtualQueryEx");
		}
		return r;
	}

	private static bool IsHeap(Region r)
	{
		if (r.State == 4096 && r.Type == 131072)
		{
			return r.Protect == 4;
		}
		return false;
	}

	private static byte[] Read(IntPtr p, long a, int n)
	{
		byte[] array = new byte[n];
		if (!ReadProcessMemory(p, new IntPtr(a), array, new UIntPtr((uint)n), out var r) || r.ToUInt64() != (ulong)n)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "ReadProcessMemory @0x" + a.ToString("X"));
		}
		return array;
	}

	public static long[] Scan(int pid, byte[] needle, int step = 4)
	{
		return (from h in ScanAny(pid, new byte[1][] { needle }, step)
			select h.Key).ToArray();
	}

	public static List<KeyValuePair<long, int>> ScanAny(int pid, byte[][] needles, int step)
	{
		int num = needles.Max((byte[] nd) => nd.Length);
		if (needles.Any((byte[] nd) => nd.Length < 8))
		{
			throw new ArgumentException("needle shorter than 8 bytes");
		}
		Dictionary<uint, List<int[]>> probes = new Dictionary<uint, List<int[]>>();
		bool[] filter = new bool[65536];
		for (int num2 = 0; num2 < needles.Length; num2++)
		{
			for (int num3 = 0; num3 < 4; num3 += step)
			{
				uint num4 = BitConverter.ToUInt32(needles[num2], num3);
				if (!probes.TryGetValue(num4, out var value))
				{
					value = (probes[num4] = new List<int[]>());
				}
				value.Add(new int[2] { num2, num3 });
				filter[num4 & 0xFFFF] = true;
			}
		}
		int overlap = (num + 3) / 4 * 4;
		IntPtr p = Open(pid, forWrite: false);
		try
		{
			List<long[]> list2 = new List<long[]>();
			long num5 = 0L;
			Region r;
			while (num5 < 140737488289792L && !(VirtualQueryEx(p, new IntPtr(num5), out r, new UIntPtr((uint)Marshal.SizeOf(typeof(Region)))) == UIntPtr.Zero))
			{
				long num6 = r.BaseAddress.ToInt64();
				long num7 = (long)r.RegionSize.ToUInt64();
				if (num7 <= 0)
				{
					break;
				}
				if (IsHeap(r))
				{
					list2.Add(new long[2] { num6, num7 });
				}
				if (num6 + num7 <= num5)
				{
					break;
				}
				num5 = num6 + num7;
			}
			List<KeyValuePair<long, int>> hits = new List<KeyValuePair<long, int>>();
			long total = 0L;
			Parallel.ForEach(list2, () => new byte[4194304], (long[] reg, ParallelLoopState state, byte[] buf) =>
			{
				long num8 = reg[0];
				long num9 = reg[1];
				long num10 = 0L;
				long num11 = 0L;
				for (; num10 < num9; num10 += 4194304 - overlap)
				{
					int num12 = (int)Math.Min(4194304L, num9 - num10);
					if (ReadProcessMemory(p, new IntPtr(num8 + num10), buf, new UIntPtr((uint)num12), out var r2) && r2.ToUInt64() != 0)
					{
						int num13 = (int)r2.ToUInt64();
						num11 += num13;
						int num14 = ((num12 < 4194304) ? num13 : (4194304 - overlap));
						for (int i = 0; i + 4 <= num13; i += 4)
						{
							if (filter[buf[i] | (buf[i + 1] << 8)] && probes.TryGetValue(BitConverter.ToUInt32(buf, i), out var value2))
							{
								foreach (int[] item in value2)
								{
									byte[] array = needles[item[0]];
									int num15 = i - item[1];
									if (num15 >= 0 && num15 < num14 && num15 + array.Length <= num13)
									{
										int j;
										for (j = 0; j < array.Length && buf[num15 + j] == array[j]; j++)
										{
										}
										if (j == array.Length)
										{
											lock (hits)
											{
												hits.Add(new KeyValuePair<long, int>(num8 + num10 + num15, item[0]));
											}
										}
									}
								}
							}
						}
					}
					if (num12 < 4194304)
					{
						break;
					}
				}
				Interlocked.Add(ref total, num11);
				return buf;
			}, (byte[] buf) =>
			{
			});
			LastBytesScanned = total;
			LastRegionsScanned = list2.Count;
			hits.Sort((KeyValuePair<long, int> x, KeyValuePair<long, int> y) => x.Key.CompareTo(y.Key));
			return hits;
		}
		finally
		{
			CloseHandle(p);
		}
	}

	public static byte[] ReadHeapBlock(int pid, long a, int n)
	{
		IntPtr p = Open(pid, forWrite: false);
		try
		{
			Region r = Query(p, a);
			long num = r.BaseAddress.ToInt64() + (long)r.RegionSize.ToUInt64();
			if (!IsHeap(r) || a < r.BaseAddress.ToInt64() || a + n > num)
			{
				return null;
			}
			return Read(p, a, n);
		}
		finally
		{
			CloseHandle(p);
		}
	}

	public static void Write32(int pid, long a, byte[] before, byte[] after)
	{
		WriteHeap(pid, a, before, after);
	}

	public static void WriteHeap(int pid, long a, byte[] before, byte[] after)
	{
		int num = after.Length;
		if (before.Length != num)
		{
			throw new ArgumentException("before/after length");
		}
		IntPtr p = Open(pid, forWrite: true);
		try
		{
			Region r = Query(p, a);
			long num2 = r.BaseAddress.ToInt64() + (long)r.RegionSize.ToUInt64();
			if (!IsHeap(r) || a + num > num2)
			{
				throw new Exception("ABORT: not in MEM_PRIVATE+PAGE_READWRITE heap");
			}
			byte[] array = Read(p, a, num);
			for (int i = 0; i < num; i++)
			{
				if (array[i] != before[i])
				{
					throw new BytesChangedException();
				}
			}
			if (!WriteProcessMemory(p, new IntPtr(a), after, new UIntPtr((uint)num), out var w) || w.ToUInt64() != (ulong)num)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "WriteProcessMemory");
			}
			byte[] array2 = Read(p, a, num);
			for (int j = 0; j < num; j++)
			{
				if (array2[j] != after[j])
				{
					throw new Exception("read-back verification failed");
				}
			}
		}
		finally
		{
			CloseHandle(p);
		}
	}
}
