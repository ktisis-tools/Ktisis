using System;

using Ktisis.Core.Attributes;

using Microsoft.Extensions.DependencyInjection;

namespace Ktisis.Core; 

[Singleton]
public class DIBuilder {
	private readonly IServiceProvider _services;

	public DIBuilder(IServiceProvider _services) {
		this._services = _services;
	}

	public object Create(Type type, params object[] parameters)
		=> ActivatorUtilities.CreateInstance(this._services, type, parameters);

	public T Create<T>(params object[] parameters)
		=> ActivatorUtilities.CreateInstance<T>(this._services, parameters);
}
