using System.Collections.Generic;

namespace Ktisis.Scene.Impl;

public interface IParentable<T> {
	public void AddChild(T child);
	public void RemoveChild(T child);

	public int Count { get; }

	public IReadOnlyList<T> GetChildren();
}
