using Ktisis.Editor.Posing.Ik.Types;
using Ktisis.Scene.Decor.Ik;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton.Constraints;

public abstract class IkNodeGroupBase : BoneNodeGroup, IIkNode {
	public readonly IIkGroup Group;

	protected IkNodeGroupBase(
		ISceneManager scene,
		EntityPose pose,
		IIkGroup group
	) : base(scene, pose) {
		this.Group = group;
	}

	public bool IsEnabled => this.Group.IsEnabled;

	public virtual void Enable() => this.Group.IsEnabled = true;
	public virtual void Disable() => this.Group.IsEnabled = false;
}

public class IkNodeGroup<T> : IkNodeGroupBase where T : IIkGroup {
	public readonly T Group;
	
	public IkNodeGroup(
		ISceneManager scene,
		EntityPose pose,
		T group
	) : base(scene, pose, group) {
		this.Group = group;
	}
}
