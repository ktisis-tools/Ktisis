using System.Numerics;

using Ktisis.Common.Utility;

namespace Ktisis.Common.Extensions;

public static class MatrixEx {
	public static Matrix4x4 ApplyDelta(this Matrix4x4 target, Matrix4x4 delta, Matrix4x4? center = null) {
		var deltaT = Transform.FromMatrix(delta);
		center ??= Matrix4x4.Identity;

		return Matrix4x4.Multiply(
			Matrix4x4.Transform(target, deltaT.Rotation),
			Matrix4x4.CreateTranslation(deltaT.Position) * Matrix4x4.CreateScale(
				deltaT.Scale,
				center.Value.Translation
			)
		);
	}
}
