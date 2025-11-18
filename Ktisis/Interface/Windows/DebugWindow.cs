using System;
using System.Numerics;
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
using Ktisis.Interface.Overlay;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Interface.Windows;

public class DebugWindow : KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;

    // tester inputs
    private int _gameObjectId;
    private bool _hasClip = false;

    // tester outputs
    private (int, int)? _apiVersion = null;
    private bool? _isPosing = null;


    // ktisis subscriptions
    private readonly ICallGateSubscriber<(int, int)> _ktisisApiVersion;
    private readonly ICallGateSubscriber<bool> _ktisisRefreshActors;
    private readonly ICallGateSubscriber<bool> _ktisisIsPosing;
    private readonly ICallGateSubscriber<uint, string, Task<bool>> _ktisisLoadPose;
    private readonly ICallGateSubscriber<uint, Task<string?>> _ktisisSavePose;

	public DebugWindow(
		IEditorContext ctx,
        GuiManager gui,
		IDalamudPluginInterface dpi
    ) : base(
        "Debug Window"
    ) {
        this._ctx = ctx;
        this._gui = gui;

        // create our IPC subs from DPI
        this._ktisisApiVersion = dpi.GetIpcSubscriber<(int, int)>("Ktisis.ApiVersion");
        this._ktisisRefreshActors = dpi.GetIpcSubscriber<bool>("Ktisis.RefreshActors");
        this._ktisisIsPosing = dpi.GetIpcSubscriber<bool>("Ktisis.IsPosing");
        this._ktisisLoadPose = dpi.GetIpcSubscriber<uint, string, Task<bool>>("Ktisis.LoadPose");
        this._ktisisSavePose = dpi.GetIpcSubscriber<uint, Task<string?>>("Ktisis.SavePose");
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
        DrawTab("Diagnostics", this.DrawDiagnosticsTab);
    }
	private static void DrawTab(string name, Action handler) {
		using var tab = ImRaii.TabItem(name);
		if (!tab.Success) return;
		ImGui.Spacing();
		handler.Invoke();
	}

	private async void DrawProviderTab() {
        ImGui.InputInt("GameObject Index", ref _gameObjectId);
        ImGui.Text($"Clipboard Pose Data: {_hasClip}");
        ImGui.Spacing();

        ImGui.Text("Ktisis.ApiVersion");
        if (ImGui.Button("GET##ApiVersion"))
            _apiVersion = this._ktisisApiVersion.InvokeFunc();
        ImGui.SameLine();
        using (ImRaii.Disabled(_apiVersion == null))
            ImGui.Text($"Version: {_apiVersion}");
        ImGui.Spacing();

        ImGui.Text("Ktisis.RefreshActors");
        if (ImGui.Button("APPLY##RefreshActors"))
            this._ktisisRefreshActors.InvokeFunc();
        ImGui.Spacing();

        ImGui.Text("Ktisis.IsPosing");
        if (ImGui.Button("GET##IsPosing"))
            _isPosing = this._ktisisIsPosing.InvokeFunc();
        ImGui.SameLine();
        using (ImRaii.Disabled(_isPosing == null))
            ImGui.Text($"Posing: {_isPosing}");
        ImGui.Spacing();

        ImGui.Text("Ktisis.LoadPose");
        using (ImRaii.Disabled(_gameObjectId < 1 || !_hasClip))
            if (ImGui.Button("APPLY (Clipboard)##LoadPose")) {
                _hasClip = CheckClipboard();
                if (_hasClip) {
                    var applied = await this._ktisisLoadPose.InvokeFunc((uint)_gameObjectId, ImGui.GetClipboardText());
                    if (applied)
                        Ktisis.Log.Debug($"[DEBUG] Loaded clipboard pose to actor {_gameObjectId}");
                    else
                        Ktisis.Log.Warning($"[DEBUG] Failed clipboard pose application to actor {_gameObjectId}");
                }
                else
                    Ktisis.Log.Warning("[DEBUG] Clipboard has invalid pose data, cannot apply");
            }
        ImGui.Spacing();

        // todo: popup bubble with the json output ala glamourer IPC tester
        ImGui.Text("Ktisis.SavePose");
        using (ImRaii.Disabled(_gameObjectId < 1))
            if (ImGui.Button("GET (Clipboard)##SavePose")) {
                var clip = await this._ktisisSavePose.InvokeFunc((uint)_gameObjectId);
                ImGui.SetClipboardText(clip);
                _hasClip = true;
                Ktisis.Log.Debug($"[DEBUG] Exported pose to clipboard from actor {_gameObjectId}: {clip}");
            }
        ImGui.Spacing();
    }

    private void DrawManagerTab() {
        ImGui.Text("TODO");
    }

    private void DrawDiagnosticsTab() {
        // existing debug text from overlay
        var overlay = this._gui.Get<OverlayWindow>();
        overlay.DrawDebug(null);

        // todo: scenetree / actors and entities details
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		var target = this._ctx.Transform.Target;
		if (target?.GetTransform() == null)
			return;
		var trans = target.GetTransform()!;
		ImGui.Text($"Target: {target.Primary?.Name}");
		ImGui.Text($"Position:\n\tX: {trans.Position.X}\n\tY: {trans.Position.Y}\n\tZ: {trans.Position.Z}");
		ImGui.Text($"Rotation:\n\tX: {trans.Rotation.X}\n\tY: {trans.Rotation.Y}\n\tZ: {trans.Rotation.Z}\n\tW: {trans.Rotation.W}");
		ImGui.Text($"Scale:\n\tX: {trans.Scale.X}\n\tY: {trans.Scale.Y}\n\tZ: {trans.Scale.Z}");
		var selection = this._ctx.Selection.GetFirstSelected();
		if (selection is BoneNode bone) {
			var matrix = bone.GetMatrixModel()!?? Matrix4x4.Identity;
			Matrix4x4.Decompose(
				matrix,
				out var scl,
				out var rot,
				out var pos
			);
			ImGui.Spacing();
			ImGui.Text($"Havok Transform");
			ImGui.Text($"Position:\n\tX: {pos.X}\n\tY: {pos.Y}\n\tZ: {pos.Z}");
			ImGui.Text($"Rotation:\n\tX: {rot.X}\n\tY: {rot.Y}\n\tZ: {rot.Z}\n\tW: {rot.W}");
			ImGui.Text($"Scale:\n\tX: {scl.X}\n\tY: {scl.Y}\n\tZ: {scl.Z}");
		}
	}

    private bool CheckClipboard() {
        var text = ImGui.GetClipboardText();
        if (text != null) {
            try {
                var file = JsonConvert.DeserializeObject<PoseFile>(text);
                if (file != null)
                    return true;
            } catch {
                return false;
            }
        }
        return false;
    }
}
