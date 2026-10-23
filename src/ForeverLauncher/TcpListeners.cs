using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ForeverLauncher;

internal static class TcpListeners
{
	private const int AF_INET = 2;

	private const int TCP_TABLE_OWNER_PID_LISTENER = 3;

	[DllImport("iphlpapi.dll", SetLastError = true)]
	private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool order, int af, int tableClass, uint reserved);

	public static List<int> Pids(int port)
	{
		List<int> list = new List<int>();
		int size = 0;
		GetExtendedTcpTable(IntPtr.Zero, ref size, order: false, 2, 3, 0u);
		IntPtr intPtr = Marshal.AllocHGlobal(size);
		try
		{
			if (GetExtendedTcpTable(intPtr, ref size, order: false, 2, 3, 0u) != 0)
			{
				return list;
			}
			int num = Marshal.ReadInt32(intPtr);
			for (int i = 0; i < num; i++)
			{
				IntPtr ptr = IntPtr.Add(intPtr, 4 + i * 24);
				int num2 = Marshal.ReadInt32(ptr, 8);
				int num3 = ((num2 & 0xFF) << 8) | ((num2 >> 8) & 0xFF);
				int item = Marshal.ReadInt32(ptr, 20);
				if (num3 == port && !list.Contains(item))
				{
					list.Add(item);
				}
			}
			return list;
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
	}
}
