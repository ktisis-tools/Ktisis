#pragma warning disable CS0414 // Field is assigned but its value is never used

using System.Diagnostics.CodeAnalysis;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.Havok;

using Ktisis.Interop;
using Ktisis.Interop.Hooking;

namespace Ktisis.Posing;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
public class PoseHooks : HookContainer {
	// Constructor

	public PoseHooks(InteropService _interop) : base(_interop) {
		this.Create();
	}

	// SetBoneModelSpace

	[Signature("48 8B C4 48 89 58 18 55 56 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? 0F 29 70 B8 0F 29 78 A8 44 0F 29 40 ?? 44 0F 29 48 ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B B1")]
	private Hook<SetBoneModelSpaceDelegate> _setBoneModelSpaceHook = null!;
	private delegate ulong SetBoneModelSpaceDelegate(nint partial, ushort boneId, nint transform, bool enableSecondary, bool enablePropagate);

	private ulong SetBoneModelSpace(nint partial, ushort boneId, nint transform, bool enableSecondary, bool enablePropagate) => boneId;

	// SyncModelSpace

	[Signature("48 83 EC 18 80 79 38 00")]
	private Hook<SyncModelSpaceDelegate> _syncModelSpaceHook = null!;
	private delegate void SyncModelSpaceDelegate(ref hkaPose pose);

	private void SyncModelSpace(ref hkaPose pose) { /* do nothing */ }

	// CalcBoneModelSpace

	[Signature("40 53 48 83 EC 10 4C 8B 49 28")]
	private Hook<CalcBoneModelSpaceDelegate> _calcBoneModelSpaceHook = null!;
	private delegate nint CalcBoneModelSpaceDelegate(ref hkaPose pose, int boneIdx);

	private unsafe nint CalcBoneModelSpace(ref hkaPose pose, int boneIdx) => (nint)(pose.ModelPose.Data + boneIdx);

	// LookAtIK

	[Signature("48 8B C4 48 89 58 08 48 89 70 10 F3 0F 11 58")]
	private Hook<LookAtIKDelegate> _lookAtIKHook = null!;
	private delegate nint LookAtIKDelegate(nint a1, nint a2, nint a3, float a4, nint a5, nint a6);

	private nint LookAtIK(nint a1, nint a2, nint a3, float a4, nint a5, nint a6) => nint.Zero;

	// AnimFrozen

	[Signature("E8 ?? ?? ?? ?? 0F B6 F0 84 C0 74 0E")]
	private Hook<AnimFrozenDelegate> _animFrozenHook = null!;
	private delegate byte AnimFrozenDelegate(nint a1, int a2);

	private byte AnimFrozen(nint a1, int a2) => 1;
}
