using System.Diagnostics;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Scenes.Tree;
using Ktisis.Scenes.Objects.World;
using Ktisis.Scenes.Objects.Impl;

namespace Ktisis.Scenes.Objects.Models;

public class Armature : SceneObject, IOverlay {
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

			PluginLog.Verbose($"Armature of '{Parent?.Name ?? "INVALID"}' detected a change in Skeleton {p} (was {prevId:X}, now {id:X}), rebuilding...");

			var t = new Stopwatch();
			t.Start();

			var buildCategories = skele->Owner->GetModelType() != CharacterBase.ModelType.Weapon;
			var builder = new BoneTreeBuilder(this, p, id, partial, buildCategories);
			if (id != 0)
				AddPartial(p, id, builder);
			if (prevId != 0)
				builder.Clean(this);

			t.Stop();
			PluginLog.Debug($"Rebuild took {t.ElapsedMilliseconds:0.00}ms");
		}
	}

	private void AddPartial(int index, uint id, BoneTreeBuilder builder) {
		Partials[index] = id;
		builder.AddToArmature(this);
	}

	// Overlay

	public bool CanDraw() => Parent?.IsRendering() ?? false;

	// Unmanaged helpers

	private unsafe CharacterBase* GetParentChara()
		=> (CharacterBase*)(Parent?.Address ?? 0);

	internal unsafe Skeleton* GetSkeleton() {
		var parent = GetParentChara();
		return parent != null ? parent->Skeleton : null;
	}
}
