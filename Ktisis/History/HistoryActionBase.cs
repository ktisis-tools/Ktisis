namespace Ktisis.History;

public abstract class HistoryActionBase {
	public string HandlerId;

	protected HistoryActionBase(string handlerId) {
		this.HandlerId = handlerId;
	}
}
