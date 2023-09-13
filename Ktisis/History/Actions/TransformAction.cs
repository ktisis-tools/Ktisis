using System.Numerics;

namespace Ktisis.History.Actions; 

public class TransformAction : ObjectActionBase {
	public TransformAction(string handlerId) : base(handlerId) {}

	public string? TargetId;

	public Matrix4x4? Initial;
	public Matrix4x4? Final;
}
