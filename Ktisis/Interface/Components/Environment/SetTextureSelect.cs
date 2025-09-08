using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;

namespace Ktisis.Interface.Components.Environment;

[Transient]
public class SetTextureSelect {
	public delegate string ResolvePathHandler(uint id);
	
	private readonly ITextureProvider _texture;
	
	public SetTextureSelect(
		ITextureProvider texture
	) {
		this._texture = texture;
	}

	public bool Draw(
		string name,
		ref uint value,
		ResolvePathHandler resolve
	) {
		using var _id = ImRaii.PushId($"##TexSelect_{name}");
		
		var result = false;
		
		var path = resolve.Invoke(value);
		var img = this._texture.GetFromGame(path);
		if (this.DrawButton(value, img, ButtonSize))
			this.OpenPopup(name, resolve);
		result |= this.DrawPopup(name, ref value);

		ImGui.SameLine();

		using var _group = ImRaii.Group();
		ImGui.Text(name);
		ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - ImGui.GetCursorPosX());
		result |= Widgets.InputUInt.Draw($"##{name}", ref value);
		return result;
	}
	
	// Buttons

	private readonly static Vector2 ButtonSize = new(48, 48);
	private readonly static Vector2 OptionSize = new(64, 64);

	private bool DrawButton(uint value, ISharedImmediateTexture? image, Vector2 size) {
		using var _col0 = ImRaii.PushColor(ImGuiCol.Button, 0);
		using var _col1 = ImRaii.PushColor(ImGuiCol.ButtonHovered, 0x6C3A3A3A);
		using var _col2 = ImRaii.PushColor(ImGuiCol.ButtonActive, 0x9C3A3A3A);
		using var _pad = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
		if (image == null)
			return ImGui.Button($"{value:D3}", size);
		return ImGui.ImageButton(image.GetWrapOrEmpty().Handle, size);
	}
	
	// Popup

	private bool _opening;
	private OptionsPopupResource? Options;
	
	private void OpenPopup(string name, ResolvePathHandler resolve) {
		this.Options?.Dispose();
		this.Options = null;

		this.Options = new OptionsPopupResource();
		this.Options.Load(this._texture, resolve);
		
		ImGui.OpenPopup($"{name}_Popup");
		this._opening = true;
	}

	private bool DrawPopup(string name, ref uint value) {
		if (this.Options == null) return false;

		const int maxColumns = 6;
		const int maxRows = 4;

		var style = ImGui.GetStyle();
		ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(
			(OptionSize.X + style.ItemSpacing.X) * maxColumns + style.ItemInnerSpacing.X + style.ScrollbarSize,
			(OptionSize.Y + style.ItemSpacing.Y) * maxRows + style.WindowPadding.Y
		));
		using var _popup = ImRaii.Popup($"{name}_Popup", ImGuiWindowFlags.AlwaysAutoResize);
		if (!_popup.Success) {
			if (this._opening) return false;
			this.Options?.Dispose();
			this.Options = null;
			return false;
		}

		this._opening = false;
		
		var i = 0;
		var result = false;
		foreach (var option in this.Options.Get()) {
			if (i++ % 6 != 0 && i > 1) ImGui.SameLine();
			if (this.DrawButton(option.Value, option.Texture, OptionSize)) {
				value = option.Value;
				result = true;
			}
		}
        
		return result;
	}
	
	// Texture options

	private class OptionsPopupResource : IDisposable {
		private readonly CancellationTokenSource Source = new();
		private readonly List<Option> List = new();

		public IEnumerable<Option> Get() {
			IEnumerable<Option> result;
			lock (this.List)
				result = this.List.ToList();
			return result;
		}

		public void Load(
			ITextureProvider tex,
			ResolvePathHandler resolve
		) {
			var values = Enumerable.Range(0, 1000)
				.Select(i => (uint)i)
				.Select(i => {
					var path = resolve.Invoke(i);
					var icon = tex.GetFromGame(path);
					if (icon == null && i > 0) return null;
					return new Option {
						Value = i,
						Texture = icon
					};
				})
				.Where(opt => opt != null)
				.Cast<Option>();
			
			this.LoadAsync(values, this.Source.Token).ContinueWith(task => {
				if (task.Exception != null)
					Ktisis.Log.Error(task.Exception.ToString());
			});
		}

		private async Task LoadAsync(
			IEnumerable<Option> values,
			CancellationToken token
		) {
			await Task.Yield();
			
			// IDalamudTextureWrap is blocking regardless of thread, so just stagger the rate they're loaded at.
			var t = new Stopwatch();
			t.Start();
			foreach (var chunk in values.Chunk(5)) {
				var time = t.Elapsed.TotalMilliseconds;
				lock (this.List) {
					if (token.IsCancellationRequested) break;
					this.List.AddRange(chunk);
				}
				await Task.Delay(Math.Min((int)time, 100), token);
				t.Restart();
			}
			token.ThrowIfCancellationRequested();
		}

		public void Dispose() {
			lock (this.List) {
				this.Source.Cancel();
				this.List.Clear();
			}
			this.Source.Dispose();
		}
	}

	private class Option {
		public required uint Value;
		public required ISharedImmediateTexture? Texture;
	}
}
