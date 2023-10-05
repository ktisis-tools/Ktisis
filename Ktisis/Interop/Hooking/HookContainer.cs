using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using Ktisis.Interop.Hooking.Wrappers;

namespace Ktisis.Interop.Hooking; 

public abstract class HookContainer {
	// Hook creation
	
	private List<IHookWrapper>? _hooks;
	
	// Hook access

	public bool Enabled { get; protected set; }

	public void EnableAll() {
		if (this._hooks == null) return;
		this.Enabled = true;
		this._hooks?.ForEach(hook => hook.Enable());
	}

	public void DisableAll() {
		if (this._hooks == null) return;
		this.Enabled = false;
		this._hooks?.ForEach(hook => hook.Disable());
	}
	
	// Reflection

	internal List<IHookWrapper> GetHooks() {
		if (this._hooks != null)
			return this._hooks;

		var hooks = new List<IHookWrapper>();
		
		var fields = this.GetType()
			.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(f => f.GetCustomAttribute<SignatureAttribute>() is not null);
		
		foreach (var field in fields) {
			try {
				if (GetHookFromField(field) is IHookWrapper wrapper)
					hooks.Add(wrapper);
			} catch (Exception err) {
				Ktisis.Log.Error($"Failed to get wrapper for field '{field.Name}':\n{err}");
			}
		}

		return this._hooks = hooks;
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
