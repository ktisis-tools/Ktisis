using System.Numerics;

using Ktisis.Common.Utility;

namespace Ktisis.Editor.Strategy.Types;

public interface ITransform {
	public Transform? GetTransform();
	public void SetTransform(Transform trans);

	public Matrix4x4? GetMatrix() => this.GetTransform()?.ComposeMatrix();
	public void SetMatrix(Matrix4x4 mx) => this.SetTransform(new Transform(mx));
}
