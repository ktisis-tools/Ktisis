using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Logging;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Scene.Objects.Tree;
using Ktisis.Scene.Objects.World;
using Ktisis.Data.Config.Display;
using Ktisis.Interop.Unmanaged;

namespace Ktisis.Scene.Objects.Models;

public class Armature : ArmatureGroup {
	// Properties

	public override ItemType ItemType => ItemType.Armature;
	
	// Bones

	private readonly Dictionary<int, uint> Partials = new();

	private readonly Dictionary<(int p, int i), Bone> BoneMap = new();
	
	// Update handler

	public unsafe override void Update(SceneManager _mgr, SceneContext ctx) {
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

			var isWeapon = skele->Owner->GetModelType() == CharacterBase.ModelType.Weapon;
			var builder = new BoneTreeBuilder(p, id, partial, !isWeapon ? ctx.GetConfig().Categories : null);
			if (id != 0) {
				this.Partials[p] = id;
				builder.AddToArmature(this);
			}
			
			if (prevId != 0)
				Clean(p, id);
			
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
			if (child is Bone bone && bone.PartialId == pId)
				this.BoneMap.Add((pIndex, bone.Data.BoneIndex), bone);
		}
	}
	
	// Data access

    private Character? ParentChara => this.Parent as Character;
	private unsafe Skeleton* Skeleton => this.CharaBase != null ? this.CharaBase->Skeleton : null;
	private unsafe CharacterBase* CharaBase => (CharacterBase*)(this.ParentChara?.Address ?? nint.Zero);
	
	public override Armature GetArmature() => this;
	public new unsafe Pointer<Skeleton> GetSkeleton() => new(this.Skeleton);

	public IReadOnlyDictionary<(int p, int i), Bone> GetBoneMap() => this.BoneMap.AsReadOnly();
}