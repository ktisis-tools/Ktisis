using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Dalamud;
using Dalamud.Plugin;

using Ktisis.Services;

namespace Ktisis.Core;

internal class ServiceManager : IServiceContainer, IDisposable {
	// Service registration

	private readonly Dictionary<Type, object> Services = new();

	internal ServiceManager AddInstance<T>(T inst) => AddInstance(typeof(T), inst!);

	private ServiceManager AddInstance(Type type, object inst) {
		this.Services.Add(type, inst);
		Ktisis.Log.Verbose($"Registered service instance: {type}");
		return this;
	}

	internal ServiceManager AddDalamudServices(DalamudPluginInterface api) {
		new DalamudServices(api).AddServices(this);
		return this;
	}

	internal ServiceManager AddServices<A>() where A : Attribute {
		Assembly.GetExecutingAssembly().GetTypes()
			.Where(type => type.CustomAttributes.Any(x => x.AttributeType == typeof(A)))
			.Select(type => {
				var inst = FormatterServices.GetUninitializedObject(type);
				AddInstance(type, inst);
				return inst;
			})
			.ToList()
			.ForEach(inst => Inject(inst.GetType(), inst));
		
		return this;
	}
	
	// Service access
	
	public T? GetService<T>()
		=> (T?)this.Services.GetValueOrDefault(typeof(T));

	public object? GetService(Type serviceType)
		=> this.Services.GetValueOrDefault(serviceType);

	public T GetRequiredService<T>() {
		var type = typeof(T);
		if (this.Services.TryGetValue(type, out var service))
			return (T)service;
		throw new Exception($"Failed to find service: {type.Name}");
	}

	private IEnumerable<T> GetServicesOfType<T>() => this.Services.Values
		.Where(inst => inst is T).Cast<T>();
	
	// Injection

	private object Create(Type type, params object?[] deps) {
		if (GetConstructor(type, out var param, deps) is ConstructorInfo ctor)
			return ctor.Invoke(param);
		throw new Exception($"Failed to resolve constructor for type '{type.Name}'");
	}

	private void Inject(Type type, object target, params object?[] deps) {
		if (GetConstructor(type, out var param, deps) is ConstructorInfo ctor)
			ctor.Invoke(target, param);
		else
			throw new Exception($"Failed to resolve constructor for type '{type.Name}'");
	}
	
	public T Inject<T>(params object?[] deps) => (T)Create(typeof(T), deps);
	
	// Attribute access

	private DIServiceAttribute GetAttribute(object inst) {
		var type = inst.GetType();
		if (type.GetCustomAttribute<DIServiceAttribute>() is {} attr)
			return attr;
		throw new Exception($"Object of type '{type.Name}' does not have a service attribute.");
	}
	
	// Resolve ctor to use

	private ConstructorInfo? GetConstructor(Type type, out object?[] @params, params object?[] deps) {
		var paramList = new List<object?>();

		foreach (var ctor in type.GetConstructors()) {
			paramList.Clear();

			var valid = true;
			foreach (var param in ctor.GetParameters().Select(p => p.ParameterType)) {
				var pObj = param switch {
					_ when param == typeof(IServiceContainer) => this,
					_ when deps.FirstOrDefault(x => x!.GetType() == param) is { } res => res,
					_ => this.GetService(param)
				};

				if (pObj != null) {
					paramList.Add(pObj);
				} else {
					valid = false;
					break;
				}
			}

			if (!valid) continue;
			@params = paramList.ToArray();
			return ctor;
		}

		@params = Array.Empty<object>();
		return null;
	}
	
	// Disposal

	public void Dispose() {
		var services = this.GetServicesOfType<IDisposable>()
			.Where(inst => inst is not IServiceType);

		foreach (var inst in services)
			Dispose(inst);
		
		this.Services.Clear();
	}

	private void Dispose(IDisposable service) {
		try {
			service.Dispose();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose service {service.GetType().Name}:\n{err}");
		}
	}
}
