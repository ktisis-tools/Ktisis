using System;

using Ktisis.Common.Utility;

namespace Ktisis.Scene.Impl;

[Flags]
public enum TransformFlags {
	None = 0,
	Propagate = 1
}

public interface ITransform {
	public Transform? GetTransform();
	public void SetTransform(Transform trans, TransformFlags flags);
}