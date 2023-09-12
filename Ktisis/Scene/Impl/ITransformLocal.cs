using System.Numerics;

using Ktisis.Common.Utility;

namespace Ktisis.Scene.Impl;

public interface ITransformLocal : ITransform {
	public Transform? GetLocalTransform();
	public void SetLocalTransform(Transform trans);

	public Transform? GetModelTransform();

	public Matrix4x4? GetLocalMatrix() => GetLocalTransform()?.ComposeMatrix();
	public void SetLocalMatrix(Matrix4x4 mx) => SetLocalTransform(new Transform(mx));
}
