using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct BattleActor {
		public readonly static ReadOnlyCollection<ObjectKind> ValidKinds = new List<ObjectKind>() {ObjectKind.Pc, ObjectKind.BattleNpc}.AsReadOnly();

		[FieldOffset(0)] public Actor Actor;

		[FieldOffset(0x1B40)] public StatusEffects StatusEffects;
	}
}
