using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct ActorGaze {
		[FieldOffset(0x30 + 480 * 0)] public Gaze Torso;
		[FieldOffset(0x30 + 480 * 1)] public Gaze Head;
		[FieldOffset(0x30 + 480 * 2)] public Gaze Eyes;
		[FieldOffset(0x30 + 480 * 3)] public Gaze Other; // Unused? Unsure.

		public Gaze this[GazeControl type] {
			get {
				switch (type) {
					case GazeControl.Torso:
						return Torso;
					case GazeControl.Head:
						return Head;
					case GazeControl.Eyes:
						return Eyes;
					default:
						return Other;
				}
			}
			set {
				switch (type) {
					case GazeControl.Torso:
						Torso = value;
						break;
					case GazeControl.Head:
						Head = value;
						break;
					case GazeControl.Eyes:
						Eyes = value;
						break;
					case GazeControl.All:
						Other = value;
						break;
				}
			}
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 0x1E0)]
	public struct Gaze {
		[FieldOffset(8)] public GazeMode Mode; // 0 or 3
		[FieldOffset(16)] public Vector3 Pos;
		[FieldOffset(32)] public uint Unk5;
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