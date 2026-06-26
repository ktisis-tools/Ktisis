using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

using GLib.Widgets;

using Ktisis.Data.Files;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Popup;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.Utility;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Modules;
using Ktisis.Scene.Types;
using Ktisis.Services.Data;
using Ktisis.Structs.Characters;

using Lumina.Excel.Sheets;

namespace Ktisis.Interface.Windows.Editors;

public class SceneWindow : KtisisWindow {

	private readonly SceneDataService _sceneDataService;
	private readonly ISceneManager _scene;
	private readonly IEditorContext _ctx;
	private readonly ITextureProvider _textureProvider;
	private readonly IDataManager _dataManager;
	
	private bool _autosave = false;
	private SceneFile? _sceneFile;
	private ISharedImmediateTexture? _texture;
	private Map _source;
	private SceneMCDFModal? _popupWindow;
	private bool _includeActors, _includeLights, _includeCameras, _includeEnv, _includeOverlays;
	
	public SceneWindow(
		IEditorContext ctx,
		ITextureProvider textureProvider,
		IDataManager dataManager
	) : base(
		"Scene Editor") {
		this._sceneDataService = ctx.Scene.Data;
		this._scene = ctx.Scene;
		this._ctx = ctx;
		this._sceneFile = null;
		this._dataManager = dataManager;
		this._textureProvider = textureProvider;
		this._includeActors = this._includeCameras = this._includeLights = this._includeEnv = this._includeOverlays = true;
	}
	
	public override void PreOpenCheck() {
		if (this._scene.IsValid) return;
		Ktisis.Log.Verbose("State for scene editor is stale, closing...");
		this.Close();
	}
	
	public override void PreDraw() {
		base.PreDraw();
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(400, 400),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.90f
		};
	}
	
	private void OpenPopupModal(SceneFile.ActorInfo entity) {
		this._popupWindow = this._ctx.Plugin.Gui.CreatePopup<SceneMCDFModal>(entity, this._ctx);
		this._popupWindow.SetScene(ref this._sceneFile);
		this._popupWindow.Open();
	}

	private void MapStuff() {
		var mapId = this._sceneDataService.GetCurrentMapID();
		if (this._sceneFile != null) {
			mapId = this._sceneFile.MapID;
		}
		this._dataManager.GetExcelSheet<Map>().TryGetRow(mapId, out this._source);
		this._dataManager.GetExcelSheet<TerritoryType>().TryGetRow(this._source.TerritoryType.RowId, out var territory);
		if (territory.LoadingImage.ValueNullable?.FileName is { IsEmpty: false }) {
			var path = $"ui/loadingimage/{territory.LoadingImage.ValueNullable?.FileName}_hr1.tex";
			this._texture = this._textureProvider.GetFromGame(path);
		}
	}
	public override void Draw() {
		this.MapStuff();
		var iconSize = UiBuilder.DefaultFontSizePx * ImGuiHelpers.GlobalScale * 2;
		var iconBtnSize = new Vector2(iconSize, iconSize);
		int cameras, actors, lights, overlays;
		bool envOver;
		if (this._sceneFile != null) {
			actors = this._sceneFile.Actors.Count;
			cameras = this._sceneFile.Cameras.Count;
			lights = this._sceneFile.Lights.Count;
			overlays = this._sceneFile.Overlays.Count;
			envOver = this._sceneFile.Environment.Override > 0;
		} else {
			actors = this._ctx.Scene.Children.Count(entity => entity is CharaEntity);
			lights = this._ctx.Scene.Children.Count(entity => entity is LightEntity);
			overlays = this._ctx.Scene.Children.Count(entity => entity is OverlayEntity);
			cameras = this._ctx.Cameras.GetCameras().Count();
			envOver = this._ctx.Scene.GetModule<EnvModule>().Override > 0;
		}
		
		ImGui.BeginGroup();
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.PersonBurst, "Load Scene file", iconBtnSize*1.5f))
			this._ctx.Interface.OpenSceneFile(s => this._sceneFile = this._ctx.Scene.Data.LoadFile(s));
		
		using(ImRaii.Disabled(this._sceneFile != null))
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Save, $"{(this._sceneFile == null? "Save Scene file" : "Unload current Scene before saving" )}", iconBtnSize*1.5f))
				this._ctx.Interface.ExportSceneFile((this._ctx.Scene.Data.Save()));

		if (this._sceneFile != null) {
			ImGui.SetCursorPosY(ImGui.GetWindowHeight() -((iconBtnSize.Y *1.5f)* 3.3f));  //space for 2 buttons?
			if(Buttons.IconButtonTooltip(FontAwesomeIcon.Times, "Unload File", iconBtnSize*1.5f))
				this._sceneFile = null;
			if (Buttons.IconButtonTooltip(this._autosave ? FontAwesomeIcon.Globe : FontAwesomeIcon.HouseChimney, $"Choose coordinate type\nCurrently: {(this._autosave ? "World space" : "Local space")}", iconBtnSize*1.5f))
				this._autosave = !this._autosave;
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Check, "Apply Scene", iconBtnSize*1.5f)) {
				this._sceneDataService.Load(this._sceneFile, this._autosave, this._includeActors, this._includeLights, this._includeCameras);
				this._sceneFile = null;
			}
		}
		
		ImGui.EndGroup();
		
		ImGui.SameLine();
		
		ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(74f, 74f, 74f, 138f)/255);
		ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4 );
		using (var child = ImRaii.Child("##SceneData", (this._ctx.Config.Editor.UseToolbar? new Vector2(ImGui.GetContentRegionAvail().X - 0.1f, 470) :Vector2.Zero),false, ImGuiWindowFlags.AlwaysAutoResize)) {

			var cursorPos = ImGui.GetCursorScreenPos();

			if (child.Success) {
				var dl = ImGui.GetWindowDrawList();
				if(this._texture != null)
					dl.AddImageRounded(this._texture.GetWrapOrEmpty().Handle, cursorPos, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().X * .563f) + cursorPos, Vector2.Zero, Vector2.One, 0xFFFFFFFF, 4f);

				using var wrap = ImRaii.TextWrapPos(ImGui.GetWindowContentRegionMax().X);
				if (this._sceneFile == null)
					ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
				else
					ImGui.PushStyleColor(ImGuiCol.Text, this._source.RowId == this._sceneDataService.GetCurrentMapID() ? ImGuiColors.HealerGreen : ImGuiColors.DPSRed);
				ImGui.SetCursorPosY(ImGui.GetContentRegionAvail().X * .563f + ImGui.GetStyle().ItemSpacing.Y);
				ImGui.BeginGroup();
				ImGui.TextUnformatted($"From: {this._source.PlaceName.Value.Name}");
				ImGui.PopStyleColor();
				if (ImGui.CollapsingHeader($"Actors {actors}")) {

					if (this._sceneFile != null) {
						ImGui.Checkbox("Load actors", ref this._includeActors);
						ImGui.Indent();
						foreach (var actorInfo in this._sceneFile!.Actors) {

							if (!this._ctx.Config.Editor.IncognitoPlayerNames)
								ImGui.TextUnformatted($"{actorInfo.Chara.Nickname}");
							else
								ImGui.TextUnformatted($"A {((int)actorInfo.Chara.Race!.Value <= 8 ? (actorInfo.Chara.Gender == Gender.Masculine ? "\u2642" : "\u2640") + actorInfo.Chara.Race.ToString() : "Non-Humanoid Actor")} ");
							if (actorInfo.MCDF != string.Empty && !this._sceneDataService.ValidMCDFPath(actorInfo)) {
								ImGui.SameLine();
								using (ImRaii.PushFont(UiBuilder.IconFont))
									ImGui.TextColored(ImGuiColors.DalamudYellow, FontAwesomeIcon.ExclamationTriangle.ToIconString());
								if (ImGui.IsItemHovered())
									using (ImRaii.Tooltip())
										ImGui.TextUnformatted("MCDF wasnt found for this character\nPlease try applying manually after loading the scene");
							}
						}
					} else {
						ImGui.Indent();
						foreach (var charaInfo in this._ctx.Scene.Children.Where(entity => entity is CharaEntity)) {
							ImGui.TextUnformatted($"{charaInfo.Name}");
						}
					}
					ImGui.Unindent();
				}
				if (ImGui.CollapsingHeader($"Cameras {cameras}")) {
					if (this._sceneFile != null) {
						ImGui.Checkbox("Load cameras", ref this._includeCameras);
						ImGui.Indent();
						foreach (var cameraInfo in this._sceneFile!.Cameras) {
							ImGui.TextUnformatted($"{cameraInfo.Name}");
						}
					} else {
						ImGui.Indent();
						foreach (var cameraInfo in this._ctx.Cameras.GetCameras()) {
							ImGui.TextUnformatted($"{cameraInfo.Name}");
						}
					}
					ImGui.Unindent();
				}
				if (lights > 0)
					if (ImGui.CollapsingHeader($"Lights {lights}")) {
						if (this._sceneFile != null) {
							ImGui.Checkbox("Load lights", ref this._includeLights);
							ImGui.Indent();
							foreach (var lightInfo in this._sceneFile!.Lights) {
								ImGui.TextUnformatted($"{lightInfo.Name}");
							}
						} else {
							ImGui.Indent();
							foreach (var lightInfo in this._ctx.Scene.Children.Where(entity => entity is LightEntity)) {
								ImGui.TextUnformatted($"{lightInfo.Name}");
							}
						}
						ImGui.Unindent();
					}
				if (overlays > 0)
					if (ImGui.CollapsingHeader($"Overlays {overlays}")) {
						if (this._sceneFile != null) {
							ImGui.Checkbox("Load Overlays", ref this._includeOverlays);
							ImGui.Indent();
							foreach (var overlayInfo in this._sceneFile!.Overlays) {
								ImGui.TextUnformatted($"{overlayInfo.Name}");
							}
						} else {
							ImGui.Indent();
							foreach (var overlayInfo in this._ctx.Scene.Children.Where(entity => entity is OverlayEntity)) {
								ImGui.TextUnformatted($"{overlayInfo.Name}");
							}
						}
						ImGui.Unindent();
					}
				
				if (envOver)
					if (ImGui.CollapsingHeader($"Environment")) {
						if (this._sceneFile != null) {
							ImGui.Checkbox("Load Environment", ref this._includeEnv);
							ImGui.Indent();
							var env = ((EnvOverride)this._sceneFile.Environment.Override);
							var list = new List<string> { };
							foreach (var en in Enum.GetValues<EnvOverride>().Except([EnvOverride.None])) {
								if (env == EnvOverride.SkyId) {
									list.Add("Sky");
									continue;
								}
								if (env == EnvOverride.Dust) {
									list.Add("Particles");
									continue;
								}
								if (env.HasFlag(en))
									list.Add(Enum.GetName(en));
								
							}
							var str =  string.Join(", ", list);
							ImGui.TextUnformatted(str);
						} else {
							ImGui.Indent();
							var env = (this._ctx.Scene.GetModule<EnvModule>().Override);
							var list = new List<string> { };
							foreach (var en in Enum.GetValues<EnvOverride>().Except([EnvOverride.None])) {
								if (env == EnvOverride.SkyId) {
									list.Add("Sky");
									continue;
								}
								if (env == EnvOverride.Dust) {
									list.Add("Particles");
									continue;
								}
								if (env.HasFlag(en))
									list.Add(Enum.GetName(en));
							}
							var str =  string.Join(", ", list);
							ImGui.TextUnformatted(str);
						}
						ImGui.Unindent();
					}
				ImGui.EndGroup();

			}
			ImGui.Dummy(new Vector2(2));
		}
		ImGui.PopStyleColor();
		ImGui.PopStyleVar();
	}
}
