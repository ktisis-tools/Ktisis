using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Ktisis.Posing;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.Models;
using Ktisis.Interface.Overlay.Render;
using Ktisis.Common.Utility;
using Ktisis.Data.Config;
using Ktisis.Editing.Attributes;
using Ktisis.Scene;

namespace Ktisis.Editing.Modes;

[ObjectMode(EditMode.Pose, Renderer = typeof(PoseRenderer))]
public class PoseMode : ModeHandler {
	// Constructor

	private readonly ConfigService _cfg;
	
	public PoseMode(SceneManager mgr, EditorService editor, ConfigService _cfg) : base(mgr, editor) {
		this._cfg = _cfg;
	}

	// Armature enumeration

	public override IEnumerable<SceneObject> GetEnumerator() {
		if (this.Manager.Scene is not SceneGraph scene)
			yield break;

		foreach (var item in FindArmatures(scene.GetChildren()))
			yield return item;
	}

	private IEnumerable<Armature> FindArmatures(IEnumerable<SceneObject> objects) {
		foreach (var item in objects) {
			if (item is Armature armature) {
				yield return armature;
				continue;
			}

			foreach (var child in FindArmatures(item.GetChildren()))
				yield return child;
		}
	}

	// Selection

	private IEnumerable<ArmatureNode> GetSelected()
		=> this.Editor.Selection
			.GetSelected()
			.Where(item => item is ArmatureNode)
			.Cast<ArmatureNode>();

	private IEnumerable<Bone> GetCorrelatingBones() {
		var unique = new HashSet<Bone>();
		foreach (var node in GetSelected()) {
			switch (node) {
				case Bone bone:
					if (unique.Add(bone))
						yield return bone;
					break;
				case ArmatureGroup group:
					foreach (var bone in group.GetIndividualBones().Where(bone => unique.Add(bone)))
						yield return bone;
					break;
				default:
					continue;
			}
		}
	}

	// Object transform

	public override ITransform? GetTransformTarget() {
		Bone? target = null;

		foreach (var bone in GetCorrelatingBones()) {
			if (target is null) {
				target = bone;
				continue;
			}

			var armature = bone.GetArmature();
			if (armature != target.GetArmature())
				continue;

			var partialIx = bone.Data.PartialIndex;
			var partial = armature.GetPartialCache(partialIx);
			if (partial is null) continue;

			int? potentialParent = (partialIx, target.Data.PartialIndex) switch {
				var (p, t) when p == t => target.Data.BoneIndex,
				var (p, t) when p < t => armature.GetPartialCache(t)?.ConnectedParentBoneIndex,
				_ => null
			};

			if (potentialParent is int i && partial.IsBoneDescendantOf(i, bone.Data.BoneIndex))
				target = bone;
		}

		return target;
	}

	// Handler for bone manipulation

	private readonly static Vector3 InverseMax = new(10f, 10f, 10f);

	public unsafe override void Manipulate(ITransform target, Matrix4x4 matrix) {
		if (target is not ArmatureNode) return;

		// Calculate delta transform

		var deltaT = new Transform(matrix);
		var mirror = this._cfg.Config.Editor_Flags.HasFlag(EditFlags.Mirror);
		if (target.GetTransform() is Transform trans) {
			var deltaPos = deltaT.Position - trans.Position;
			var deltaRot = deltaT.Rotation / trans.Rotation;
			deltaT.Position = mirror ? -deltaPos : deltaPos;
			deltaT.Rotation = mirror ? Quaternion.Inverse(deltaRot) : deltaRot;
			deltaT.Scale /= trans.Scale;
		} else return;

		// Build armature map
		// TODO: Cache this by delegating to SelectState events?

		var armatureMap = new Dictionary<Armature, Dictionary<int, List<Bone>>>();
		foreach (var bone in GetCorrelatingBones()) {
			var armature = bone.GetArmature();
			if (armature.Parent == target) continue;

			var dictExists = armatureMap.TryGetValue(armature, out var dict);
			dict ??= new Dictionary<int, List<Bone>>();

			var partialIx = bone.Data.PartialIndex;
			var listExists = dict.TryGetValue(partialIx, out var list);
			list ??= new List<Bone>();
			list.Add(bone);

			if (!listExists) dict.Add(partialIx, list);
			if (!dictExists) armatureMap.Add(armature, dict);
		}

		// Carry out transforms

		foreach (var (armature, partialMap) in armatureMap) {
			var skeleton = armature.GetSkeleton();
			if (skeleton.IsNull || skeleton.Data->PartialSkeletons == null)
				continue;

			var modelTrans = new Transform(skeleton.Data->Transform);

			var partialCt = skeleton.Data->PartialSkeletonCount;
			for (var p = 0; p < partialCt; p++) {
				if (!partialMap.TryGetValue(p, out var boneList))
					continue;

				var partial = skeleton.Data->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				foreach (var bone in boneList) {
					var index = bone.Data.BoneIndex;
					var initial = PoseEdit.GetWorldTransform(skeleton.Data, pose, index);
					if (initial is null) continue;

					Matrix4x4 newMx;
					if (bone == target) {
						newMx = matrix;
					} else {
						var scale = Matrix4x4.CreateScale(mirror ? Vector3.Min(initial.Scale / deltaT.Scale, InverseMax) : initial.Scale * deltaT.Scale);
						var rot = Matrix4x4.CreateFromQuaternion(initial.Rotation) * Matrix4x4.CreateFromQuaternion(deltaT.Rotation);
						var pos = Matrix4x4.CreateTranslation(initial.Position + deltaT.Position);
						newMx = scale * rot * pos;
					}

					PoseEdit.SetWorldTransform(skeleton.Data, pose, index, newMx);

					// TODO: Propagation flags
					{
						var initialModel = initial.WorldToModel(modelTrans);
						var final = PoseEdit.GetModelTransform(pose, index);
						if (final is not null)
							PoseEdit.Propagate(skeleton.Data, p, index, final, initialModel);
					}
				}
			}
		}
	}
}
