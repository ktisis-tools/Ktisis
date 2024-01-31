using Ktisis.Editor.Posing.Ik.TwoJoints;

namespace Ktisis.Scene.Decor.Ik;

public interface ITwoJointsNode : IIkNode {
	public TwoJointsGroup Group { get; }
}
