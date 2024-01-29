using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.Havok;

using Ktisis.Interop.Hooking;
using Ktisis.Structs.Havok;

namespace Ktisis.Editor.Posing;

public sealed class IkModule : HookModule {
	private readonly PosingManager Manager;

	public IkModule(
		IHookMediator hook,
		PosingManager manager
	) : base(hook) {
		this.Manager = manager;
	}
	
	// Initialization

	public override bool Initialize() {
		var init = base.Initialize();
		if (init) this.EnableAll();
		return init;
	}
	
	// Methods

	[Signature("E8 ?? ?? ?? ?? 0F 28 55 10")]
	public SolveIkDelegate SolveIk = null!;
	public unsafe delegate nint SolveIkDelegate(byte* result, TwoJointsIkSetup* setup, hkaPose* pose);

	[Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC 30 48 8B 01 49 8B E9")]
	public InitHkaPoseDelegate InitHkaPose = null!;
	public unsafe delegate nint InitHkaPoseDelegate(hkaPose* pose, int space, nint unk, hkArray<hkQsTransformf>* transforms);
	
	// Hooks

	[Signature("48 89 5C 24 ?? 57 48 83 EC 30 F3 0F 10 81 ?? ?? ?? ?? 48 8B FA", DetourName = nameof(UpdateAnimationDetour))]
	private Hook<UpdateAnimationDelegate> UpdateAnimationHook = null!;
	private delegate void UpdateAnimationDelegate(nint a1);

	private void UpdateAnimationDetour(nint a1) {
		this.UpdateAnimationHook.Original(a1);
		this.UpdateIkPoses();
	}
	
	// Solving
	
	public bool IsSolving { get; private set; }

	private void UpdateIkPoses() {
		// TODO: Get IK solvers from pose manager
	}
}
