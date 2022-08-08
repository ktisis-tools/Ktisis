using System.Numerics;
using System.Collections;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Structs.Havok;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit, Size = 0x2B4)]
	public unsafe struct ActorModel {
		[FieldOffset(0)] public Object Object;

		[FieldOffset(0x050)] public Vector3 Position;
		[FieldOffset(0x060)] public Quaternion Rotation;
		[FieldOffset(0x070)] public Vector3 Scale;

		[FieldOffset(0x26C)] public float Height;

		[FieldOffset(0x0A0)] public HkaIndexVec* HkaIndex;
	}

	[StructLayout(LayoutKind.Explicit, Size = 0x70)]
	public unsafe struct HkaIndexVec : IEnumerable {
		[FieldOffset(0x50)] public short Count;
		[FieldOffset(0x68)] public HkaIndex* Handle;

		public HkaIndex this[int index] {
			get => Handle[index];
			set => Handle[index] = value;
		}

		public IEnumerator GetEnumerator() {
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}
	}
}