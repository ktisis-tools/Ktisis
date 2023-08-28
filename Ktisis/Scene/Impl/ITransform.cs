using Ktisis.Common.Utility;

namespace Ktisis.Scene.Impl;

public interface ITransform {
	public Transform? GetTransform();
	public void SetTransform(Transform trans);
}
