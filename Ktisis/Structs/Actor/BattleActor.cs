using FFXIVClientStructs.FFXIV.Client.Game.Object;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor
{
    [StructLayout(LayoutKind.Explicit, Size = 0x84A)]
    public struct BattleActor
    {
        public readonly static ReadOnlyCollection<ObjectKind> ValidKinds = new List<ObjectKind>() { ObjectKind.Pc, ObjectKind.BattleNpc }.AsReadOnly();

        [FieldOffset(0)]
        public Actor Actor;

        [FieldOffset(0x1B40)]
        public StatusEffects StatusEffects;
    }
}
