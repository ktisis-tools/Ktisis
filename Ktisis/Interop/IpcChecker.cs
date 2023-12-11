using System;
using System.Linq;

namespace Ktisis.Interop {
	public class IpcChecker {
		private static bool? _isActive;
		private static DateTime? _lastCheck;
        
		public static bool IsGlamourerActive() {
			var now = DateTime.Now;
			if (_isActive == null || (now - _lastCheck)!.Value.TotalSeconds > 60) {
				_lastCheck = now;
				_isActive = Services.PluginInterface.InstalledPlugins
					.FirstOrDefault(p => p is { InternalName: "Glamourer", IsLoaded: true }) != null;
			}
			return _isActive.Value;
		}
	}
}
