using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Style;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using GLib.Widgets;

using Ktisis.Data.Files;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Types;
using Ktisis.Services.Data;
using Ktisis.Services.Plugin;

using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

using Microsoft.Extensions.DependencyInjection;

namespace Ktisis.Interface.Windows.Editors;

public class SceneWindow : KtisisWindow {

	private readonly SceneDataService _sceneDataService;
	private readonly ISceneManager _scene;
	private readonly IEditorContext _ctx;
	private readonly ITextureProvider _textureProvider;
	private readonly IDataManager _dataManager;
	
	private bool autosave = false;
	private SceneFile? _sceneFile;
	private ISharedImmediateTexture? _texture;
	private Map _source;
	
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
	}
	
	public override void PreOpenCheck() {
		if (this._scene.IsValid) return;
		Ktisis.Log.Verbose("State for scene editor is stale, closing...");
		this.Close();
	}
	
	public override void PreDraw() {
		base.PreDraw();
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(400, 300),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.90f
		};
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

	public void TestMCDFBeforeLoad() {
		foreach (var entity in this._sceneFile.Actors) {
			var ignored = false;
			while(entity.MCDF != string.Empty && !Path.Exists(entity.MCDF) && !ignored) {
				DrawPopupModal(entity);
			}
		}
		this._sceneDataService.Load(this._sceneFile);
		this._sceneFile = null;
	}

	public bool DrawPopupModal(SceneFile.ActorInfo entity) {
		using (var popup = ImRaii.PopupModal("MCDF not found!###MCDFWarn")) {
			if (popup.Success) {
				using var wrap = ImRaii.TextWrapPos(ImGui.GetWindowContentRegionMax().X);
				ImGui.TextUnformatted($"The MCDF linked to the actor {entity.Chara.Nickname} wasn't found, do you want select a file to load for them?");
				ImGui.SetCursorPos(new Vector2(ImGui.GetContentRegionAvail().Y * .80f,ImGui.GetContentRegionAvail().X * .25f));
				if (ImGui.Button("Pick File")) {
					this._ctx.Interface.OpenMcdfFile((s => {
						var f = this._sceneFile.Actors.Find(e => e.Index == entity.Index);
						f.MCDF = s;
					}));
					return true;
				}
				if (ImGui.Button("Ignore")) {
					return true;
				}
			}

		}
		return false;
	}
	
	public unsafe override void Draw() {
		var style = ImGui.GetStyle();
		var iconSize = UiBuilder.DefaultFontSizePx * ImGuiHelpers.GlobalScale * 2;
		var iconBtnSize = new Vector2(iconSize, iconSize);
		int cameras, actors, lights;
		if (this._sceneFile != null) {
			actors = this._sceneFile.Actors.Count;
			cameras = this._sceneFile.Cameras.Count;
			lights = this._sceneFile.Lights.Count;
		} else {
			actors = this._ctx.Scene.Children.Count(entity => entity is CharaEntity);
			lights = this._ctx.Scene.Children.Count(entity => entity is LightEntity);
			cameras = this._ctx.Cameras.GetCameras().Count();
		}

		ImGui.BeginGroup();
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.PersonBurst, "Load Scene file", iconBtnSize))
			this._ctx.Interface.OpenSceneFile(s => this._sceneFile = this._ctx.Scene.Data.LoadFile(s));
			//this._ctx.Interface.OpenSceneFile((s => this._ctx.Scene.Data.Load(this._ctx.Scene.Data., this.autosave)));
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Save, "Save Scene file", iconBtnSize))
			this._ctx.Interface.ExportSceneFile((this._ctx.Scene.Data.Save()));

		if (this._sceneFile != null) {
			ImGui.SetCursorPosY(ImGui.GetWindowHeight() -(iconBtnSize.Y * 2.5f));  //space for 2 buttons?
			if (Buttons.IconButtonTooltip(this.autosave ? FontAwesomeIcon.Globe : FontAwesomeIcon.HouseChimney, "Choose coordinate type", iconBtnSize))
				this.autosave = !this.autosave;
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Check, "Apply Scene", iconBtnSize))
				this.TestMCDFBeforeLoad();
		}


		ImGui.EndGroup();
		
		ImGui.SameLine();

		ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(74f, 74f, 74f, 138f)/255);
		ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4 );
		using (var child = ImRaii.Child("##SceneData", Vector2.Zero,false)) {
			// Check if this child is drawing
			var cursorPos = ImGui.GetCursorScreenPos();
			if (child.Success) {

				var dl = ImGui.GetWindowDrawList();
				dl.AddImageRounded(this._texture.GetWrapOrEmpty().Handle, cursorPos,new Vector2(ImGui.GetContentRegionAvail().X*.64f, ImGui.GetContentRegionAvail().X*.36f)+cursorPos, Vector2.Zero, Vector2.One, 0xFFFFFFFF, 4f  );
				//ImGui.Image(this._texture.GetWrapOrEmpty().Handle, );
				ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X*.64f + 2f);
				using var wrap = ImRaii.TextWrapPos(ImGui.GetWindowContentRegionMax().X);
				ImGui.PushStyleColor(ImGuiCol.Text, this._source.RowId == this._sceneDataService.GetCurrentMapID()? ImGuiColors.HealerGreen : ImGuiColors.DPSRed);
				ImGui.BeginGroup();
				ImGui.TextUnformatted($"From: {this._source.PlaceName.Value.Name}");
				ImGui.PopStyleColor();
				ImGui.TextUnformatted($"Actors: {actors}\nCameras: {cameras}\nLights: {lights}");
				ImGui.EndGroup();
			}
		}
		ImGui.PopStyleColor();
		ImGui.PopStyleVar();
	}
	

}
