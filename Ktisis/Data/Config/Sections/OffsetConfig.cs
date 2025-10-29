using System.Numerics;
using System.Collections.Generic;

using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Data.Config.Sections;

public class OffsetConfig {
	/*
	 * 0101 (RaceSexId): {
	 *	bone_name (BoneName): <1.0, 1.0, 1.0>,
	 *	bone_name2: <1.0, 1.0, 1.0>
	 * },
	 * 0201: { ... }
	 */
	public Dictionary<string, Dictionary<string, Vector3>> BoneOffsets = new();

	public Vector3? GetOffset(BoneNode bone) {
		if (bone.Pose.Parent is not ActorEntity actor) return null;

		var raceSexId = actor.GetRaceSexId();
		if (raceSexId is null) return null;

		if (this.BoneOffsets.TryGetValue(raceSexId, out var bones))
			if (bones.TryGetValue(bone.Info.Name, out var offset)) return offset;

		return new Vector3();
	}

	// VBO: use in ConfigWindow
	public void UpsertOffset(string raceSexId, string boneName, Vector3 offset) {
		if (!this.BoneOffsets.TryGetValue(raceSexId, out var bones))
			this.BoneOffsets.Add(raceSexId, new Dictionary<string, Vector3>() { { boneName, offset }});
		else if (!bones.TryGetValue(boneName, out _))
			this.BoneOffsets[raceSexId].Add(boneName, offset);
		else
			this.BoneOffsets[raceSexId][boneName] = offset;
	}

	// VBO: use in ConfigWindow
	public void RemoveOffset(string raceSexId, string boneName) {
		if (!this.BoneOffsets.TryGetValue(raceSexId, out _)) return;
		this.BoneOffsets[raceSexId].Remove(boneName);
	}
}
