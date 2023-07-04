using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Dalamud.Logging;
using Dalamud.Interface.Internal.Notifications;

namespace Ktisis.Core.Singletons; 

public class ServiceProvider : Singleton {
	protected virtual bool _Ready { get; set; }
	
	// Initialization

	public override void Init() {
		PluginLog.Verbose("Starting service initialization...");

		var hasError = false;
		foreach (var (field, attr) in GetServicesFieldsAndAttributes()) {
			try {
				PluginLog.Verbose($"Creating service: {field.FieldType.Name}");

				var instance = field.GetValue(this);
				if (instance == null) {
					instance = Activator.CreateInstance(field.FieldType);
					field.SetValue(this, instance);
				}

				if (instance is Service service) {
					service.Init();
					OnInitService(service);
				} else {
					PluginLog.Warning("Failed to create service!");
				}
			} catch (Exception e) {
				if (attr.Flags.HasFlag(ServiceFlags.Critical)) {
					PluginLog.Fatal("Critical service failed to start, aborting plugin initialization.");
					throw;
				}

				hasError = true;
				PluginLog.Error($"{field.Name} encountered an error during initialization:\n{e}");
			}
		}

		if (hasError) {
			Services.PluginInterface.UiBuilder.AddNotification(
				"Ktisis failed to load one or more non-critical service.\nPlease see the error log for more information.",
				Ktisis.VersionName, NotificationType.Warning, 10000
			);
		}
	}

	protected virtual void OnInitService(Service service) { }

	// Invoke OnReady

	public override void OnReady() {
		foreach (var field in GetServiceFields()) {
			try {
				if (field.GetValue(this) is Service service)
					service.OnReady();
			} catch (Exception e) {
				PluginLog.Error($"Error occurred in {field.Name}.OnReady:\n{e}");
			}
		}

		_Ready = true;
	}
	
	// Disposal

	public override void Dispose() {
		PluginLog.Verbose("Disposing services...");
		foreach (var field in GetServiceFields()) {
			try {
				if (field.GetValue(this) is not Service service) continue;
				service.Dispose();
				OnDisposeService(service);
			} catch (Exception e) {
				PluginLog.Error($"Error while disposing {field.Name}:\n{e}");
			}
		}
	}

	protected virtual void OnDisposeService(Service service) { }

	// Reflection

	private IEnumerable<FieldInfo> GetAllFields() => GetType()
		.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

	private IEnumerable<FieldInfo> GetServiceFields() => GetAllFields()
		.Where(f => f.GetCustomAttribute<ServiceAttribute>() is not null);

	private IEnumerable<(FieldInfo, ServiceAttribute)> GetServicesFieldsAndAttributes() => GetServiceFields()
		.Select(f => (Field: f, Attribute: f.GetCustomAttribute<ServiceAttribute>()))
		.Where(x => x.Attribute is not null)!;
}

[Flags]
internal enum ServiceFlags {
	None,
	Critical
}

[AttributeUsage(AttributeTargets.Field)]
internal class ServiceAttribute : Attribute {
	internal readonly ServiceFlags Flags;
	
	internal ServiceAttribute(ServiceFlags flags = ServiceFlags.None)
		=> Flags = flags;
}