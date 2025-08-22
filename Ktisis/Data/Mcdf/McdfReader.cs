using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

using K4os.Compression.LZ4.Legacy;

namespace Ktisis.Data.Mcdf;

public sealed class McdfReader : IDisposable {
	private readonly FileStream _stream;
	private readonly LZ4Stream _lz4;
	private readonly McdfHeader _header;
	
	private McdfReader(
		FileStream stream,
		LZ4Stream lz4,
		McdfHeader header
	) {
		this._stream = stream;
		this._lz4 = lz4;
		this._header = header;
	}
	
	public static McdfReader FromPath(string path) {
		var stream = File.OpenRead(path);
		var lz4 = new LZ4Stream(stream, LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression);
		var header = ReadHeader(path, lz4);
		if (header == null)
			throw new Exception($"'{Path.GetFileName(path)}' is not a valid MCDF file.");
		return new McdfReader(stream, lz4, header);
	}
	
	// Header parsing
	
	private const uint MareMagic = 0x4644434D;

	private static McdfHeader? ReadHeader(string path, LZ4Stream lz4) {
		var br = new BinaryReader(lz4);

		var magic = br.ReadUInt32();
		if (magic != MareMagic) return null;

		var version = br.ReadByte();
		if (version != 1) return null;

		var len = br.ReadInt32();
		var bytes = br.ReadBytes(len);
		var data = Encoding.UTF8.GetString(bytes);
		return new McdfHeader {
			Version = version,
			FilePath = path,
			Data = JsonSerializer.Deserialize<McdfData>(data)!
		};
	}
	
	// Data

	public McdfData GetData() => this._header.Data;

	public Dictionary<string, string> Extract(string dir) {
		using var br = new BinaryReader(this._lz4);
		
		var files = new Dictionary<string, string>();
		
		foreach (var fileData in this._header.Data.Files) {
			var filePath = Path.Combine(dir, $"ktisis_{fileData.Hash}.tmp");

			using var ws = File.OpenWrite(filePath);
			using var bw = new BinaryWriter(ws);

			var bytes = br.ReadBytes(fileData.Length);
			bw.Write(bytes);
			bw.Flush();

			foreach (var gamePath in fileData.GamePaths) {
				files[gamePath] = filePath;
				Ktisis.Log.Debug($"{gamePath} => {Path.GetFileName(filePath)}");
			}
		}

		return files;
	}
	
	// IDisposable

	public void Dispose() {
		this._lz4.Dispose();
		this._stream.Dispose();
	}
}
