using System;
using System.Collections.Generic;

namespace Ktisis.Actions;

public interface IActionManager {
	public IHistoryManager History { get; }

	public T Get<T>() where T : ActionHandler;
	public bool TryGet<T>(out T? handler) where T : ActionHandler;

	public IActionManager Register<T>(T handler) where T : ActionHandler;
}

public class ActionManager : IActionManager, IDisposable {
	private readonly Dictionary<Type, ActionHandler> Handlers = new();
	
	public IHistoryManager History { get; } = new HistoryManager();

	public ActionManager(
		
	) {
		
	}

	public T Get<T>() where T : ActionHandler
		=> (T)this.Handlers[typeof(T)];

	public bool TryGet<T>(out T? handler) where T : ActionHandler {
		var result = this.Handlers.TryGetValue(typeof(T), out var baseHandler);
		handler = baseHandler as T;
		return result;
	}

	public IActionManager Register<T>(T handler) where T : ActionHandler {
		this.Handlers.Add(typeof(T), handler);
		return this;
	}
	
	public void Dispose() {
		this.History.Clear();
		GC.SuppressFinalize(this);
	}
}
