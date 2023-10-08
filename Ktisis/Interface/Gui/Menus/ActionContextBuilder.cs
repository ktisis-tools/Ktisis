using System;
using System.Linq;

using Ktisis.Core;
using Ktisis.Actions;
using Ktisis.Actions.Impl;
using Ktisis.Editing;
using Ktisis.Interface.Input;
using Ktisis.Interface.Localization;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.Game;
using Ktisis.Scene.Objects.Skeleton;

namespace Ktisis.Interface.Gui.Menus; 

[DIService]
public class ActionContextBuilder {
	private readonly ActionManager _actions;
	private readonly InputManager _input;
	private readonly LocaleManager _locale;
	private readonly Editor _editor;
	
	public ActionContextBuilder(
		ActionManager _action,
		InputManager _input,
		LocaleManager _locale,
		Editor _editor
	) {
		this._actions = _action;
		this._input = _input;
		this._locale = _locale;
		this._editor = _editor;
	}

	public ContextMenuFactory GetFactory(string name)
		=> new(name);
	
	// Actions

	public ActionContext GetActionContext<T>() where T : IAction {
		var action = this._actions.Get<T>();
		var name = action.GetName();

		var shortcut = string.Empty;
		if (this._input.TryGetHotkey(name, out var hotkey) && hotkey != null)
			shortcut = hotkey.Keybind.GetShortcutString();
        
		return new ActionContext {
			Name = this._locale.Translate(name),
			Shortcut = shortcut,
			Invoke = () => {
				if (action.CanInvoke())
					action.Invoke();
			}
		};
	}
	
	public ActionContextBuilder AddActionContext<T>(ContextMenuFactory factory) where T : IAction {
		var context = GetActionContext<T>();
		context.AddTo(factory);
		return this;
	}

	public ActionContextBuilder AddActionContext<T>(ContextNodeFactory factory) where T : IAction {
		var context = GetActionContext<T>();
		context.AddTo(factory);
		return this;
	}

	public sealed class ActionContext {
		public required string Name;
		public required string Shortcut;
		public required Action Invoke;

		public void AddTo<T>(IContextNodeFactoryBase<T> factory) {
			factory.AddAction(this.Name, this.Invoke, this.Shortcut);
		}
	}
	
	// Objects

	public ContextMenu BuildFromObject(SceneObject item) {
		var factory = GetFactory("Context_ItemNode");

		factory.AddActionGroup(group => {
			if (!item.IsSelected()) {
				group.AddAction(
					this._locale.Translate("context.select"),
					() => this._editor.Selection.Select(item)
				);
			} else {
				group.AddAction(
					this._locale.Translate("context.unselect"),
					() => this._editor.Selection.Unselect(item)
				);
			}
		});

		if (item is Actor actor)
			AddActorContext(actor, factory);

		if (item is ArmatureNode armNode)
			AddArmatureContext(armNode, factory);
        
		return factory.Create();
	}

	private void AddActorContext(Actor actor, ContextMenuFactory factory) {
        factory.AddActionGroup(group => {
			var chara = this._locale.Translate("file.chara.name");
			var pose = this._locale.Translate("file.pose.name");
			
			group.AddSubMenu(
				this._locale.Translate("file.import"),
				menu => {
					menu.AddAction(chara, _ActionPlaceholder_)
						.AddAction(pose, _ActionPlaceholder_);
				}
			);

			group.AddSubMenu(
				this._locale.Translate("file.export"),
				menu => {
					menu.AddAction(chara, _ActionPlaceholder_)
						.AddAction(pose, _ActionPlaceholder_);
				}
			);
		});
		
		factory.AddActionGroup(group => {
			group.AddAction(
				this._locale.Translate("chara_edit.hint"),
				_ActionPlaceholder_
			);
		});
	}

	private void AddArmatureContext(ArmatureNode node, ContextMenuFactory factory) {
		factory.AddActionGroup(group => {
			group.AddAction(this._locale.Translate("file.pose.ctx_import"), _ActionPlaceholder_)
				.AddAction(this._locale.Translate("file.pose.ctx_export"), _ActionPlaceholder_);
		});

		var importGroup = node is BoneGroup;
		var importSelect = this._editor.Selection.GetSelected()
			.Any(item => item is Bone or BoneGroup);

		if (importGroup || importSelect)
			factory.AddSeparator();
		
		if (importGroup)
			factory.AddAction(this._locale.Translate("file.pose.ctx_import.group"), _ActionPlaceholder_);

		if (importSelect)
			factory.AddAction(this._locale.Translate("file.pose.ctx_import.select"), _ActionPlaceholder_);
	}

	private void _ActionPlaceholder_() { }
}
