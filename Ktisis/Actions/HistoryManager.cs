using System.Collections.Generic;

using GLib.State;

using Ktisis.Actions.Types;

namespace Ktisis.Actions;

public interface IHistoryManager {
	public int Count { get; }
	
	public bool CanUndo { get; }
	public bool CanRedo { get; }

	public void Add(IMemento item);
	public void Clear();

	public IEnumerable<IMemento> GetTimeline();

	public void Undo();
	public void Redo();
}

public class HistoryManager : IHistoryManager {
	private readonly HistoryState<IMemento> State = new();
	
	public int Count => this.State.Count;

	public bool CanUndo => this.State.CanUndo;
	public bool CanRedo => this.State.CanRedo;

	public void Add(IMemento item) {
		this.State.Add(item);
		Ktisis.Log.Debug($"Memento added: {item.GetType().Name}");
	}
	public void Clear() => this.State.Clear();
	
	public IEnumerable<IMemento> GetTimeline() => this.State.GetReadOnly();

	public void Undo() => this.State.Previous()?.Restore();
	public void Redo() => this.State.Next()?.Apply();
}
