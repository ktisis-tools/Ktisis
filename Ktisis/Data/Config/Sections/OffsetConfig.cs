using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;

using Dalamud.Bindings.ImGui;

using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

using Newtonsoft.Json;

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

	public void UpsertOffset(string raceSexId, string boneName, Vector3 offset) {
		if (!this.BoneOffsets.TryGetValue(raceSexId, out var bones))
			this.BoneOffsets.Add(raceSexId, new Dictionary<string, Vector3>() { { boneName, offset }});
		else if (!bones.TryGetValue(boneName, out _))
			this.BoneOffsets[raceSexId].Add(boneName, offset);
		else
			this.BoneOffsets[raceSexId][boneName] = offset;
	}

	public void RemoveOffset(string raceSexId, string boneName) {
		if (!this.BoneOffsets.TryGetValue(raceSexId, out _)) return;
		this.BoneOffsets[raceSexId].Remove(boneName);
	}

	public void RemoveOffsetsForId(string raceSexId) {
		if (!this.BoneOffsets.TryGetValue(raceSexId, out _)) return;
		this.BoneOffsets[raceSexId] = new Dictionary<string, Vector3>();
	}

	public bool SaveToClipboard() {
		try {
			ImGui.SetClipboardText(Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this.BoneOffsets))));
			return true;
		} catch (Exception e) {
			Ktisis.Log.Error($"Could not serialize offsets to clipboard: {e}");
			return false;
		}
	}

	public bool LoadFromClipboard() {
		try {
			this.BoneOffsets = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Vector3>>>(Encoding.UTF8.GetString(Convert.FromBase64String(ImGui.GetClipboardText())))!;
			return true;
		} catch (Exception e) {
			Ktisis.Log.Error($"Could not deserialize offsets from clipboard: {e}");
			return false;
		}
	}

	// todo: remove with v0.3 release
	public bool LoadLegacyFromClipboard(string? raceSexId) {
		if (raceSexId is null) return false;

		try {
			var offsets = JsonConvert.DeserializeObject<Dictionary<string, Vector3>>(Encoding.UTF8.GetString(Convert.FromBase64String(ImGui.GetClipboardText())));
			if (offsets is null) return false;
			// modify translation for legacy conversion??????
			foreach (var key in offsets.Keys)
				offsets[key] = new Vector3(offsets[key].X, offsets[key].Y, offsets[key].Z);
			this.BoneOffsets[raceSexId] = offsets;
			return true;
		} catch (Exception e) {
			Ktisis.Log.Error($"Could not deserialize legacy offsets from clipboard: {e}");
			return false;
		}
	}
}
