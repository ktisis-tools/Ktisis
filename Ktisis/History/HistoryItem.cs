namespace Ktisis.History {
	public abstract class HistoryItem {
		public abstract HistoryItem Clone();
		public abstract void Update(bool undo);
	}
}
