using System;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

using Ktisis.Core.Attributes;

namespace Ktisis.Services.Game;

[Singleton]
public unsafe class FestivalService {
	private readonly IFramework _framework;
	private readonly GPoseService _gpose;
	public FestivalService(
		IFramework framework,
		GPoseService gpose
	) {
		this._gpose = gpose;
		this._framework = framework;
		this.BaseFestivals = LayoutWorld.Instance()->ActiveLayout->ActiveFestivals.ToArray(); // copy to revert to
		this.ActiveFestivals = LayoutWorld.Instance()->ActiveLayout->ActiveFestivals.ToArray();
	}
	
	private GameMain.Festival[] BaseFestivals;
	private GameMain.Festival[] ActiveFestivals;
	private bool Active;

	public void SetFestivals(GameMain.Festival festival) {
		if (!this.Active) {
			this._gpose.StateChanged += this.Reset;
			this.Active = true;
		}
		this.ActiveFestivals[0] = festival;
		var instance = LayoutWorld.Instance();
		instance->ActiveLayout->FestivalStatus = 5;
		instance->ActiveLayout->FestivalLayersAddTimer = 20;

		fixed (GameMain.Festival* fst = this.ActiveFestivals)
			instance->ActiveLayout->SetActiveFestivals(fst);

		void Poller(IFramework f) {
			if (instance->ActiveLayout->FestivalLayersToAdd.First != null) {
				var layer = instance->ActiveLayout->FestivalLayersToAdd.First->Value;
				var e = layer->Instances.GetEnumerator();
				foreach (var i in e) {
					if (!i.Item2.IsNull) {
						i.Item2.Value->SetColliderActive(false);
						i.Item2.Value->UpdateCollider();
					}
				}
				e.Dispose();
				this._framework.Update -= Poller;
			}
		};
		this._framework.Update += Poller;
	}
	
	public void Reset(GPoseService serv, bool state) {
		this._gpose.StateChanged -= this.Reset;
		if (!state) {
			var instance = LayoutWorld.Instance();
			instance->ActiveLayout->FestivalStatus = 5;
			fixed (GameMain.Festival* fst = this.BaseFestivals)
				instance->ActiveLayout->SetActiveFestivals(fst);
		
			instance->ActiveLayout->Update(&Framework.Instance()->FrameDeltaTime);
		}
	}
}