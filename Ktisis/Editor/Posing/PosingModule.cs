using System;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Interop.Hooking;
using Ktisis.Services.Game;

namespace Ktisis.Editor.Posing;

public unsafe delegate void SkeletonInitHandler(GameObject owner, Skeleton* skeleton, ushort partialId);

public sealed class PosingModule : HookModule {
	private readonly PosingManager Manager;
	private readonly ActorService _actors;

	public event SkeletonInitHandler? OnSkeletonInit;

	public PosingModule(
		IHookMediator hook,
		PosingManager manager,
		ActorService actors
	) : base(hook) {
		this.Manager = manager;
		this._actors = actors;
	}
	
	// Module interface
	
	public bool IsEnabled { get; private set; }

	public override void EnableAll() {
		base.EnableAll();
		this.IsEnabled = true;
	}

	public override void DisableAll() {
		base.DisableAll();
		this.IsEnabled = false;
	}
	
	// Posing hooks - thanks to perchbird (@lmcintyre) for his initial implementation of these.
	// https://github.com/ktisis-tools/Ktisis/pull/8
	
	// SetBoneModelSpace
	
	[Signature("48 8B C4 48 89 58 18 55 56 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? 0F 29 70 B8 0F 29 78 A8 44 0F 29 40 ?? 44 0F 29 48 ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B B1", DetourName = nameof(SetBoneModelSpace))]
	public Hook<SetBoneModelSpaceDelegate> _setBoneModelSpaceHook = null!;
	public delegate ulong SetBoneModelSpaceDelegate(nint partial, ushort boneId, nint transform, bool enableSecondary, bool enablePropagate);

	private ulong SetBoneModelSpace(nint partial, ushort boneId, nint transform, bool enableSecondary, bool enablePropagate) => boneId;
	
	// SyncModelSpace

	[Signature("48 83 EC 18 80 79 38 00", DetourName = nameof(SyncModelSpace))]
	public Hook<SyncModelSpaceDelegate> _syncModelSpaceHook = null!;
	public unsafe delegate void SyncModelSpaceDelegate(hkaPose* pose);

	private unsafe void SyncModelSpace(hkaPose* pose) {
		if (this.Manager.IsSolvingIk)
			this._syncModelSpaceHook.Original(pose);
		
		// do nothing
	}
	
	// CalcBoneModelSpace
	
	[Signature("40 53 48 83 EC 10 4C 8B 49 28", DetourName = nameof(CalcBoneModelSpace))]
	public Hook<CalcBoneModelSpaceDelegate> _calcBoneModelSpaceHook = null!;
	public delegate nint CalcBoneModelSpaceDelegate(ref hkaPose pose, int boneIdx);

	private unsafe nint CalcBoneModelSpace(ref hkaPose pose, int boneIdx) {
		if (this.Manager.IsSolvingIk)
			return this._calcBoneModelSpaceHook.Original(ref pose, boneIdx);
		return (nint)(pose.ModelPose.Data + boneIdx);
	}
	
	// LookAtIK

	[Signature("48 8B C4 48 89 58 08 48 89 70 10 F3 0F 11 58", DetourName = nameof(LookAtIK))]
	private Hook<LookAtIKDelegate> _lookAtIKHook = null!;
	private delegate nint LookAtIKDelegate(nint a1, nint a2, nint a3, float a4, nint a5, nint a6);

	private nint LookAtIK(nint a1, nint a2, nint a3, float a4, nint a5, nint a6) => nint.Zero;
	
	// AnimFrozen

	[Signature("E8 ?? ?? ?? ?? 0F B6 F0 84 C0 74 0E", DetourName = nameof(AnimFrozen))]
	private Hook<AnimFrozenDelegate> _animFrozenHook = null!;
	private delegate byte AnimFrozenDelegate(nint a1, int a2);

	private byte AnimFrozen(nint a1, int a2) => 1;
	
	// UpdatePpos

	[Signature("E8 ?? ?? ?? ?? EB 29 48 8B 5F 08", DetourName = nameof(UpdatePosDetour))]
	private Hook<UpdatePosDelegate> UpdatePosHook = null!;
	private delegate void UpdatePosDelegate(nint gameObject);

	private void UpdatePosDetour(nint gameObject) { }
	
	// Pose preservation handlers

	[Signature("E8 ?? ?? ?? ?? 48 C1 E5 08", DetourName = nameof(SetSkeletonDetour))]
	private Hook<SetSkeletonDelegate> SetSkeletonHook = null!;
	private unsafe delegate byte SetSkeletonDelegate(Skeleton* skeleton, ushort partialId, nint a3);
	private unsafe byte SetSkeletonDetour(Skeleton* skeleton, ushort partialId, nint a3) {
		var result = this.SetSkeletonHook.Original(skeleton, partialId, a3);
		try {
			this.HandleRestoreState(skeleton, partialId);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle SetSkeleton:\n{err}");
		}
		return result;
	}

	private unsafe void HandleRestoreState(Skeleton* skeleton, ushort partialId) {
		if (!this.Manager.IsValid || !this.IsEnabled || skeleton->PartialSkeletons == null) return;

		var partial = skeleton->PartialSkeletons[partialId];
		var pose = partial.GetHavokPose(0);
		if (pose == null) return;

		this._syncModelSpaceHook.Original(pose);

		var actor = this._actors.GetSkeletonOwner(skeleton);
		if (actor == null) return;
		Ktisis.Log.Verbose($"Restoring partial {partialId} for {actor.Name} ({actor.ObjectIndex})");
		
		this.OnSkeletonInit?.Invoke(actor, skeleton, partialId);
	}
}
