using System;
using System.Collections.Generic;

using Ktisis.Core;

namespace Ktisis.Editing.History;

public enum HistoryMod {
	Undo,
	Redo
}

[DIService]
public class HistoryManager {
	// Constructor

	private readonly IServiceContainer _services;

	public HistoryManager(
		IServiceContainer _services
	) {
		this._services = _services;
	}
	
	// Clients
	
	private readonly Dictionary<string, IHistoryClient> Clients = new();

	public T CreateClient<T>(string id) where T : IHistoryClient  {
		var client = this._services.Inject<T>(id, this);
		this.Clients.Add(id, client);
		return client;
	}
	
	// State

	private const int TimelineMax = 100; // TODO: Cull timeline
	
	private readonly List<HistoryActionBase> Timeline = new();

	private int Cursor;

	public void AddAction(HistoryActionBase action) {
		var count = this.Timeline.Count;
		if (this.Cursor < count) {
			Ktisis.Log.Verbose($"If history must be unwritten, let it be unwritten. ({this.Cursor} <- {count})");
			this.Timeline.RemoveRange(this.Cursor, count - this.Cursor);
		}
		
		this.Timeline.Add(action);
		this.Cursor++;
	}
	
	// Undo + redo handling

	public bool CanUndo => this.Cursor > 0;
	public bool CanRedo => this.Cursor < this.Timeline.Count;

	private bool InvokeAction(HistoryMod mod) {
		try {
			var action = this.Timeline[this.Cursor];
			if (this.Clients.GetValueOrDefault(action.HandlerId) is IHistoryClient client)
				return client.InvokeHandler(action, mod);

			Ktisis.Log.Warning($"Missing handler '{action.HandlerId}' for action '{action}'");
		} catch (Exception err) {
			Ktisis.Log.Error($"Error invoking history action:\n{err}");
		}
		
		return false;
	}

	public void Undo() {
		var i = 0;
		while (this.CanUndo && i++ < TimelineMax) {
			this.Cursor--;
			if (InvokeAction(HistoryMod.Undo)) break;
		}
	}

	public void Redo() {
		var i = 0;
		while (this.CanRedo && i++ < TimelineMax) {
			var result = InvokeAction(HistoryMod.Redo);
			this.Cursor++;
			if (result) break;
		}
	}
}
