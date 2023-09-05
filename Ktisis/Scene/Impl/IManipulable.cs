using System.Numerics;

using Ktisis.Common.Utility;

namespace Ktisis.Scene.Impl;

public interface IManipulable : ITransform {
	public Matrix4x4? ComposeMatrix()
		=> GetTransform()?.ComposeMatrix();

	public void SetMatrix(Matrix4x4 mx)
		=> SetTransform(new Transform(mx));
}
