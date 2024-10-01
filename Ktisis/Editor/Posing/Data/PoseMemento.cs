using System.Collections.Generic;

using Ktisis.Actions.Types;
using Ktisis.Editor.Posing.Types;

namespace Ktisis.Editor.Posing.Data;

public class PoseMemento(EntityPoseConverter converter) : IMemento {
	public required PoseMode Modes { get; init; }
	public required PoseTransforms Transforms { get; init; }
	public required List<PartialBoneInfo>? Bones { get; init; }
	public required PoseContainer Initial { get; init; }
	public required PoseContainer Final { get; init; }
		
	public void Restore() => this.Apply(this.Initial);
		
	public void Apply() => this.Apply(this.Final);

	private void Apply(PoseContainer pose) {
		if (!converter.IsPoseValid) return;
		if (this.Bones != null) {
			var bones = converter.IntersectBonesByName(this.Bones);
			converter.LoadBones(pose, bones, this.Transforms);
		} else {
			converter.Load(pose, this.Modes, this.Transforms);
		}
	}
}
