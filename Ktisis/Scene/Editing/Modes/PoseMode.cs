using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Ktisis.Posing;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.Models;
using Ktisis.Common.Utility;

namespace Ktisis.Scene.Editing.Modes;

public class PoseMode : ModeHandler {
	public PoseMode(SceneManager mgr) : base(mgr) {}
	
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
		=> this.Manager.Editor.Selection
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

	private Bone? GetTransformTarget() {
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

	public override Transform? GetTransform()
		=> GetTransformTarget()?.GetTransform();
	
	// Handler for bone manipulation

	public unsafe override void Manipulate(Matrix4x4 matrix, Matrix4x4 deltaMx) {
		var target = GetTransformTarget();
		
		// Calculate delta transform

		var deltaT = new Transform(matrix);
		if (target?.GetTransform() is Transform trans) {
			deltaT.Position -= trans.Position;
			deltaT.Rotation /= trans.Rotation;
			deltaT.Scale = new Transform(deltaMx).Scale;
		} else return;
		
		// Build armature map
		// TODO: Cache this by delegating to SelectState events?
		
		var armatureMap = new Dictionary<Armature, Dictionary<int, List<Bone>>>();
		foreach (var bone in GetCorrelatingBones()) {
			var armature = bone.GetArmature();

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
			if (skeleton.IsNullPointer || skeleton.Data->PartialSkeletons == null)
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
					var initial = PoseEditor.GetWorldTransform(skeleton.Data, pose, index);
					if (initial is null) continue;

					Matrix4x4 newMx;
					if (bone == target) {
						newMx = matrix;
					} else {
						var scale = Matrix4x4.CreateScale(initial.Scale * deltaT.Scale);
						var rot = Matrix4x4.CreateFromQuaternion(initial.Rotation) * Matrix4x4.CreateFromQuaternion(deltaT.Rotation);
						var pos = Matrix4x4.CreateTranslation(initial.Position + deltaT.Position);
						newMx = scale * rot * pos;
					}
					
					PoseEditor.SetWorldTransform(skeleton.Data, pose, index, newMx);
					
					// TODO: Propagation flags
					{
						var initialModel = PoseEditor.WorldToModel(initial, modelTrans);
						var final = PoseEditor.GetModelTransform(pose, index);
						if (final is not null)
							PoseEditor.Propagate(skeleton.Data, p, index, final, initialModel);
					}
				}
			}
		}
	}
}