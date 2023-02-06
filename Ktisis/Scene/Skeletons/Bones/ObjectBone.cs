using Ktisis.Posing;
using Ktisis.Services;

using Ktisis.Scene.Interfaces;
using Ktisis.Scene.Skeletons.Bones;
using Ktisis.Library.Extensions;
using Dalamud.Logging;

namespace Ktisis.Scene.Skeletons {
	public class ObjectBone : Manipulable, ITransformable, IVisibilityToggle {
		public ObjectBone(SkeletonObject skele, Bone bone) {
			Skeleton = skele;

			BoneName = bone.HkaBone.Name.String ?? "Unknown";
			Partial = bone.Partial;
			Index = bone.Index;
		}

		// Properties

		private SkeletonObject Skeleton;

		private string BoneName;
		internal int Partial;
		internal int Index;

		internal (int p, int i) Pair => (Partial, Index);
		internal string Key => $"{Pair.p},{Pair.i}";

		// Methods

		public unsafe Bone? GetBone() {
			var skele = Skeleton.GetSkeleton();
			if (skele == null) return null;
			return new Bone(skele, Partial, Index);
		}

		public bool IsBone(Bone bone) => bone.Partial == Partial && bone.Index == Index;

		public bool ShouldDraw() {
			var res = Visible;
			if (Parent is BoneGroup cat)
				res &= cat.ShouldDraw();
			return res;
		}

		// Manipulable

		public override string Name {
			get => LocaleService.GetBoneName(BoneName);
			set { }
		}

		public override void Context() {}

		// Visibility

		public bool Visible {
			get => Skeleton.VisibilityMap.TryGetValue(Key, out var value) ? value : false;
			set => Skeleton.VisibilityMap[Key] = value;
		}

		// Transformable

		public Transform? GetTransform() {
			var bone = GetBone();
			if (bone == null) return null;
			return Transform.FromHavok(bone.Transform);
		}

		public unsafe void SetTransform(Transform trans) {
			var bone = GetBone();
			if (bone == null) return;

			var transform = bone.AccessModelSpace();

			var initialRot = transform->Rotation.ToQuat();
			var initialPos = transform->Translation.ToVector3();

			*transform = trans.ToHavok();
			if (Ktisis.Configuration.EnableParenting)
				bone.PropagateChildren(transform, initialPos, initialRot);

			var boneName = bone.HkaBone.Name.ToString() ?? "";
			if (boneName.EndsWith("_l") || boneName.EndsWith("_r")) {
				var sibling = bone.GetMirrorSibling();
				if (sibling != null)
					sibling.PropagateSibling(transform->Rotation.ToQuat() / initialRot, Ktisis.Configuration.SiblingLink);
			}
		}
	}
}