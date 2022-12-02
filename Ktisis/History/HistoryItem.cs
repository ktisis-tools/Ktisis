namespace Ktisis.History {
	public abstract class HistoryItem {
		public abstract unsafe HistoryItem Clone();
		public abstract unsafe void Update();
		public virtual string DebugPrint() {
			return "I don't use debug prints and I like to live on the edge.";
		}
	}
}
