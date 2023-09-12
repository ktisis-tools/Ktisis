using System.Collections.Generic;

using Dalamud.Logging;

using Ktisis.Core;
using Ktisis.Core.Impl;
using Ktisis.History;

namespace Ktisis.History;

public enum HistoryMod {
    Undo,
	Redo
}

[KtisisService]
public class HistoryService {
	// Constructor

	private readonly IServiceContainer _services;

	public HistoryService(IServiceContainer _services) {
		this._services = _services;
	}
	
	// Clients
	
	private readonly List<IHistoryClient> Clients = new();

	public T CreateClient<T>(string id, params object[] @params) where T : IHistoryClient  {
		var client = this._services.Inject<T>(id, this);
		this.Clients.Add(client);
		return client;
	}
	
	// State
	
	private readonly List<HistoryActionBase> Timeline = new();

	private int Cursor = 0;

	public void AddAction(HistoryActionBase action) {
		PluginLog.Information($"Adding action: {action}");
		
		this.Timeline.Add(action);
	}
}
