using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Interop.Unmanaged;
using Ktisis.Data.Config.Display;
using Ktisis.Scene.Objects.World;
using Ktisis.Scene.Objects.Tree;

namespace Ktisis.Scene.Objects.Models;

public class Armature : ArmatureGroup {
	// Properties

	public override ItemType ItemType => ItemType.Armature;

	// Bones

	private readonly Dictionary<int, PartialCache> Partials = new();

	private readonly Dictionary<(int p, int i), Bone> BoneMap = new();

	// Update handler

	public unsafe override void Update(SceneGraph scene, SceneContext ctx) {
		var skele = this.Skeleton;
		if (skele == null) return;

		for (var p = 0; p < skele->PartialSkeletonCount; p++) {
			var partial = skele->PartialSkeletons[p];
			var res = partial.SkeletonResourceHandle;

			var id = res != null ? res->ResourceHandle.Id : 0;

			uint prevId = 0;
			if (this.Partials.TryGetValue(p, out var cache)) {
				prevId = cache.Id;
			} else {
				cache = new PartialCache(id);
				this.Partials.Add(p, cache);
			}

			if (id == prevId) continue;

			PluginLog.Verbose($"Armature of '{this.Parent?.Name ?? "UNKNOWN"}' detected a change in Skeleton #{p} (was {prevId:X}, now {id:X}), rebuilding.");

			var t = new Stopwatch();
			t.Start();

			var isWeapon = skele->Owner->GetModelType() == CharacterBase.ModelType.Weapon;
			var builder = new BoneTreeBuilder(p, id, partial, !isWeapon ? ctx.GetConfig().Categories : null, ctx);
			
			if (prevId != 0)
				Clean(p, id);

			cache.CopyPartial(id, partial);
			if (id != 0)
				builder.AddToArmature(this);

			BuildMap(p, id);

			t.Stop();
			PluginLog.Debug($"Rebuild took {t.Elapsed.TotalMilliseconds:00.00}ms");
		}
	}

	// Bone mapping

	private void BuildMap(int pIndex, uint pId) {
		foreach (var key in this.BoneMap.Keys.Where(key => key.p == pIndex))
			this.BoneMap.Remove(key);

		if (pId == 0) return;

		foreach (var child in RecurseChildren()) {
			if (child is Bone bone && bone.Data.PartialIndex == pIndex)
				this.BoneMap.Add((pIndex, bone.Data.BoneIndex), bone);
		}
	}

	// Data access

	private Character? ParentChara => this.Parent as Character;
	private unsafe Skeleton* Skeleton => this.CharaBase != null ? this.CharaBase->Skeleton : null;
	private unsafe CharacterBase* CharaBase => (CharacterBase*)(this.ParentChara?.Address ?? nint.Zero);

	// Armature / Skeleton

	public override Armature GetArmature() => this;
	public new unsafe Pointer<Skeleton> GetSkeleton() => new(this.Skeleton);

	// Bone map

	public Bone? GetBoneFromMap(int partialIx, int boneIx)
		=> this.BoneMap!.GetValueOrDefault((partialIx, boneIx), null);

	// Partial cache

	public PartialCache? GetPartialCache(int partialIx)
		=> this.Partials.TryGetValue(partialIx, out var result) ? result : null;
}
