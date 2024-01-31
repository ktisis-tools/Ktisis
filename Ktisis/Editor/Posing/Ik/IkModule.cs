using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.Havok;

using Ktisis.Editor.Posing.Ik.Ccd;
using Ktisis.Editor.Posing.Ik.TwoJoints;
using Ktisis.Interop;
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
		var ccd = this.CreateCcdSolver();
		var twoJoints = this.CreateTwoJointsSolver();
		var controller = new IkController(this, ccd, twoJoints);
		lock (this.Controllers)
			this.Controllers.Add(controller);
		return controller;
	}

	public bool RemoveController(IIkController controller) {
		lock (this.Controllers)
			return this.Controllers.Remove(controller);
	}
	
	// Solvers
	
	public unsafe CcdSolver CreateCcdSolver(int iterations = 8, float gain = 0.5f) {
		var ccd = new Alloc<CcdIkSolver>();
		ccd.Data->_vfTable = this.CcdVfTable;
		ccd.Data->hkRefObject.MemSizeAndRefCount = 0xFFFF0001;
		ccd.Data->m_iterations = iterations;
		ccd.Data->m_gain = gain;
		var solver = new CcdSolver(this, ccd);
		solver.Setup();
		return solver;
	}

	public TwoJointsSolver CreateTwoJointsSolver() {
		var solver = new TwoJointsSolver(this);
		solver.Setup();
		return solver;
	}
	
	// Virtual Tables

	[Signature("E8 ?? ?? ?? ?? BA ?? ?? ?? ?? 48 C7 43 ?? ?? ?? ?? ??", ScanType = ScanType.StaticAddress)]
	private unsafe nint** CcdVfTable = null;
	
	// Methods

	[Signature("E8 ?? ?? ?? ?? 0F 28 55 10")]
	public SolveTwoJointsDelegate SolveTwoJoints = null!;
	public unsafe delegate nint SolveTwoJointsDelegate(byte* result, TwoJointsIkSetup* setup, hkaPose* pose);

	[Signature("E8 ?? ?? ?? ?? 8B 45 EF 48 8B 75 F7")]
	public SolveCcdDelegate SolveCcd = null!;
	public unsafe delegate nint SolveCcdDelegate(CcdIkSolver* solver, byte* result, hkArray<CcdIkConstraint>* constraints, hkaPose* hkaPose);

	[Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC 30 48 8B 01 49 8B E9")]
	public InitHkaPoseDelegate InitHkaPose = null!;
	public unsafe delegate nint InitHkaPoseDelegate(hkaPose* pose, int space, nint unk, hkArray<hkQsTransformf>* transforms);
	
	// Hooks

	[Signature("48 89 5C 24 ?? 57 48 83 EC 30 F3 0F 10 81 ?? ?? ?? ?? 48 8B FA", DetourName = nameof(UpdateAnimationDetour))]
	private Hook<UpdateAnimationDelegate> UpdateAnimationHook = null!;
	private delegate void UpdateAnimationDelegate(nint a1);

	private void UpdateAnimationDetour(nint a1) {
		this.UpdateAnimationHook.Original(a1);
		try {
			var t = new Stopwatch();
			t.Start();
			this.UpdateIkPoses();
			t.Stop();
			//Ktisis.Log.Info($"IK latency: {t.Elapsed.TotalMilliseconds:00.00}ms");
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to update IK poses:\n{err}");
		}
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
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to update IK controllers:\n{err}");
		} finally {
			this.Manager.IsSolvingIk = false;
		}
	}
}
