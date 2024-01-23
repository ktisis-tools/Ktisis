using System;

using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Ktisis.Editor.Posing.Types;

public interface IPosingManager : IDisposable {
	public bool IsValid { get; }
	
	public void Initialize();

	public bool IsEnabled { get; }
	public void SetEnabled(bool enable);

	public unsafe void PreservePoseFor(GameObject gameObject, Skeleton* skeleton);
	public unsafe void RestorePoseFor(GameObject gameObject, Skeleton* skeleton, ushort partialId);
}
