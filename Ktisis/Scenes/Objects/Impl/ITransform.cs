using Ktisis.Common.Utility;

namespace Ktisis.Scenes.Objects.Impl;

public interface ITransform {
	public Transform? GetTransform();
	public void SetTransform(Transform trans);
}
