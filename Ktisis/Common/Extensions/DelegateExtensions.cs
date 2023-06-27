using System;

using Dalamud.Logging;

namespace Ktisis.Common.Extensions; 

public static class DelegateExtensions {
	public static void InvokeSafely(this Delegate @delegate, params object?[]? args) {
		foreach (var invoke in @delegate.GetInvocationList()) {
			try {
				invoke.DynamicInvoke(args);
			} catch (Exception e) {
				PluginLog.Error($"Error in invocation of {@delegate.GetType().Name}:\n{e}");
			}
		}
	}
}