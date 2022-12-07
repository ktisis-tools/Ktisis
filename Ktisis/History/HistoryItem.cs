namespace Ktisis.History {
	public abstract class HistoryItem {
		public abstract unsafe HistoryItem Clone();
		public abstract unsafe void Update(bool undo);
	}
}