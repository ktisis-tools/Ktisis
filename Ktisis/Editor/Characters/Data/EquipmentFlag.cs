using System;

namespace Ktisis.Editor.Characters.Data;

[Flags]
public enum EquipmentFlag : uint {
	None = 0,
	SetVisor = 1,
	SetHatVisible = 2,
	SetMainHandVisible = 4,
	SetOffHandVisible = 8
}
