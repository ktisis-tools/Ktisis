using Ktisis.Overlay;

namespace Ktisis.History {
	public enum HistoryItemType {
		ActorBone,
		Camera
	}
	
	public abstract class HistoryItem {
		public abstract HistoryItem Clone();
		public abstract void Update(bool undo);

		public void AddToHistory() => HistoryManager.AddEntryToHistory(this);
		
		// Factory
		
		public static HistoryItem? Create(HistoryItemType type) {
			HistoryItem? item = null;

			switch (type) {
				case HistoryItemType.ActorBone:
					item = CreateBone();
					break;
				case HistoryItemType.Camera:
					item = CreateCamera(CameraEvent.None);
					break;
				default:
					throw new System.ArgumentException("You are trying to add an unknown type to the history.", "type");
			}


			return item;
		}
		
		public static CameraHistory CreateCamera(CameraEvent camEvent)
			=> new(camEvent);

		public static ActorBone? CreateBone() {
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
