using System.Numerics;

using Ktisis.Actions.Types;
using Ktisis.Common.Utility;

namespace Ktisis.Editor.Transforms.Types;

public interface ITransformMemento : IMemento {
	public ITransformMemento Save();
	
	public void SetTransform(Transform transform);
	public void SetMatrix(Matrix4x4 matrix);
	
	public void Dispatch();
}
