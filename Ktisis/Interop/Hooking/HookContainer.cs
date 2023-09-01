using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;

namespace Ktisis.Interop.Hooking;

public abstract class HookContainer {
	// Constructor

	private readonly InteropService _interop;

	public HookContainer(InteropService _interop) {
		this._interop = _interop;
	}

	// Hook creation

	protected readonly List<IHookWrapper> _hooks = new();

	public void Create() {
		try {
			SignatureHelper.Initialise(this);
		} catch (Exception) {
			GetHooks().ForEach(hook => hook.Dispose());
			throw;
		}

		foreach (var hook in GetHooks()) {
			this._hooks.Add(hook);
			this._interop.Hooks.Add(hook);
		}
	}

	// Hook access

	public bool Enabled { get; protected set; }

	public void EnableAll() {
		this.Enabled = true;
		this._hooks.ForEach(hook => hook.Enable());
	}

	public void DisableAll() {
		this.Enabled = false;
		this._hooks.ForEach(hook => hook.Disable());
	}

	// Reflection

	private List<IHookWrapper> GetHooks() {
		var results = new List<IHookWrapper>();

		var fields = this.GetType()
			.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(f => f.GetCustomAttribute<SignatureAttribute>() is not null);

		foreach (var field in fields) {
			try {
				if (GetHookFromField(field) is IHookWrapper wrapper)
					results.Add(wrapper);
			} catch (Exception err) {
				PluginLog.Error($"Failed to get wrapper for field '{field.Name}':\n{err}");
			}
		}

		return results;
	}

	private IHookWrapper? GetHookFromField(FieldInfo field) {
		var type = field.FieldType;
		if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Hook<>))
			return null;

		var value = field.GetValue(this);
		if (value is null)
			return null;

		var typeGen = typeof(HookWrapper<>)
			.GetGenericTypeDefinition()
			.MakeGenericType(type.GenericTypeArguments);

		return (IHookWrapper?)Activator.CreateInstance(typeGen, value);
	}
}
