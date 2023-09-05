using System;

namespace Ktisis.Scene.Editing.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ObjectModeAttribute : Attribute {
	public readonly EditMode Id;

	public Type? Renderer { get; init; }

	public ObjectModeAttribute(EditMode id) {
		this.Id = id;
	}
}
