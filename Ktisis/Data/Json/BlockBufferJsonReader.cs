using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Ktisis.Data.Json;

/**
 * <summary>Synchronous JSON-Reader-over-Stream abstraction that uses a potentially stack-allocated block buffer.</summary>
 */
public ref struct BlockBufferJsonReader {

	private enum State {
		INIT,
		READING,
		FINAL_READ,
		CLOSED
	}

	public Utf8JsonReader Reader = default;
	private readonly Stream stream;
	private readonly Span<byte> blockBuffer;
	private Span<byte> readSlice = default;
	private JsonReaderState jsonState;
	private State state = State.INIT;

	public BlockBufferJsonReader(Stream stream, Span<byte> blockBuffer, JsonReaderOptions options) {
		this.stream = stream;
		this.blockBuffer = blockBuffer;
		jsonState = new JsonReaderState(options);
	}

	public bool Read() {
		switch (state) {
			case State.CLOSED:
				return false;
			case State.READING:
			case State.FINAL_READ:
				if (Reader.Read()) return true;
				if (state == State.FINAL_READ) {
					state = State.CLOSED;
					return false;
				}
				goto case State.INIT;
			case State.INIT:
				acquireReader();
				goto case State.READING;
		}
		Debug.Assert(false, "This point is unreachable");
		throw new Exception("This point is unreachable");
	}

	private void acquireReader() {
		int preRead = 0;
		Debug.Assert(state != State.FINAL_READ, "Shouldn't be in final read here");
		if (state != State.INIT) {
			if (Reader.BytesConsumed == 0)
				throw new Exception("JSON value appears to exceed the bounds of the block buffer. Increase the buffer size or decrease your JSON value size.");
			jsonState = Reader.CurrentState;
			Span<byte> remainingSlice = readSlice[(int) Reader.BytesConsumed..];
			remainingSlice.CopyTo(blockBuffer);
			preRead = remainingSlice.Length;
		}

		int read = stream.Read(blockBuffer[preRead..]);
		readSlice = blockBuffer[..(preRead+read)];
		state = readSlice.Length == 0 ? State.FINAL_READ : State.READING;

		Reader = new Utf8JsonReader(readSlice, readSlice.Length == 0, jsonState);
	}
}
