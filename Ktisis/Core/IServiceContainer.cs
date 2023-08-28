using System;

namespace Ktisis.Core; 

public interface IServiceContainer : IServiceProvider {
	public T? GetService<T>();
	public T GetRequiredService<T>();

	public T Inject<T>(object[]? deps = null);
	public bool Inject<T>(T inst, object[]? deps = null);
}