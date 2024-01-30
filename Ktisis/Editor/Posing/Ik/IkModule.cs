﻿using System.Collections.Generic;
using System.Linq;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.Havok;

using Ktisis.Interop.Hooking;
using Ktisis.Structs.Havok;

namespace Ktisis.Editor.Posing.Ik;

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
	
	// Controllers

	private readonly List<IIkController> Controllers = new();

	public IIkController CreateController() {
		var controller = new IkController(this);
		controller.Setup();
		lock (this.Controllers)
			this.Controllers.Add(controller);
		return controller;
	}

	public bool RemoveController(IIkController controller) {
		lock (this.Controllers)
			return this.Controllers.Remove(controller);
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

	private void UpdateIkPoses() {
		if (!this.Manager.IsValid) return;

		IEnumerable<IIkController> controllers;
		lock (this.Controllers)
			controllers = this.Controllers.ToList();

		try {
			this.Manager.IsSolvingIk = true;
			foreach (var controller in controllers)
				controller.Solve(this.Manager.IsEnabled);
		} finally {
			this.Manager.IsSolvingIk = false;
		}
	}
}