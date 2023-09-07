using System.Numerics;

using Ktisis.Common.Utility;

namespace Ktisis.Scene.Impl;

public interface ITransform {
	public Transform? GetTransform();
	public void SetTransform(Transform trans);

	public Matrix4x4? GetMatrix() => GetTransform()?.ComposeMatrix();
	public void SetMatrix(Matrix4x4 mx) => SetTransform(new Transform(mx));
}
