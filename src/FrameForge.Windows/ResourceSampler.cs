using System.Runtime.InteropServices;

namespace FrameForge.Windows;

public sealed record ResourceSample(DateTimeOffset Time, double? CpuPercent, long TotalMemory, long AvailableMemory)
{
    public double UsedMemoryPercent => TotalMemory > 0 ? 100d * (TotalMemory - AvailableMemory) / TotalMemory : 0;
}

/// <summary>Read-only native counters. No WMI, performance-counter setup, or persistent worker.</summary>
public sealed class ResourceSampler
{
    ulong lastIdle, lastKernel, lastUser;
    bool initialized;

    public ResourceSample Read()
    {
        double? cpu = null;
        if (GetSystemTimes(out var idle, out var kernel, out var user))
        {
            if (initialized && kernel >= lastKernel && user >= lastUser && idle >= lastIdle)
            {
                var total = (kernel - lastKernel) + (user - lastUser);
                var idleDelta = idle - lastIdle;
                if (total > 0) cpu = Math.Clamp(100d * (total - Math.Min(total, idleDelta)) / total, 0, 100);
            }
            lastIdle = idle; lastKernel = kernel; lastUser = user; initialized = true;
        }
        var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        if (!GlobalMemoryStatusEx(ref memory)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return new(DateTimeOffset.UtcNow, cpu, (long)memory.TotalPhysical, (long)memory.AvailablePhysical);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetSystemTimes(out ulong idle, out ulong kernel, out ulong user);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalMemoryStatusEx(ref MemoryStatus memory);
    [StructLayout(LayoutKind.Sequential)]
    struct MemoryStatus
    {
        public uint Length, Load;
        public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile, TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
    }
}
