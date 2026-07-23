using System.Collections.Generic;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Editor.Expressions.State;
using Ktisis.Editor.Expressions.Types;
using Ktisis.Editor.Posing;
using Ktisis.Scene.Decor;

namespace Ktisis.Editor.Expressions.Handlers;

public class ExpressionController : IExpressionController {
	private readonly IExpressionManager _mgr;

	private ISkeleton? Skeleton;
	
	public ExpressionController(
		IExpressionManager mgr
	) {
		this._mgr = mgr;
	}

	public void Setup(ISkeleton skeleton) {
		this.Skeleton = skeleton;
	}
	
	// Expression state
	
	public ushort RaceSexId { get; private set; }

	public int Count => this._state.Count;

	public IReadOnlyDictionary<string, ExpressionState> GetExpressions() => this._state;

	private readonly Dictionary<string, ExpressionState> _state = new();

	public void Load(ushort raceSexId) {
		Ktisis.Log.Debug($"Loading expression data: {raceSexId}");
		
		this.RaceSexId = raceSexId;
		if (!this._mgr.TryGetSchemaFile(raceSexId, out var file)) {
			Ktisis.Log.Warning($"Expression data not found for {raceSexId}");
			this.Unload();
			return;
		}
		
		this._state.Clear();
		foreach (var data in file.Data) {
			var state = this._state[data.Id] = new ExpressionState { Data = data };
			state.PrepareBlend();
		}
	}

	public void Unload() {
		this.RaceSexId = 0;
		this._state.Clear();
	}
	
	public void ResetBlendState() {
		foreach (var (_, state) in this._state)
			state.Reset();
	}
	
	// Update handler

	public unsafe void Update() {
		if (this.Skeleton == null) return;
		
		ushort raceSexId = 0;
		
		var skeleton = this.Skeleton.GetSkeleton();
		if (skeleton == null) return;
		
		var owner = skeleton->Owner;
		if (owner != null && owner->GetModelType() == CharacterBase.ModelType.Human)
			raceSexId = ((Human*)owner)->RaceSexId;

		if (this.RaceSexId == raceSexId) return;

		if (raceSexId == 0) {
			this.Unload();
			return;
		}

		this.Load(raceSexId);
	}
	
	// Blending

	public unsafe void ApplyBlend(string id, float weight) {
		if (this.Skeleton == null) return;

		var skele = this.Skeleton.GetSkeleton();
		if (skele == null || skele->PartialSkeletons == null) return;

		var pose = skele->PartialSkeletons[1].GetHavokPose(0);
		if (pose == null) return;

		if (!this._state.TryGetValue(id, out var state)) return; // Blend state

		var bones = pose->Skeleton->Bones;
		for (var i = 1; i < bones.Length; i++) {
			var name = bones[i].Name.String;
			if (name == null) continue;

			if (!state.Blend.TryGetValue(name, out var last))
				continue;
			var target = state.Data.Transforms[name];

			// interpolate based on weight
			var pos = Vector3.Lerp(Vector3.Zero, target.Position, weight);
			var rot = Quaternion.Slerp(Quaternion.Identity, target.Rotation, weight);
			var scale = Vector3.Lerp(Vector3.One, target.Scale, weight);
			var tLerp = new Transform(pos, rot, scale);
			
			// project to model space
			var parent = pose->Skeleton->ParentIndices[i];
			if (parent != -1) {
				var tParent = new Transform(pose->ModelPose[parent]);
				tLerp.Position = tParent.Position + Vector3.Transform(tLerp.Position, tParent.Rotation);
				tLerp.Rotation = tParent.Rotation * tLerp.Rotation;
				last.Position = tParent.Position + Vector3.Transform(last.Position, tParent.Rotation);
				last.Rotation = tParent.Rotation * last.Rotation;
			}
			
			// apply new delta
			var initial = new Transform(pose->ModelPose[i]);
			tLerp.Position = (initial.Position - last.Position) + tLerp.Position;
			tLerp.Rotation = (initial.Rotation / last.Rotation) * tLerp.Rotation;
			tLerp.Scale = (initial.Scale / last.Scale) * tLerp.Scale;
			HavokPosing.SetModelTransform(pose, i, tLerp);
			HavokPosing.Propagate(skele, 1, i, tLerp, initial);

			last.Position = pos;
			last.Rotation = rot;
			last.Scale = scale;
		}

		state.Weight = weight;
	}
	
	// Remove handlers

	public void Destroy() {
		this._mgr.RemoveController(this);
	}
}
