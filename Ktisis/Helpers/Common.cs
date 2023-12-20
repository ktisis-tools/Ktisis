using System;
using System.IO;
using System.Diagnostics;
using System.Security.AccessControl;

namespace Ktisis.Helpers {
	internal static class Common {
		// From SimpleTweaks - Thanks Caraxi
		internal static void OpenBrowser(string url)
			=> Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

		internal static bool IsPathValid(string path) {
			var di = new DirectoryInfo(path);
			if (!di.Exists) return false;

			var canRead = false;

			try {
				var acl = di.GetAccessControl();
				var rules = acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
				foreach (FileSystemAccessRule rule in rules) {
					if ((rule.FileSystemRights & FileSystemRights.Read) == 0)
						continue;

					switch (rule.AccessControlType) {
						case AccessControlType.Allow:
							canRead = true;
							break;
						case AccessControlType.Deny:
							return false;
						default:
							continue;
					}
				}

				return canRead;
			} catch (UnauthorizedAccessException e) {
				Logger.Error(e, "Unable to check access to path: {Path}", path);
				
				return false;
			}
		}
	}
}
