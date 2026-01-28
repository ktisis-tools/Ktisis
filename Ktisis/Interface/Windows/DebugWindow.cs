using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Editor.Context;
using Ktisis.Scene.Modules.Actors;
using Newtonsoft.Json;

using Ktisis.Data.Config;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Config;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Types;
using Ktisis.Services.Data;
using Ktisis.Localization;
using Ktisis.Interop.Ipc;
using Ktisis.Interface.Overlay;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Common.Utility;

namespace Ktisis.Interface.Windows;

public class DebugWindow : KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;
	private readonly TransformTable _transformTable;

	// tester inputs
	private int _gameObjectId;
	private bool _hasClip = false;

	// tester outputs
	private (int, int)? _apiVersion = null;
	private bool? _isPosing = null;

	//Transform stuff
	private Transform _transform = new();
	private string _boneName = string.Empty;
	private bool _useWorldSpace = true;
	private string _batchBoneNames = "j_kao";

	// ktisis subscriptions
	private readonly ICallGateSubscriber<(int, int)> _ktisisApiVersion;
	private readonly ICallGateSubscriber<bool> _ktisisRefreshActors;
	private readonly ICallGateSubscriber<bool> _ktisisIsPosing;
	private readonly ICallGateSubscriber<uint, string, Task<bool>> _ktisisLoadPose;
	private readonly ICallGateSubscriber<uint, Task<string?>> _ktisisSavePose;
	private readonly ICallGateSubscriber<Task<Dictionary<int, HashSet<string>>>> _ktisisSelectedBones;

	private readonly ICallGateSubscriber<uint, string, bool, Task<Matrix4x4?>> _ktisisGetMatrix;
	private readonly ICallGateSubscriber<uint, string, Matrix4x4, bool, Task<bool>> _ktisisSetMatrix;
	private readonly ICallGateSubscriber<uint, List<string>, bool, Task<Dictionary<string, Matrix4x4?>>> _ktisisBatchGetMatrix;
	private readonly ICallGateSubscriber<uint, bool, Task<Dictionary<string, Matrix4x4?>>> _ktisisGetAllMatrices;
	private readonly ICallGateSubscriber<uint, Dictionary<string, Matrix4x4>, bool, Task<bool>> _ktisisBatchSetMatrix;

	public DebugWindow(
		IEditorContext ctx,
		GuiManager gui,
		IDalamudPluginInterface dpi,
		ConfigManager cfg,
		LocaleManager locale
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
		this._ktisisSelectedBones = dpi.GetIpcSubscriber<Task<Dictionary<int, HashSet<string>>>>("Ktisis.SelectedBones");

		// Matrix subs
		this._ktisisGetMatrix = dpi.GetIpcSubscriber<uint, string, bool, Task<Matrix4x4?>>("Ktisis.GetMatrix");
		this._ktisisSetMatrix = dpi.GetIpcSubscriber<uint, string, Matrix4x4, bool, Task<bool>>("Ktisis.SetMatrix");
		this._ktisisBatchGetMatrix = dpi.GetIpcSubscriber<uint, List<string>, bool, Task<Dictionary<string, Matrix4x4?>>>("Ktisis.BatchGetMatrix");
		this._ktisisGetAllMatrices = dpi.GetIpcSubscriber<uint, bool, Task<Dictionary<string, Matrix4x4?>>>("Ktisis.GetAllMatrices");
		this._ktisisBatchSetMatrix = dpi.GetIpcSubscriber<uint, Dictionary<string, Matrix4x4>, bool, Task<bool>>("Ktisis.BatchSetMatrix");

		this._transformTable = new TransformTable(cfg, locale);
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
			if (ImGui.Button("APPLY (Clipboard)##LoadPose"))
			{
				_hasClip = CheckClipboard();
				if (_hasClip)
				{
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
			if (ImGui.Button("GET (Clipboard)##SavePose"))
			{
				var clip = await this._ktisisSavePose.InvokeFunc((uint)_gameObjectId);
				ImGui.SetClipboardText(clip);
				_hasClip = true;
				Ktisis.Log.Debug($"[DEBUG] Exported pose to clipboard from actor {_gameObjectId}: {clip}");
			}
		ImGui.Spacing();

		ImGui.Text("Ktisis.SelectedBones");
		if (ImGui.Button("GET##SelectedBones"))
		{
			var bones = await this._ktisisSelectedBones.InvokeFunc();

			foreach (var (actorId, boneSet) in bones)
			{
				Ktisis.Log.Debug($"[DEBUG] Actor {actorId} selected bones: {string.Join(", ", boneSet)}");
			}
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Text("Bone Transform Get/Set");

		ImGui.Checkbox("Use World Space", ref _useWorldSpace);
		ImGui.SameLine();
		ImGui.TextDisabled(_useWorldSpace ? "(World → Default)" : "(Parent-Relative)");

		ImGui.InputText("Bone Name", ref _boneName, 64);

		using (ImRaii.Disabled(_gameObjectId < 1 || string.IsNullOrEmpty(_boneName)))
		{
			if (ImGui.Button("GET##GetBoneTransform"))
			{
				var matrix = await this._ktisisGetMatrix.InvokeFunc((uint)_gameObjectId, _boneName, _useWorldSpace);
				if (matrix != null)
				{
					_transform = new Transform(matrix.Value);
					Ktisis.Log.Debug($"[DEBUG] Got matrix for bone {_boneName} on actor {_gameObjectId}");
				} else
				{
					Ktisis.Log.Warning($"[DEBUG] Failed to get matrix for bone {_boneName} on actor {_gameObjectId}");
				}
			}

			ImGui.Text("Transform Matrix:");
			if (this._transformTable.Draw(_transform, out var result, TransformTableFlags.Default))
				_transform = result;

			if (ImGui.Button("SET##SetBoneTransform"))
			{
				var success = await this._ktisisSetMatrix.InvokeFunc((uint)_gameObjectId, _boneName, _transform.ComposeMatrix(), _useWorldSpace);
				if (success)
				{
					Ktisis.Log.Debug($"[DEBUG] Set matrix for bone {_boneName} on actor {_gameObjectId}");
				}
				else
				{
					Ktisis.Log.Warning($"[DEBUG] Failed to set matrix for bone {_boneName} on actor {_gameObjectId}");
				}
			}
		}
		ImGui.Spacing();
		ImGui.Text("Batch Operations");
		ImGui.InputText("Batch Names", ref _batchBoneNames, 256);
		using (ImRaii.Disabled(_gameObjectId < 1 || string.IsNullOrEmpty(_batchBoneNames)))
		{
			if (ImGui.Button("Batch GET"))
			{
				var names = _batchBoneNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
				var results = await _ktisisBatchGetMatrix.InvokeFunc((uint)_gameObjectId, names, _useWorldSpace);
				if (results != null)
				{
					foreach (var kvp in results)
					{
						Ktisis.Log.Debug($"[DEBUG] Batch Get {kvp.Key}: {(kvp.Value.HasValue ? kvp.Value.Value.ToString() : "null")}");
					}
				}
			}

			// Uses current transform for all bones, probably better to do it some other way but idk how rn
			ImGui.SameLine();

			if (ImGui.Button("Batch SET (Current Transform)"))
			{
				var names = _batchBoneNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
				var dict = new Dictionary<string, Matrix4x4>();
				var mat = _transform.ComposeMatrix();
				foreach (var name in names) dict[name] = mat;

				var result = await _ktisisBatchSetMatrix.InvokeFunc((uint)_gameObjectId, dict, _useWorldSpace);
				Ktisis.Log.Debug($"[DEBUG] Batch Set Result: {result}");
			}

			ImGui.Spacing();
			if (ImGui.Button("Get All Matrices"))
			{
				var matrices = await _ktisisGetAllMatrices.InvokeFunc((uint)_gameObjectId, _useWorldSpace);
				if (matrices != null)
				{
					Ktisis.Log.Debug($"[DEBUG] GetAllMatrices returned {matrices.Count} entries.");
					foreach (var (name, matrix) in matrices)
					{
						var val = matrix.HasValue ? matrix.Value.ToString() : "null";
						Ktisis.Log.Debug($"[DEBUG] {name}: {val}");
					}
				} 
				else
				{
					Ktisis.Log.Warning("[DEBUG] GetAllMatrices returned null.");
				}
			}
		}
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
		
		DrawTransform();
	}

	private void DrawTransform()
	{
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
			var t = bone.GetTransformModel() ?? new Transform();
			ImGui.Spacing();
			ImGui.Text($"Havok (Matrix Decompose / Raw Transform)");
			ImGui.Text($"Position:\n\tX: {pos.X} / {t.Position.X}\n\tY: {pos.Y} / {t.Position.Y}\n\tZ: {pos.Z} / {t.Position.Z}");
			ImGui.Text($"Rotation:\n\tX: {rot.X} / {t.Rotation.X}\n\tY: {rot.Y} / {t.Rotation.Y}\n\tZ: {rot.Z} / {t.Rotation.Z}\n\tW: {rot.W} / {t.Rotation.W}");
			ImGui.Text($"Scale:\n\tX: {scl.X} / {t.Scale.X}\n\tY: {scl.Y} / {t.Scale.Y}\n\tZ: {scl.Z} / {t.Scale.Z}");
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
