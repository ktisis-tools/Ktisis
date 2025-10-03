using System;
using System.Collections.Generic;

using Dalamud.Bindings.ImGui;

using Ktisis.Core;
using Ktisis.Core.Attributes;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Transforms.Types;
using Ktisis.Interface.Editor.Properties;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Scene.Entities;

namespace Ktisis.Interface.Components.Objects;

[Transient]
public class PropertyEditor {
	private readonly DIBuilder _di;

	private readonly PropertyListBuilder _builder = new();

	private readonly List<ObjectPropertyList> _editors = new();
	
	public PropertyEditor(
		DIBuilder di
	) {
		this._di = di;
	}
	
	// Initialize property editors

	public void Prepare(IEditorContext ctx, GuiManager gui) {
		this.Create<ActorPropertyList>(ctx, gui)
			.Create<BasePropertyList>()
			.Create<PosePropertyList>(ctx, gui)
			.Create<LightPropertyList>()
			.Create<ImagePropertyList>(ctx)
			.Create<WeaponPropertyList>()
			.Create<PresetPropertyList>(ctx);
	}

	private PropertyEditor Create<T>(params object[] parameters) where T : ObjectPropertyList {
		Ktisis.Log.Verbose($"Creating property editor: {typeof(T).Name}");
		this._editors.Add(this._di.Create<T>(parameters));
		return this;
	}
	
	// Draw handler

	public void Draw(SceneEntity entity) {
		this._builder.Clear();

		foreach (var editor in this._editors)
			editor.Invoke(this._builder, entity);
		
		foreach (var header in this._builder.Build()) {
			if (!ImGui.CollapsingHeader(header.Name))
				continue;
			try {
				header.Callback.Invoke();
			} catch (Exception err) {
				Ktisis.Log.Error($"Error on '{header.Name}':\n{err.Message}");
				ImGui.Text("Encountered a UI error!\nPlease submit a bug report.");
			}
			ImGui.Spacing();
		}
	}

	private class PropertyListBuilder : IPropertyListBuilder {
		private readonly List<PropertyHeader> _headers = new();
		
		public void Clear() => this._headers.Clear();

		public void AddHeader(string name, Action callback, int priority = int.MinValue) {
			this._headers.Add(new PropertyHeader {
				Name = name,
				Callback = callback,
				Priority = priority == int.MinValue ? this._headers.Count : priority
			});
		}

		public IReadOnlyList<PropertyHeader> Build() {
			this._headers.Sort((a,b) => a.Priority - b.Priority);
			return this._headers.AsReadOnly();
		}

		public class PropertyHeader {
			public required string Name;
			public required Action Callback;
			public required int Priority;
		}
	}
}
