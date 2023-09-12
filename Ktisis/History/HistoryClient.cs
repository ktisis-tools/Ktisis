namespace Ktisis.History;

public interface IHistoryClient {
	public string HandlerId { get; }

	public void InvokeHandler(HistoryActionBase action, HistoryMod mod);
}

public delegate bool HandleAction<in T>(T action, HistoryMod mod);

public abstract class HistoryClient<T> : IHistoryClient where T : HistoryActionBase {
	// Dependency access
	
	private readonly HistoryService _history;
	
	// Constructor

	public string HandlerId { get; }

	protected HistoryClient(string id, HistoryService _history) {
		this._history = _history;
		
		this.HandlerId = id;
	}
	
	// Client

	private HandleAction<T>? Handler;

	public void AddHandler(HandleAction<T> handler)
		=> this.Handler += handler;
	
	public void InvokeHandler(HistoryActionBase _actionBase, HistoryMod mod) {
		if (_actionBase is not T action) return;
		this.Handler?.Invoke(action, mod);
	}
	
	// Action factory

	protected T? Action { get; private set; }

	protected abstract T Create();

	public virtual void Begin() {
		this.Action = Create();
	}

	public virtual void End() {
		if (this.Action == null) return;
		this._history.AddAction(this.Action);
		Destroy();
	}

	public void Destroy() => this.Action = null;
}
