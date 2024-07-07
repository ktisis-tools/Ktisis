using System.Numerics;

using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;

namespace Ktisis.Structs.Poses {
	public class Transform {
		public Vector3 Position;
		public Quaternion Rotation;
		public Vector3 Scale;

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
