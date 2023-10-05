using System.Linq;
using System.Reflection;

using Ktisis.Core;

namespace Ktisis.Interface.Input.Factory;

public delegate bool HotkeyFactoryHandler();

public class HotkeyFactory {
	private readonly InputService _input;
	private readonly IServiceContainer _services;
	
	public HotkeyFactory(InputService _input, IServiceContainer _services) {
		this._input = _input;
		this._services = _services;
	}

	public HotkeyFactory Create<T>() {
		var inst = this._services.Inject<T>();

		var handlers = typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Select(m => (method: m, attr: m.GetCustomAttribute<HotkeyAttribute>()))
			.Where(m => m.attr != null);

		foreach (var (method, attr) in handlers) {
			var handler = method.CreateDelegate<HotkeyFactoryHandler>(inst);
			this._input.RegisterHotkey(new HotkeyInfo(
				attr!.Name,
				_ => handler.Invoke(),
				attr.Flags
			), attr.Keybind);
		}

		return this;
	}
}
