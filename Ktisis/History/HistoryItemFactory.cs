using Ktisis.Overlay;

namespace Ktisis.History {
	public enum HistoryItemType {
		ActorBone
	}

	public static class HistoryItemFactory {
		public static HistoryItem? Create(HistoryItemType type) {
			HistoryItem? item = null;

			switch (type) {
				case HistoryItemType.ActorBone:
					item = CreateActorBoneItem();
					break;
				default:
					throw new System.ArgumentException("You are trying to add an unknown type to the history.", "type");
			}


			return item;
		}

		private static ActorBone? CreateActorBoneItem() {
			var bone = Skeleton.GetSelectedBone();
			if (bone == null) return null;

			var res = new ActorBone(
				bone,
				Ktisis.Configuration.EnableParenting,
				Ktisis.Configuration.SiblingLink
			);
			res.SetMatrix(true);
			return res;
		}
	}
}
