using System;

namespace Ktisis.Core.Attributes; 

[AttributeUsage(AttributeTargets.Class)]
public class VersionAttribute : Attribute {
	public readonly string Target;

	public VersionAttribute(
		string target
	) {
		this.Target = target;
	}

	public bool IsValidated(out string target, out string current) {
		target = this.Target;
		current = GameVersion.GetCurrent();
		return target == current;
	}
}
