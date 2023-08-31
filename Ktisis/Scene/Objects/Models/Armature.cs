using System.Diagnostics;
using System.Collections.Generic;

using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Scene.Objects.Tree;
using Ktisis.Scene.Objects.World;
using Ktisis.Data.Config.Display;

namespace Ktisis.Scene.Objects.Models;

public class Armature : ArmatureGroup {
	// Properties

	public override ItemType ItemType => ItemType.Armature;

	// Bones

	private readonly Dictionary<int, uint> Partials = new();

	// Update handler

	public unsafe override void Update(SceneContext ctx) {
		var skele = this.Skeleton;
		if (skele == null) return;

		for (var p = 0; p < skele->PartialSkeletonCount; p++) {
			var partial = skele->PartialSkeletons[p];
			var res = partial.SkeletonResourceHandle;

			var id = res != null ? res->ResourceHandle.Id : 0;
			if (!this.Partials.TryGetValue(p, out var prevId)) {
				this.Partials.Add(p, id);
				prevId = 0;
			}

			if (id == prevId) continue;

			PluginLog.Verbose($"Armature of '{this.Parent?.Name ?? "UNKNOWN"}' detected a change in Skeleton #{p} (was {prevId:X}, now {id:X}), rebuilding.");

			var t = new Stopwatch();
			t.Start();

			var builder = new BoneTreeBuilder(p, id, partial, ctx.GetConfig().Categories);
			if (id != 0) {
				this.Partials[p] = id;
				builder.AddToArmature(this);
			}

			if (prevId != 0)
				Clean(p, id);

			t.Stop();
			PluginLog.Debug($"Rebuild took {t.Elapsed.TotalMilliseconds:00.00}ms");
		}
	}

	// Data access

	private Character? ParentChara => this.Parent as Character;

	private unsafe Skeleton* Skeleton => this.CharaBase != null ? this.CharaBase->Skeleton : null;
	private unsafe CharacterBase* CharaBase => (CharacterBase*)(this.ParentChara?.Address ?? nint.Zero);
}
