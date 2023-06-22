using System;
using System.Collections.Generic;

using Dalamud.Logging;

namespace Ktisis.Core.Singletons;

internal class SingletonManager : Singleton {
	// Singletons

	private readonly List<Instance> Registered = new();

	// Init & dispose

	public override void Init() {
		Registered.ForEach(item => item.Init());
	}

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
	
	// Register

	internal Instance<T> Register<T>() where T : Singleton, new() {
		var singleton = new Instance<T>();
		Registered.Add(singleton);
		return singleton;
	}
}
