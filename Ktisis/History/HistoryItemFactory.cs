using Ktisis.Overlay;

using static FFXIVClientStructs.Havok.hkaPose;

namespace Ktisis.History {
	public static class HistoryItemFactory {

		public unsafe static HistoryItem Create(string type) {
			HistoryItem item;

			switch (type) {
				case "ActorBone":
					item = CreateActorBoneItem();
					break;
				default:
					throw new System.ArgumentException("You are trying to add an unknown type to the history.", "type");
			}
			return item;
		}

		private static unsafe ActorBone CreateActorBoneItem() {
			var bone = Skeleton.GetSelectedBone();
			var boneTransform = bone!.AccessModelSpace(PropagateOrNot.DontPropagate);
			var matrix = Interop.Alloc.GetMatrix(boneTransform);
			return new ActorBone(
						matrix,
						bone,
						Ktisis.Configuration.EnableParenting,
						Ktisis.Configuration.SiblingLink);
		}
	}
}
