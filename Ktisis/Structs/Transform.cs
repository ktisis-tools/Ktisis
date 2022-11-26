using System.Numerics;

using FFXIVClientStructs.Havok;

namespace Ktisis.Structs {
	public class Transform {
		Vector3 Position;
		Quaternion Rotation;
		Vector3 Scale;

		public static Transform FromHavok(hkQsTransformf t) => new Transform {
			Position = t.Translation.ToVector3(),
			Rotation = t.Rotation.ToQuat(),
			Scale = t.Scale.ToVector3()
		};

		public hkQsTransformf ToHavok() => new hkQsTransformf {
			Translation = Position.ToHavok(),
			Rotation = Rotation.ToHavok(),
			Scale = Scale.ToHavok()
		};
	}
}