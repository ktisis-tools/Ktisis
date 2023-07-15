using System;
using System.Linq;
using System.Reflection;

using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using Dalamud.Interface.Internal.Notifications;

namespace Ktisis.Interop.Resolvers; 

internal class DelegateResolver {
	// Dependency injection
	
	private const BindingFlags ReflectionFlags = BindingFlags.Default | BindingFlags.NonPublic | BindingFlags.Instance;

	private const int FailThreshold = 5;
	
	private int FailCount;
	
	internal void Resolve(object tar) {
		PluginLog.Verbose($"Resolving method for {tar.GetType().Name}...");

		SignatureHelper.Initialise(tar);
		FailCount += Validate(tar);

		switch (FailCount) {
			case >= FailThreshold:
				throw new Exception($"Resolution fail count exceeded threshold ({FailCount} >= {FailThreshold}).");
			case > 0:
				Ktisis.Notify(
					NotificationType.Warning,
					$"Ktisis failed to find {FailCount} game function{(FailCount == 1 ? "" : "s")}. You may encounter some instability.\n" +
					"If the game just updated, then the plugin might be incompatible with this patch!"
				);
				break;
			default:
				return;
		}
	}

	private int Validate(object container) => container.GetType()
		.GetFields(ReflectionFlags)
		.Count(f => GetFieldSignature(f) != null && f.GetValue(container) == null);

	private SignatureAttribute? GetFieldSignature(FieldInfo field)
		=> field.GetCustomAttribute<SignatureAttribute>();
}