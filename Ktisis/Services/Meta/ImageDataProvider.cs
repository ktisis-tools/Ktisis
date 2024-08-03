using System.IO;

using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

using GLib.Popups.ImFileDialog;
using GLib.Popups.ImFileDialog.Data;

using Ktisis.Core.Attributes;

namespace Ktisis.Services.Meta;

[Singleton]
public class ImageDataProvider {
	private readonly ITextureProvider _tex;
	private readonly FileMetaHandler _handler;
	
	public ImageDataProvider(
		ITextureProvider tex
	) {
		this._tex = tex;
		this._handler = new FileMetaHandler(tex);
	}

	public void Initialize() {
		this._handler.AddFileType("*", this.BuildMeta);
	}
	
	public void BindMetadata(FileDialog dialog) => dialog.WithMetadata(this._handler);
	
	public ISharedImmediateTexture GetFromFile(string path) => this._tex.GetFromFile(path);

	private FileMeta BuildMeta(string path) {
		var texture = this.GetFromFile(path);
		var name = Path.GetFileName(path);
		return new FileMeta(name) {
			Texture = texture
		};
	}
}
