using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace RasElHanoutGraffiti;

public static class GraffitiNatives
{
	public static readonly MemoryFunctionWithReturn<nint, nint, nint, nint, nint> CCSPlayerPawnIsAbleToApplySpray = new MemoryFunctionWithReturn<nint, nint, nint, nint, nint>(GameData.GetSignature("CCSPlayerPawn::IsAbleToApplySpray"));
}
