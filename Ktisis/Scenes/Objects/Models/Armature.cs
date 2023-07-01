using System.Collections.Generic;

using Dalamud.Interface;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Scenes.Tree;
using Ktisis.Scenes.Objects.World;

namespace Ktisis.Scenes.Objects.Models;

public class Armature : SceneObject {
	// Trees

	public override uint Color { get; init; } = 0xFFFF9F68;

	public override FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.CircleNodes;

	// Bones

	private new Character? Parent => base.Parent as Character;

	private readonly Dictionary<int, uint> Partials = new();

	// Update armature

	internal override void Update() {
		UpdatePartials();
	}

	// Partials

	private unsafe void UpdatePartials() {
		var skele = GetSkeleton();
		if (skele == null) return;

		for (var p = 0; p < skele->PartialSkeletonCount; p++) {
			var partial = skele->PartialSkeletons[p];
			var res = partial.SkeletonResourceHandle;

			var id = res != null ? res->ResourceHandle.Id : 0;
			if (!Partials.TryGetValue(p, out var prevId)) {
				Partials.Add(p, id);
				prevId = 0;
			}

			if (id == prevId) continue;
			Partials[p] = id;

			var builder = new BoneTreeBuilder(p, id, partial);
			if (id != 0)
				AddPartial(p, id, builder);
			if (prevId != 0)
				builder.Clean(this);
		}
	}

	private void AddPartial(int index, uint id, BoneTreeBuilder builder) {
		Partials[index] = id;
		builder.Add(this);
	}

	// Unmanaged helpers

	private unsafe CharacterBase* GetParentChara()
		=> (CharacterBase*)(Parent?.Address ?? 0);

	private unsafe Skeleton* GetSkeleton() {
		var parent = GetParentChara();
		return parent != null ? parent->Skeleton : null;
	}
}
