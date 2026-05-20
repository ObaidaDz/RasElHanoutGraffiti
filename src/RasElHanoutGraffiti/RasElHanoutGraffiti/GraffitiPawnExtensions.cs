using System;
using CounterStrikeSharp.API.Core;

namespace RasElHanoutGraffiti;

public static class GraffitiPawnExtensions
{
	public static bool IsAbleToApplyGraffiti(this CCSPlayerPawn pawn, nint tracePtr = 0)
	{
		return GraffitiNatives.CCSPlayerPawnIsAbleToApplySpray.Invoke(pawn.Handle, tracePtr, 0, 0) == IntPtr.Zero;
	}
}
