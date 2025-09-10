using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok.Animation.Rig;
using RenderSkeleton = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Posing.Ik;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton;

public class EntityPose : SkeletonGroup, ISkeleton, IConfigurable {
	private readonly IPoseBuilder _builder;
	
	public readonly IIkController IkController;
	
	public EntityPose(
		ISceneManager scene,
		IPoseBuilder builder,
		IIkController ik
	) : base(scene) {
		this._builder = builder;
		this.IkController = ik;
		
		this.Type = EntityType.Armature;
		this.Name = "Pose";
		this.Pose = this;
	}
	
	// Bones

	private readonly Dictionary<int, PartialSkeletonInfo> Partials = new();
	private readonly Dictionary<(int p, int i), BoneNode> BoneMap = new();
	
	// Update handler

	public override void Update() {
		if (!this.IsValid) return;
		this.UpdatePose();
	}

	public unsafe void Refresh() {
		this.Partials.Clear();
		
		var skeleton = this.GetSkeleton();
		if (skeleton == null) return;
		
		for (var index = 0; index < skeleton->PartialSkeletonCount; index++) {
			var partial = skeleton->PartialSkeletons[index];
			var id = GetPartialId(partial);
			this.Clean(index, id);
		}
	}

	private unsafe void UpdatePose() {
		var skeleton = this.GetSkeleton();
		if (skeleton == null) return;
		
		for (var index = 0; index < skeleton->PartialSkeletonCount; index++)
			this.UpdatePartial(skeleton, index);
	}

	private unsafe void UpdatePartial(RenderSkeleton* skeleton, int index) {
		var partial = skeleton->PartialSkeletons[index];
		var id = GetPartialId(partial);

		uint prevId = 0;
		if (this.Partials.TryGetValue(index, out var info)) {
			prevId = info.Id;
		} else {
			info = new PartialSkeletonInfo(id);
			this.Partials.Add(index, info);
		}

		if (id == prevId) return;
		
		Ktisis.Log.Verbose($"Skeleton of '{this.Parent?.Name ?? "UNKNOWN"}' detected a change in partial #{index} (was {prevId:X}, now {id:X}), rebuilding.");

		var t = new Stopwatch();
		t.Start();
		
		var builder = this._builder.BuildBoneTree(index, id, partial);
		
		var isWeapon = skeleton->Owner->GetModelType() == CharacterBase.ModelType.Weapon;
		if (!isWeapon)
			builder.BuildCategoryMap();
		else
			builder.BuildBoneList();
		
		if (prevId != 0) this.Clean(index, id);

		info.CopyPartial(id, partial);
		if (id != 0) builder.BindTo(this);
		this.FilterTree();

		this.BuildBoneMap(index, id);
		
		t.Stop();
		Ktisis.Log.Debug($"Rebuild took {t.Elapsed.TotalMilliseconds:00.00}ms");
	}

	private void BuildBoneMap(int index, uint id) {
		foreach (var key in this.BoneMap.Keys.Where(key => key.p == index))
			this.BoneMap.Remove(key);

		if (id == 0) return;
		
		foreach (var child in this.Recurse()) {
			if (child is BoneNode bone && bone.Info.PartialIndex == index)
				this.BoneMap[(index, bone.Info.BoneIndex)] = bone;
		}
	}

	private unsafe static uint GetPartialId(PartialSkeleton partial) {
		var resource = partial.SkeletonResourceHandle;
		return resource != null ? resource->Id : 0;
	}
	
	// Filtering

	private void FilterTree() {
		var bones = this.Recurse()
			.Where(entity => entity is BoneNode)
			.Cast<BoneNode>()
			.ToList();

		var remove = Enumerable.Empty<BoneNode>();
		
		// Jaw bones
		if (bones.Any(bone => bone.Info.Name == "j_f_ago")) {
			var oldJaw = bones.Where(bone => bone.Info.Name == "j_ago");
			remove = remove.Concat(oldJaw);
		}
		
		// Viera ears
		if (
			!this.Scene.Context.Config.Categories.ShowAllVieraEars
			&& this.Parent is ActorEntity actor
			&& actor.TryGetEarIdAsChar(out var earId)
		) {
			var invalid = bones.Where(bone => bone.IsVieraEarBone() && bone.Info.Name[5] != earId);
			remove = remove.Concat(invalid);
		}

		// Remove all filtered bones
		foreach (var bone in remove)
			bone.Remove();
	}
	
	// Human features

	private bool HasTail() => this.FindBoneByName("n_sippo_a") != null;
	private bool HasEars() {
		return (
			this.FindBoneByName("j_zera_a_l") != null
			|| this.FindBoneByName("j_zerb_a_l") != null
			|| this.FindBoneByName("j_zerc_a_l") != null
			|| this.FindBoneByName("j_zerd_a_l") != null
		);
	}
	
	public void CheckFeatures(out bool hasTail, out bool isBunny) {
		hasTail = this.HasTail();
		isBunny = this.HasEars();
	}
	
	// Skeleton access

	public unsafe RenderSkeleton* GetSkeleton() {
		if (!this.IsValid)
			return null;
		if (this.Parent is not CharaEntity parent || !parent.IsDrawing())
			return null;
		// abort skeleton fetch if pose's parent is an actor which is drawing
		if (this.Parent is ActorEntity actor && !actor.Actor.IsDrawing())
			return null;

		var character = parent.GetCharacter();
		return character != null ? character->Skeleton : null; 
	}

	public unsafe hkaPose* GetPose(int index) {
		var skeleton = this.GetSkeleton();
		if (skeleton == null) return null;
		var partial = skeleton->PartialSkeletons[index];
		return partial.GetHavokPose(0);
	}

	public BoneNode? GetBoneFromMap(int partialIx, int boneIx)
		=> this.BoneMap.GetValueOrDefault((partialIx, boneIx));

	public BoneNode? FindBoneByName(string name)
		=> this.BoneMap.Values.FirstOrDefault(bone => bone.Info.Name == name);

	public PartialSkeletonInfo? GetPartialInfo(int index)
		=> this.Partials.GetValueOrDefault(index);

	public bool ShouldDraw() {
		return this.Recurse().OfType<IVisibility>().Any(vis => vis.Visible);
	}
	// Remove handlers

	public override void Remove() {
		try {
			this.IkController.Destroy();
		} finally {
			base.Remove();
		}
	}
}
