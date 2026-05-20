using System.Runtime.InteropServices;

namespace RasElHanoutGraffiti;

[StructLayout(LayoutKind.Explicit, Size = 68)]
public struct TraceHitboxData
{
	[FieldOffset(56)]
	public int HitGroup;

	[FieldOffset(64)]
	public int HitboxId;
}
