using System.Linq;
using System.Reflection;

using Dalamud.Plugin;

using Microsoft.Extensions.DependencyInjection;

using Ktisis.Services;
using Ktisis.Core.Attributes;

namespace Ktisis.Core; 

public sealed class ServiceComposer {
	private readonly ServiceCollection _services = new();

	public ServiceComposer AddSingleton<T>(T inst) where T : class {
		this._services.AddSingleton(inst);
		return this;
	}

	public ServiceComposer AddDalamudServices(DalamudPluginInterface dpi) {
		var inst = dpi.Create<DalamudServices>()!;
		inst.Add(dpi, this._services);
		return this;
	}

	public ServiceComposer AddFromAttributes() {
		var types = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(
				t => t.CustomAttributes.Any(
					attr => attr.AttributeType.IsAssignableTo(typeof(ServiceAttribute))
				)
			);

		foreach (var type in types) {
			var attr = type.GetCustomAttributes()
				.First(attr => attr is ServiceAttribute);
			
			switch (attr) {
				case SingletonAttribute:
					this._services.AddSingleton(type);
					break;
				case TransientAttribute:
					this._services.AddTransient(type);
					break;
			}
		}

		this._services.BuildServiceProvider(new ServiceProviderOptions() {

		});
		
		return this;
	}

	public ServiceProvider BuildProvider()
		=> this._services.BuildServiceProvider();
}
