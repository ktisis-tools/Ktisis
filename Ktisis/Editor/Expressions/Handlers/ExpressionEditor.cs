using System;
using System.Collections.Generic;
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
	// Face partial skeletons (face + secondary face), parented to j_kao.
	private static readonly int[] FacePartials = [1, 2];

	// Rotations below this angle (radians) are treated as noise when capturing.
	private const float CaptureEpsilon = 0.0087f; // ~0.5 degrees
	private const float CapturePosEpsilon = 0.0005f;

	private ExpressionLibrary Library => mgr.GetLibrary(actor);

	public ActionUnitCatalog Catalog => this.Library.Catalog;

	private ExpressionState State => mgr.GetState(actor.Actor.ObjectIndex);

	public bool HasNeutral => this.State.Neutral != null;

	// Neutral & deltas are stored in HEAD-RELATIVE space (relative to the face
	// partial root, i.e. j_kao), NOT immediate-parent local. This is what makes an
	// AU orientation-independent and lets deltas be authored/baked offline from a
	// flat pose dump: the head's contribution is divided out. At apply time the
	// result is re-expressed through the head's CURRENT model transform, so the
	// expression follows the head when it's posed. HavokPosing only writes model
	// space, so we convert on read (model -> head-relative) and write (head-relative
	// -> model).

	// Neutral baseline

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
	}

	public void InvalidateNeutral() => this.State.Neutral = null;

	// Weights

	public float GetWeight(string id) => this.State.Weights.GetValueOrDefault(id, 0f);

	public void SetWeight(string id, float weight) {
		this.EnsureNeutral();
		this.State.Weights[id] = weight;
		this.ApplyBlend();
	}

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

		var neutral = this.State.Neutral;
		if (neutral == null) return;

		var library = this.Library;
		var weights = this.State.Weights;

		foreach (var partialIx in FacePartials) {
			if (partialIx >= skeleton->PartialSkeletonCount) continue;
			var hkaPose = pose.GetPose(partialIx);
			if (hkaPose == null || hkaPose->Skeleton == null) continue;

			var head = HavokPosing.GetModelTransform(hkaPose, 0);
			if (head == null) continue;

			// Affected bones in this partial, applied parent-first (ascending index).
			var targets = new List<(int idx, string name)>();
			for (var i = 1; i < hkaPose->Skeleton->Bones.Length; i++) {
				var name = hkaPose->Skeleton->Bones[i].Name.String;
				if (name.IsNullOrEmpty()) continue;
				if (!library.AffectedBones.Contains(name)) continue;
				if (!neutral.ContainsKey(name)) continue;
				targets.Add((i, name));
			}
			targets.Sort((a, b) => a.idx.CompareTo(b.idx));

			// Phase 1: pin EVERY captured bone to its head-relative neutral — a clean,
			// drift-free slate. We can't reset only the affected bones: non-affected
			// descendants (e.g. the tongue under the jaw) are moved by phase-2
			// propagation and, if not re-anchored each pass, accumulate offsets across
			// the many ApplyBlend calls a slider drag produces. Pinning the whole face
			// keeps ApplyBlend idempotent. Neutral is the captured face (incl. manual
			// posing), so this only deviates where an active AU drives a bone.
			for (var i = 1; i < hkaPose->Skeleton->Bones.Length; i++) {
				var name = hkaPose->Skeleton->Bones[i].Name.String;
				if (name.IsNullOrEmpty() || !neutral.TryGetValue(name, out var baseline)) continue;
				HavokPosing.SetModelTransform(hkaPose, i, HeadToModel(head, baseline.Rotation, baseline.Position, baseline.Scale));
			}

			// Phase 2: apply only the *active* AUs (parent-first), propagating so each
			// bone carries the chain below it — e.g. Jaw Open rotates the jaw and the
			// lower-mouth bones follow. Inactive affected bones are left at neutral or
			// carried by an active ancestor instead of being pinned.
			foreach (var (i, name) in targets) {
				var (deltaRot, posDelta) = this.ComposeDelta(library.Catalog, name, weights);
				if (IsIdentity(deltaRot) && posDelta.LengthSquared() < 1e-10f) continue;

				var baseline = neutral[name];
				var relRot = Quaternion.Normalize(deltaRot * baseline.Rotation);
				var relPos = baseline.Position + posDelta;
				var target = HeadToModel(head, relRot, relPos, baseline.Scale);

				var initial = HavokPosing.GetModelTransform(hkaPose, i);
				if (initial == null) continue;

				HavokPosing.SetModelTransform(hkaPose, i, target);
				HavokPosing.Propagate(skeleton, partialIx, i, target, initial);
			}
		}
	}

	// Converts a head-relative rotation/position back to a model-space transform via
	// the head's current model transform.
	private static Transform HeadToModel(Transform head, Quaternion relRot, Vector3 relPos, Vector3 scale) {
		var modelRot = Quaternion.Normalize(head.Rotation * relRot);
		var modelPos = head.Position + Vector3.Transform(relPos, head.Rotation);
		return new Transform(modelPos, modelRot, scale);
	}

	private static bool IsIdentity(Quaternion q) => MathF.Abs(q.W) > 0.999995f;

	// Composes the weighted AU deltas for a single bone, in head-relative space.
	// Rotation: slerp-from-identity scaled by weight, multiplied in catalog order.
	// Position: linear sum of weighted deltas (only for AUs that opt in).
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

	// Capture current face as an AU

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
		// Zero the weight and re-blend first so the AU's bones return to neutral
		// while it's still in the catalog (afterwards they're no longer "affected"
		// and wouldn't be reset), then delete it.
		this.State.Weights[id] = 0f;
		this.ApplyBlend();
		this.Library.RemoveUnit(id);
	}

	// Expresses a bone's model transform relative to the face root (head).
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
