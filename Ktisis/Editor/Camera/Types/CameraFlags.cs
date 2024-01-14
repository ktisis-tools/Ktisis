using System;

namespace Ktisis.Editor.Camera.Types;

[Flags]
public enum CameraFlags {
	None = 0,
	DefaultCamera = 1,
	NoCollide = 2,
	Delimit = 4,
	Orthographic = 8
}
