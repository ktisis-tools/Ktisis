using GLib.Popups.Context;

using Ktisis.Core.Attributes;
using Ktisis.Editor;
using Ktisis.Editor.Characters;
using Ktisis.Editor.Context;
using Ktisis.Editor.Posing;
using Ktisis.Interface.Types;
using Ktisis.Interface.Windows.Editors;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Interface.Menus;

[Transient]
public class EntityMenuFactory {
	private readonly GuiManager _gui;
	private readonly FileDialogManager _dialog;
	
	public EntityMenuFactory(
		GuiManager gui,
		FileDialogManager dialog
	) {
		this._gui = gui;
		this._dialog = dialog;
	}

	public ContextMenu Build(IEditorContext context, SceneEntity entity) {
		var mediator = new EntityMenuMediator(this, context);
		return new SceneEntityMenu(
			mediator,
			context,
			entity
		).Build();
	}

	public KtisisWindow? OpenEditorFor(IEditorContext context, SceneEntity entity) {
		return entity switch {
			ActorEntity actor => this.OpenEditor<ActorEditWindow, ActorEntity>(context, actor),
			LightEntity light => this.OpenEditor<LightEditWindow, LightEntity>(context, light),
			_ => null
		};
	}

	private T OpenEditor<T, TA>(IEditorContext context, TA entity) where T : EntityEditWindow<TA> where TA : SceneEntity {
		var window = this._gui.GetOrCreate<T>(context);
		window.SetTarget(entity);
		window.Open();
		return window;
	}

	private class EntityMenuMediator(
		EntityMenuFactory factory,
		IEditorContext context
	) : IEntityMenuMediator {
		public void ExportChara(EntityCharaConverter converter)
			=> factory._dialog.ExportCharaFile(converter);
		
		public void ExportPose(EntityPoseConverter converter)
			=> factory._dialog.ExportPoseFile(converter);

		public void OpenEditor<T>(T entity) where T : SceneEntity
			=> factory.OpenEditorFor(context, entity);
		
		public void OpenEditor<T, TA>(TA entity) where T : EntityEditWindow<TA> where TA : SceneEntity
			=> factory.OpenEditor<T, TA>(context, entity);
	}
}
