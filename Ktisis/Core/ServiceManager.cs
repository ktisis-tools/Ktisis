using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Dalamud;
using Dalamud.Logging;
using Dalamud.Plugin;

using Ktisis.Services;

namespace Ktisis.Core;

internal class ServiceManager : IServiceContainer, IDisposable {
	// Service registration

	private readonly Dictionary<Type, object> Services = new();

	internal ServiceManager AddService<T>() {
		var type = typeof(T);
		this.Services.Add(type, new Lazy<T>(CreateInstance<T>, true));
		PluginLog.Verbose($"Registered lazy service: {type}");
		return this;
	}

	internal ServiceManager AddInstance<T>(T inst) {
		var type = typeof(T);
		if (inst is null)
			throw new Exception($"Attempted to register service as null: {type}");
		this.Services.Add(type, inst);
		PluginLog.Verbose($"Registered service instance: {type}");
		return this;
	}

	internal ServiceManager AddDalamudServices(DalamudPluginInterface api) {
		new DalamudServices(api).AddServices(this);
		return this;
	}

	// Handlers for lazy initialization & dependency injection.

	private T CreateInstance<T>() {
		var type = typeof(T);

		// While circular dependencies should generally be avoided,
		// this is done to ensure that nothing explodes if they do exist.

		var inst = (T)FormatterServices.GetUninitializedObject(type)!;
		this.Services[type] = inst;

		// Resolve a valid constructor to use for dependency injection.

		if (this.Inject(inst)) return inst;

		throw new Exception($"Failed to find suitable constructor for type: {type.Name}");
	}

	public T Inject<T>(params object?[] deps) {
		var inst = (T)FormatterServices.GetUninitializedObject(typeof(T))!;
		if (!this.Inject(inst, deps))
			throw new Exception($"Failed to inject dependencies into instance of '{typeof(T).Name}'.");
		return inst;
	}

	public bool Inject<T>(T inst, params object?[] deps) {
		var ctors = typeof(T).GetConstructors();
		foreach (var ctor in ctors) {
			var @params = new List<object>();

			var isValid = true;
			foreach (var pInfo in ctor.GetParameters()) {
				var pType = pInfo.ParameterType;
				if (pType == typeof(IServiceContainer)) {
					@params.Add(this);
				} else if (this.GetService(pType) is object pObj) {
					@params.Add(pObj);
				} else {
					var dep = deps.FirstOrDefault(dep => dep!.GetType() == pType, null);
					if (dep is not null) {
						@params.Add(dep);
						continue;
					}
					isValid = false;
					break;
				}
			}

			if (!isValid) continue;
			ctor.Invoke(inst, @params.ToArray());
			return true;
		}

		return false;
	}

	// Service acquisition

	public T? GetService<T>() => (T?)GetService(typeof(T));

	public object? GetService(Type serviceType) {
		if (!this.Services.TryGetValue(serviceType, out var service))
			return null;

		// Resolve for Lazy<T> types.
		var outerType = service.GetType();
		if (outerType.IsGenericType && outerType.GetGenericTypeDefinition() == typeof(Lazy<>)) {
			PluginLog.Verbose($"Invoking init handler for service: {serviceType.Name}");

			var result = outerType
				.GetProperty("Value")!
				.GetGetMethod()!
				.Invoke(service, null);

			if (result is INotifyReady notify && this.IsReady) {
				PluginLog.Warning($"Ready notifier for service '{serviceType.Name}' was invoked late!");
				notify.OnReady();
			}

			return result;
		}

		return service;
	}

	public T GetRequiredService<T>() {
		var service = GetService(typeof(T));
		if (service is null)
			throw new Exception($"Failed to find service of type '{typeof(T).Name}'.");
		return (T)service;
	}

	// Notify ready

	private bool IsReady;

	public void NotifyReady() {
		if (this.IsReady) return;
		this.IsReady = true;

		this.Services.Values
			.Where(inst => inst is INotifyReady)
			.Cast<INotifyReady>()
			.ToList()
			.ForEach(inst => inst.OnReady());
	}

	// Disposal

	public void Dispose() {
		PluginLog.Verbose("Disposing services...");
		this.Services
			.Values
			.Where(inst => inst is IDisposable and not IServiceType)
			.Cast<IDisposable>()
			.ToList()
			.ForEach(this.Dispose);
		this.Services.Clear();
	}

	private void Dispose<T>(T inst) where T : IDisposable {
		var name = inst.GetType().Name;
		try {
			PluginLog.Verbose($"Disposing of service: {name}");
			inst.Dispose();
		} catch (Exception err) {
			PluginLog.Error($"Error while disposing of service '{name}':\n{err}");
		}
	}
}
