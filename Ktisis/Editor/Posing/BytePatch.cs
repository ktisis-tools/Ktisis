using System;

using Dalamud;
using Dalamud.Plugin.Services;

using FFXIVClientStructs;

namespace Ktisis.Editor.Posing;

public class BytePatch : IDisposable{

	private string Sig;
	private readonly ISigScanner _sigScanner;
	public BytePatch(
		string sig,
		ISigScanner scanner
	) {
		this.Sig = sig;
		this._sigScanner = scanner;
		this.SetupPatch();
	}
	private byte[] newInstructions;
	private byte[] oldInstructions;

	private nint Address = 0;
	private void SetupPatch() {
		try {
			this.Address = this._sigScanner.ScanText(Sig);
			var offset = this.Address - this._sigScanner.Module.BaseAddress;
			SafeMemory.ReadBytes(this.Address, 64, out this.oldInstructions);
			SafeMemory.ReadBytes(offset + this._sigScanner.SearchBase, 64, out this.newInstructions);
			SafeMemory.WriteBytes(this.Address, this.newInstructions);
		} catch {
			Ktisis.Log.Warning("Patch application failed");
		}

	}
	
	public void Dispose() {
		try {
			SafeMemory.WriteBytes(this.Address,  this.oldInstructions);
		} catch {Ktisis.Log.Warning("Patch removal failed");}
	}
}
