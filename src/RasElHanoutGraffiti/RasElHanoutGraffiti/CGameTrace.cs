using System.Numerics;
using System.Runtime.InteropServices;

namespace RasElHanoutGraffiti;

[StructLayout(LayoutKind.Explicit, Size = 184)]
public struct CGameTrace
{
	[FieldOffset(0)]
	public unsafe void* Surface;

	[FieldOffset(8)]
	public unsafe void* HitEntity;

	[FieldOffset(16)]
	public unsafe TraceHitboxData* HitboxData;

	[FieldOffset(80)]
	public uint Contents;

	[FieldOffset(120)]
	public Vector3 StartPos;

	[FieldOffset(132)]
	public Vector3 EndPos;

	[FieldOffset(144)]
	public Vector3 HitNormal;

	[FieldOffset(156)]
	public Vector3 Position;

	[FieldOffset(172)]
	public float Fraction;

	[FieldOffset(182)]
	public bool AllSolid;
}
