using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Sequential)]
	public struct ActorGaze {
		public GazeContainer Torso;
		public GazeContainer Head;
		public GazeContainer Eyes;
		public GazeContainer Other; // Unused? Unsure.

		public Gaze this[GazeControl type] {
			get {
				switch (type) {
					case GazeControl.Torso:
						return Torso.Gaze;
					case GazeControl.Head:
						return Head.Gaze;
					case GazeControl.Eyes:
						return Eyes.Gaze;
					default:
						return Other.Gaze;
				}
			}
			set {
				switch (type) {
					case GazeControl.Torso:
						Torso.Gaze = value;
						break;
					case GazeControl.Head:
						Head.Gaze = value;
						break;
					case GazeControl.Eyes:
						Eyes.Gaze = value;
						break;
					case GazeControl.All:
						Other.Gaze = value;
						break;
				}
			}
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 0x28)]
	public struct Gaze {
		[FieldOffset(0x08)] public GazeMode Mode; // 0 or 3
		[FieldOffset(0x10)] public Vector3 Pos;
		[FieldOffset(0x20)] public uint Unk5;
	}

	[StructLayout(LayoutKind.Explicit, Size = 0x1E0)]
	public struct GazeContainer {
		[FieldOffset(0x30)] public Gaze Gaze;
	}

	public enum GazeControl {
		All = -1,
		Torso = 0,
		Head = 1,
		Eyes = 2
	}

	public enum GazeMode : uint {
		Disabled = 0,
		Freeze = 1,
		Rotate = 2,
		Target = 3,

		_KtisisFollowCam_ = 9
	}
}