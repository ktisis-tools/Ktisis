using Ktisis.Common.Utility;

namespace Ktisis.Interface.SceneUi.Logic; 

public interface ITransform {
	public Transform? GetTransform();
	public void SetTransform(Transform trans);
}