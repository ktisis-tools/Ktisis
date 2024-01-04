using System.Collections.Generic;
using System.Linq;

using Ktisis.Editor.Actions.Types;

namespace Ktisis.Editor.Actions;

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

// TODO: Revisit this for multiple selections
public class HistoryManager : IHistoryManager {
	// State
	
	private const int TimelineMax = 100; // TODO: Cull timeline
	
	private readonly List<IMemento> Timeline = new();
	private int Cursor;

	public int Count => this.Timeline.Count;

	public void Add(IMemento item) {
		var count = this.Timeline.Count();
		if (this.Cursor < count) {
			Ktisis.Log.Verbose($"If history must be unwritten, let it be unwritten. ({this.Cursor} <- {count})");
			this.Timeline.RemoveRange(this.Cursor, count - this.Cursor);
		}

		this.Timeline.Add(item);
		this.Cursor++;
	}

	public void Clear() {
		this.Timeline.Clear();
		this.Cursor = 0;
	}

	public IEnumerable<IMemento> GetTimeline() => this.Timeline;
	
	// Undo + redo handling

	public bool CanUndo => this.Cursor > 0;
	public bool CanRedo => this.Cursor < this.Timeline.Count;

	public void Undo() {
		if (!this.CanUndo) return;
		this.Cursor--;
		this.Timeline[this.Cursor].Restore();
	}

	public void Redo() {
		if (!this.CanRedo) return;
		this.Timeline[this.Cursor].Apply();
		this.Cursor++;
	}
}
