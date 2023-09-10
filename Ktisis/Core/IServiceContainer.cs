using System;

namespace Ktisis.Core; 

public interface IServiceContainer : IServiceProvider {
	public T? GetService<T>();
	public T GetRequiredService<T>();

	public T Inject<T>(params object?[] deps);
}
