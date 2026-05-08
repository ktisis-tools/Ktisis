using System.Numerics;

using Ktisis.Common.Utility;

namespace Ktisis.Scene.Decor;

public interface ITransform {
	public Transform? GetTransform();
	public void SetTransform(Transform trans);

	public Matrix4x4? GetMatrix() => this.GetTransform()?.ComposeMatrix();
	public void SetMatrix(Matrix4x4 mx) {
		if (this.GetTransform() is { } transform)
			this.SetTransform(new Transform(mx, transform));
		else
			this.SetTransform(new Transform(mx));
	}
}
