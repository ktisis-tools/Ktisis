using System;

namespace Ktisis.Editor.Camera.Types;

[Flags]
public enum CameraFlags {
	None = 0,
	DefaultCamera = 1,
	FreeCamera = 2,
	NoCollide = 4,
	Delimit = 8
}
