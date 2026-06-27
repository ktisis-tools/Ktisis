using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Utility;

using Ktisis.Common.Utility;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Expressions.Data;
using Ktisis.Editor.Expressions.Types;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Posing.Data;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Expressions.Handlers;

public class ExpressionEditor(
	IExpressionManager mgr,
	IEditorContext ctx,
	ActorEntity actor
) : IExpressionEditor {
	private static readonly int[] FacePartials = [1, 2];

	// Rotations below this angle (radians) are treated as noise when capturing.
	private const float CaptureEpsilon = 0.0087f; // ~0.5 degrees
	private const float CapturePosEpsilon = 0.0005f;

	private ExpressionLibrary Library => mgr.GetLibrary(actor);

	public ActionUnitCatalog Catalog => this.Library.Catalog;

	private ExpressionState State => mgr.GetState(actor.Actor.ObjectIndex);

	public bool HasNeutral => this.State.Neutral != null;
	
	public void EnsureNeutral() {
		if (this.State.Neutral == null)
			this.CaptureNeutral();
	}

	public unsafe void CaptureNeutral() {
		var entityPose = actor.Pose;
		if (entityPose == null) return;
		var skeleton = entityPose.GetSkeleton();
		if (skeleton == null) return;

		var neutral = new PoseContainer();
		foreach (var partialIx in FacePartials) {
			if (partialIx >= skeleton->PartialSkeletonCount) continue;
			var pose = entityPose.GetPose(partialIx);
			if (pose == null || pose->Skeleton == null) continue;

			var head = HavokPosing.GetModelTransform(pose, 0);
			if (head == null) continue;
			var invHeadRot = Quaternion.Inverse(head.Rotation);

			for (var i = 0; i < pose->Skeleton->Bones.Length; i++) {
				var name = pose->Skeleton->Bones[i].Name.String;
				if (name.IsNullOrEmpty()) continue;
				var model = HavokPosing.GetModelTransform(pose, i);
				if (model == null) continue;
				neutral[name] = ToHeadRelative(model, head, invHeadRot);
			}
		}

		this.State.Neutral = neutral;
		this.State.LastTouched.Clear();
		this.State.SolverLocal.Clear();
	}

	// Weights

	public float GetWeight(string id) => this.State.Weights.GetValueOrDefault(id, 0f);

	public void SetWeight(string id, float weight) {
		if (this.HasActiveWeights())
			this.EnsureNeutral();
		else
			this.CaptureNeutral();

		this.State.Weights[id] = weight;
		this.ApplyBlend();
	}

	private bool HasActiveWeights() => this.State.Weights.Values.Any(weight => weight != 0f);

	public void ResetWeights() {
		this.State.Weights.Clear();
		this.ApplyBlend();
	}

	public Dictionary<string, float> ExportWeights()
		=> new(this.State.Weights);

	public void LoadState(IReadOnlyDictionary<string, float>? weights, PoseContainer? neutral) {
		var state = this.State;
		state.Weights.Clear();
		if (neutral != null)
			state.Neutral = neutral;
		else
			this.CaptureNeutral();

		if (weights != null) {
			foreach (var (id, weight) in weights)
				state.Weights[id] = weight;
		}

		this.ApplyBlend();
	}

	// Blend application

	public unsafe void ApplyBlend() {
		var pose = actor.Pose;
		if (pose == null) return;
		var skeleton = pose.GetSkeleton();
		if (skeleton == null) return;

		var state = this.State;
		var neutral = state.Neutral;
		if (neutral == null) return;

		var library = this.Library;
		var weights = state.Weights;
		var lastTouched = state.LastTouched;
		var lastSolver = state.SolverLocal;
		var touched = new HashSet<string>();
		var newSolver = new Dictionary<string, Transform>();

		foreach (var partialIx in FacePartials) {
			if (partialIx >= skeleton->PartialSkeletonCount) continue;
			var hkaPose = pose.GetPose(partialIx);
			if (hkaPose == null || hkaPose->Skeleton == null) continue;

			var head = HavokPosing.GetModelTransform(hkaPose, 0);
			if (head == null) continue;

			var bones = hkaPose->Skeleton->Bones;
			var parents = hkaPose->Skeleton->ParentIndices;
			
			var protectedLocals = new List<(int idx, int parent, Transform local)>();
			for (var i = 1; i < bones.Length; i++) {
				var bone = bones[i];
				if (bone.Name.String is null) { continue; }
				
				if (!IsProtected(bone.Name.String)) continue;
				
				var parent = parents[i];
				var parentTransform = HavokPosing.GetModelTransform(hkaPose, parent);
				var cm = HavokPosing.GetModelTransform(hkaPose, i);
				if (parentTransform == null || cm == null) continue;
				
				protectedLocals.Add((i, parent, ToLocal(parentTransform, cm)));
			}

			var targets = new List<(int idx, string name)>();
			for (var i = 1; i < bones.Length; i++) {
				var name = bones[i].Name.String;
				if (name.IsNullOrEmpty()) continue;
				if (!library.AffectedBones.Contains(name)) continue;
				if (!neutral.ContainsKey(name)) continue;
				targets.Add((i, name));
			}
			
			targets.Sort((a, b) => a.idx.CompareTo(b.idx));

			//Normalise to any user edits.
			var tweaks = new Dictionary<string, Transform>();
			foreach (var (i, name) in targets) {
				Transform? baseline = lastSolver.TryGetValue(name, out var s)
					? s : NeutralParentLocal(neutral, bones[parents[i]].Name.String ?? "UNKNOWN", name);
				if (baseline == null) continue;
				var pm = HavokPosing.GetModelTransform(hkaPose, parents[i]);
				var cm = HavokPosing.GetModelTransform(hkaPose, i);
				if (pm == null || cm == null) continue;
				var tweak = ExtractUserCustomisation(baseline, ToLocal(pm, cm));
				if (tweak != null) tweaks[name] = tweak;
			}

			//Revert only AU poses back to neutral.
			for (var i = 1; i < bones.Length; i++) {
				var name = bones[i].Name.String;
				if (name.IsNullOrEmpty() || !lastTouched.Contains(name)) continue;
				if (!neutral.TryGetValue(name, out var bl)) continue;
				HavokPosing.SetModelTransform(hkaPose, i, HeadToModel(head, bl.Rotation, bl.Position, bl.Scale));
			}

			
			var touchedHere = new List<(int idx, string name)>();
			foreach (var (i, name) in targets) {
				var (deltaRot, posDelta) = this.ComposeDelta(library.Catalog, name, weights);
				if (IsIdentity(deltaRot) && posDelta.LengthSquared() < 1e-10f) continue;

				var bl = neutral[name];
				var relRot = Quaternion.Normalize(deltaRot * bl.Rotation);
				var relPos = bl.Position + posDelta;
				var target = HeadToModel(head, relRot, relPos, bl.Scale);

				var initial = HavokPosing.GetModelTransform(hkaPose, i);
				if (initial == null) continue;

				HavokPosing.SetModelTransform(hkaPose, i, target);
				HavokPosing.Propagate(skeleton, partialIx, i, target, initial);

				if (touched.Add(name)) touchedHere.Add((i, name));
				for (var j = i + 1; j < bones.Length; j++) {
					if (!HavokPosing.IsBoneDescendantOf(parents, j, i)) continue;
					var dn = bones[j].Name.String;
					if (dn.IsNullOrEmpty() || IsProtected(dn)) continue; // tongue stays put
					if (touched.Add(dn)) touchedHere.Add((j, dn));
				}
			}

			
			var solverPoses = new List<(int idx, int parent, string name, Transform local)>();
			foreach (var (i, name) in touchedHere) {
				var parent = parents[i];
				var pm = HavokPosing.GetModelTransform(hkaPose, parent);
				var cm = HavokPosing.GetModelTransform(hkaPose, i);
				if (pm == null || cm == null) continue;
				solverPoses.Add((i, parent, name, ToLocal(pm, cm)));
			}
			solverPoses.Sort((a, b) => a.idx.CompareTo(b.idx));

			foreach (var (i, parent, name, local) in solverPoses) {
				newSolver[name] = local;
				var pm = HavokPosing.GetModelTransform(hkaPose, parent);
				if (pm == null) continue;
				var finalLocal = tweaks.TryGetValue(name, out var t) ? ApplyTweak(local, t) : local;
				HavokPosing.SetModelTransform(hkaPose, i, FromParentLocal(pm, finalLocal));
			}

			//Reapply eyes/tongue protection.
			foreach (var (i, parent, local) in protectedLocals) {
				var pm = HavokPosing.GetModelTransform(hkaPose, parent);
				if (pm == null) continue;
				HavokPosing.SetModelTransform(hkaPose, i, FromParentLocal(pm, local));
			}
		}

		state.LastTouched = touched;
		state.SolverLocal = newSolver;
	}
	
	private static bool IsProtected(string name)
		=> !name.IsNullOrEmpty()
		&& (name.Contains("eye") || name.Contains("iris") || name.Contains("bero"));

	private static Transform ToLocal(Transform parent, Transform child) {
		var invRot = Quaternion.Inverse(parent.Rotation);
		return new Transform(
			Vector3.Transform(child.Position - parent.Position, invRot),
			Quaternion.Normalize(invRot * child.Rotation),
			child.Scale);
	}

	private static Transform FromParentLocal(Transform parent, Transform local) => new(
		parent.Position + Vector3.Transform(local.Position, parent.Rotation),
		Quaternion.Normalize(parent.Rotation * local.Rotation),
		local.Scale);

	private static Transform? NeutralParentLocal(PoseContainer neutral, string parentName, string name)
		=> neutral.TryGetValue(parentName, out var p) && neutral.TryGetValue(name, out var c)
			? ToLocal(p, c) : null;


	private static Transform? ExtractUserCustomisation(Transform baseline, Transform current) {
		var rot = Quaternion.Normalize(Quaternion.Inverse(baseline.Rotation) * current.Rotation);
		var pos = current.Position - baseline.Position;
		var scale = new Vector3(
			Ratio(current.Scale.X, baseline.Scale.X),
			Ratio(current.Scale.Y, baseline.Scale.Y),
			Ratio(current.Scale.Z, baseline.Scale.Z));

		var rotated = MathF.Abs(rot.W) < 0.9999995f;
		var moved = pos.LengthSquared() > 1e-12f;
		var scaled = MathF.Abs(scale.X - 1f) > 1e-5f || MathF.Abs(scale.Y - 1f) > 1e-5f || MathF.Abs(scale.Z - 1f) > 1e-5f;
		return rotated || moved || scaled ? new Transform(pos, rot, scale) : null;
	}

	private static float Ratio(float a, float b) => MathF.Abs(b) > 1e-6f ? a / b : 1f;

	// Layers a tweak from ExtractTweak back onto a solver parent-local pose.
	private static Transform ApplyTweak(Transform solver, Transform tweak) => new(
		solver.Position + tweak.Position,
		Quaternion.Normalize(solver.Rotation * tweak.Rotation),
		solver.Scale * tweak.Scale);

	// Converts a head-relative rotation/position back to a model-space transform
	private static Transform HeadToModel(Transform head, Quaternion relRot, Vector3 relPos, Vector3 scale) {
		var modelRot = Quaternion.Normalize(head.Rotation * relRot);
		var modelPos = head.Position + Vector3.Transform(relPos, head.Rotation);
		return new Transform(modelPos, modelRot, scale);
	}

	private static bool IsIdentity(Quaternion q) => MathF.Abs(q.W) > 0.999995f;

	private (Quaternion deltaRot, Vector3 posDelta) ComposeDelta(
		ActionUnitCatalog catalog,
		string name,
		IReadOnlyDictionary<string, float> weights
	) {
		var accum = Quaternion.Identity;
		var posDelta = Vector3.Zero;

		foreach (var unit in catalog.AllUnits()) {
			if (!unit.Bones.TryGetValue(name, out var delta)) continue;
			if (!weights.TryGetValue(unit.Id, out var weight)) continue;

			var min = unit.Bidirectional ? -1f : 0f;
			weight = Math.Clamp(weight, min, 1f);
			if (weight == 0f) continue;

			var effective = weight >= 0f ? delta.Rotation : Quaternion.Inverse(delta.Rotation);
			var scaled = Quaternion.Slerp(Quaternion.Identity, effective, MathF.Abs(weight));
			accum = Quaternion.Normalize(scaled * accum);

			if (unit.UsePosition)
				posDelta += delta.Position * weight;
		}

		return (accum, posDelta);
	}

	public unsafe ActionUnit CaptureCurrentAsAu(string id, string label) {
		var unit = new ActionUnit { Id = id, Label = label };

		var entityPose = actor.Pose;
		var neutral = this.State.Neutral;
		if (entityPose == null || neutral == null) return unit;
		var skeleton = entityPose.GetSkeleton();
		if (skeleton == null) return unit;

		foreach (var partialIx in FacePartials) {
			if (partialIx >= skeleton->PartialSkeletonCount) continue;
			var pose = entityPose.GetPose(partialIx);
			if (pose == null || pose->Skeleton == null) continue;

			var head = HavokPosing.GetModelTransform(pose, 0);
			if (head == null) continue;
			var invHeadRot = Quaternion.Inverse(head.Rotation);

			for (var i = 1; i < pose->Skeleton->Bones.Length; i++) {
				var name = pose->Skeleton->Bones[i].Name.String;
				if (name.IsNullOrEmpty() || !neutral.TryGetValue(name, out var baseline)) continue;

				var model = HavokPosing.GetModelTransform(pose, i);
				if (model == null) continue;
				var current = ToHeadRelative(model, head, invHeadRot);

				var deltaRot = Quaternion.Normalize(current.Rotation * Quaternion.Inverse(baseline.Rotation));
				var posDelta = current.Position - baseline.Position;

				var hasRot = IsSignificant(deltaRot);
				var hasPos = posDelta.Length() > CapturePosEpsilon;
				if (!hasRot && !hasPos) continue;

				if (hasPos) unit.UsePosition = true;
				unit.Bones[name] = new Transform(posDelta, deltaRot, Vector3.One);
			}
		}

		if (unit.Bones.Count > 0)
			this.Library.AddCapturedUnit(unit);

		return unit;
	}

	public void RemoveUnit(string id) {
		this.State.Weights[id] = 0f;
		this.ApplyBlend();
		this.Library.RemoveUnit(id);
	}

	private static Transform ToHeadRelative(Transform model, Transform head, Quaternion invHeadRot) {
		var relRot = Quaternion.Normalize(invHeadRot * model.Rotation);
		var relPos = Vector3.Transform(model.Position - head.Position, invHeadRot);
		return new Transform(relPos, relRot, model.Scale);
	}

	private static bool IsSignificant(Quaternion delta) {
		var dot = Math.Clamp(MathF.Abs(delta.W), -1f, 1f);
		var angle = 2f * MathF.Acos(dot);
		return angle > CaptureEpsilon;
	}

	// Undo

	public PoseContainer BeginEdit() => new EntityPoseConverter(actor.Pose!).Save();

	public void CommitEdit(PoseContainer initial) {
		var pose = actor.Pose;
		if (pose == null) return;

		var converter = new EntityPoseConverter(pose);
		var final = converter.Save();
		ctx.Actions.History.Add(new PoseMemento(converter) {
			Modes = PoseMode.All,
			Transforms = PoseTransforms.Position | PoseTransforms.Rotation,
			Bones = null,
			Initial = initial,
			Final = final
		});
	}
}
