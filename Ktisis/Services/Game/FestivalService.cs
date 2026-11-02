using System;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

using Ktisis.Core.Attributes;

namespace Ktisis.Services.Game;

[Singleton]
public unsafe class FestivalService : IDisposable {
	private readonly IFramework _framework;
	public FestivalService(
		IFramework framework
	) {
		this._framework = framework;
		this.BaseFestivals = GameMain.Instance()->ActiveFestivals.ToArray();
		this.ActiveFestivals = this.BaseFestivals;
	}
	
	private GameMain.Festival[] BaseFestivals;
	private GameMain.Festival[] ActiveFestivals;

	public void SetFestivals(GameMain.Festival festival) {
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
	
	public void Reset() {
		var instance = LayoutWorld.Instance();
		instance->ActiveLayout->FestivalStatus = 0;
		fixed (GameMain.Festival* fst = this.BaseFestivals)
			instance->ActiveLayout->SetActiveFestivals(fst);
		
		instance->ActiveLayout->Update(&Framework.Instance()->FrameDeltaTime);
	}

	public void Dispose() {
		
		this.Reset();
	}
}