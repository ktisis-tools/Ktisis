using System;
using System.Threading.Tasks;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Editor.Context;
using Ktisis.Scene.Modules.Actors;
using Newtonsoft.Json;

using Ktisis.Data.Config;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Config;
using Ktisis.Interface.Types;
using Ktisis.Services.Data;
using Ktisis.Localization;
using Ktisis.Interop.Ipc;

namespace Ktisis.Interface.Windows;

public class DebugWindow : KtisisWindow {
	private readonly IEditorContext _ctx;

    // tester inputs
    private int _gameObjectId;
    private string _poseJson;

    // tester outputs
    private (int, int)? _apiVersion = null;
    private bool? _isPosing = null;


    // ktisis subscriptions
    private readonly ICallGateSubscriber<(int, int)> _ktisisApiVersion;
    private readonly ICallGateSubscriber<bool> _ktisisRefreshActors;
    private readonly ICallGateSubscriber<bool> _ktisisIsPosing;
    private readonly ICallGateSubscriber<IGameObject, string, Task<bool>> _ktisisLoadPose;
    private readonly ICallGateSubscriber<IGameObject, Task<string?>> _ktisisSavePose;

	public DebugWindow(
		IEditorContext ctx,
		IDalamudPluginInterface dpi
    ) : base(
        "Debug Window"
    ) {
        this._ctx = ctx;

        // create our IPC subs from DPI
        this._ktisisApiVersion = dpi.GetIpcSubscriber<(int, int)>("Ktisis.ApiVersion");
        this._ktisisRefreshActors = dpi.GetIpcSubscriber<bool>("Ktisis.RefreshActors");
        this._ktisisIsPosing = dpi.GetIpcSubscriber<bool>("Ktisis.IsPosing");
        this._ktisisLoadPose = dpi.GetIpcSubscriber<IGameObject, string, Task<bool>>("Ktisis.LoadPose");
        this._ktisisSavePose = dpi.GetIpcSubscriber<IGameObject, Task<string?>>("Ktisis.SavePose");
    }

	public override void Draw() {
        if (!this._ctx.IsValid) {
            this.Close();
            return;
        }

		using var tabs = ImRaii.TabBar("##ConfigTabs");
		if (!tabs.Success) return;
		DrawTab("IPC Provider", this.DrawProviderTab);
		DrawTab("IPC Manager", this.DrawManagerTab);
    }
	private static void DrawTab(string name, Action handler) {
		using var tab = ImRaii.TabItem(name);
		if (!tab.Success) return;
		ImGui.Spacing();
		handler.Invoke();
	}

	private void DrawProviderTab() {
        ImGui.InputInt("GameObject Index", ref _gameObjectId);
        ImGui.InputText("Pose JSON", ref _poseJson);
        ImGui.Spacing();

        ImGui.Text("Ktisis.ApiVersion");
        if(ImGui.Button("GET"))
            _apiVersion = this._ktisisApiVersion.InvokeFunc();
        ImGui.SameLine();
        using (ImRaii.Disabled(_apiVersion == null))
            ImGui.Text($"Version: {_apiVersion}");
        ImGui.Spacing();

        ImGui.Text("Ktisis.RefreshActors");
        if(ImGui.Button("APPLY")) {}
        ImGui.Spacing();

        ImGui.Text("Ktisis.IsPosing");
        if(ImGui.Button("GET"))
            _isPosing = this._ktisisIsPosing.InvokeFunc();
        ImGui.SameLine();
        using (ImRaii.Disabled(_isPosing == null))
            ImGui.Text($"Posing: {_isPosing}");
        ImGui.Spacing();

        ImGui.Text("Ktisis.LoadPose");
        using (ImRaii.Disabled(_gameObjectId < 1 || string.IsNullOrEmpty(_poseJson)))
            if(ImGui.Button("APPLY")) {} // this._ktisisLoadPose.InvokeFunc();
        ImGui.Spacing();

        ImGui.Text("Ktisis.SavePose");
        using (ImRaii.Disabled(_gameObjectId == null))
            if(ImGui.Button("GET")) {}
        // clipboard? popup?
        ImGui.Spacing();
    }

    private void DrawManagerTab() {
        ImGui.Text("TODO");
    }
}