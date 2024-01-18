using System;

namespace Ktisis.Editor.Characters.Data;

[Flags]
public enum EquipmentFlags : uint {
	None = 0,
	SetVisor = 1
}

public enum EquipmentVisible {
	None = 0,
	Hidden = 1,
	Visible = 2
}