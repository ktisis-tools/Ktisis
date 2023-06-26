using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Logging;

namespace Ktisis.Core.Singletons;

internal class SingletonManager : Singleton {
	// Singletons

	private readonly List<Instance> Registered = new();

	// Handle singleton registration

	internal Instance<T> Register<T>() where T : Singleton, new() {
		var singleton = new Instance<T>();
		Registered.Add(singleton);
		return singleton;
	}
	
	// Initialize registered singletons

	public override void Init()
		=> Registered.ForEach(item => item.Init());
	
	// Invoke OnReady for initialized singletons

	public override void OnReady()
		=> Registered.ForEach(item => item.OnReady());
	
	// Access registered singletons

	internal T Get<T>() where T : Singleton
		=> (T)Registered.First(item => item.Singleton is T).Singleton!;
	
	// Disposal
	
	public override void Dispose() {
		Registered.Reverse();
		foreach (var item in Registered) {
			try {
				item.Dispose();
			} catch (Exception e) {
				PluginLog.Error($"Error while disposing {item.Name}:\n{e}");
			}
		}
	}
}
