﻿using System.Runtime.CompilerServices;

namespace nGGPO.Utils;

public static class Tracer
{
    public static void Debug(string msg, params object[] args)
    {
    }

    public static void Log(string msg, params object[] args)
    {
    }

    public static void Warn(string msg, params object[] args)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Fail(string msg) => Assert(false, msg);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Assert(bool condition, string? msg = null)
    {
        if (msg is not null)
            System.Diagnostics.Trace.Assert(condition, msg);
        else
            System.Diagnostics.Trace.Assert(condition);
    }
}