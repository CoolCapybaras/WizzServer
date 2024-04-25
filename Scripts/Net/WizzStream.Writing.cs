using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Buffers.Binary;
using System.Text;

namespace WizzServer.Net
{
	public partial class WizzStream
	{
		public void WriteByte(sbyte value)
		{
			BaseStream.WriteByte((byte)value);
		}

		public async Task WriteByteAsync(sbyte value)
		{
			await WriteUnsignedByteAsync((byte)value);
		}

		public void WriteUnsignedByte(byte value)
		{
			BaseStream.WriteByte(value);
		}

		public async Task WriteUnsignedByteAsync(byte value)
		{
			await WriteAsync([value]);
		}

		public void WriteBoolean(bool value)
		{
			BaseStream.WriteByte((byte)(value ? 0x01 : 0x00));
		}

		public async Task WriteBooleanAsync(bool value)
		{
			await WriteByteAsync((sbyte)(value ? 0x01 : 0x00));
		}

		public void WriteUnsignedShort(ushort value)
		{
			Span<byte> span = stackalloc byte[2];
			BinaryPrimitives.WriteUInt16LittleEndian(span, value);
			BaseStream.Write(span);
		}

		public async Task WriteUnsignedShortAsync(ushort value)
		{
			using var write = new RentedArray<byte>(sizeof(ushort));
			BinaryPrimitives.WriteUInt16LittleEndian(write, value);
			await WriteAsync(write);
		}

		public void WriteShort(short value)
		{
			Span<byte> span = stackalloc byte[2];
			BinaryPrimitives.WriteInt16LittleEndian(span, value);
			BaseStream.Write(span);
		}

		public async Task WriteShortAsync(short value)
		{
			using var write = new RentedArray<byte>(sizeof(short));
			BinaryPrimitives.WriteInt16LittleEndian(write, value);
			await WriteAsync(write);
		}

		public void WriteInt(int value)
		{
			Span<byte> span = stackalloc byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(span, value);
			BaseStream.Write(span);
		}

		public async Task WriteIntAsync(int value)
		{
			using var write = new RentedArray<byte>(sizeof(int));
			BinaryPrimitives.WriteInt32LittleEndian(write, value);
			await WriteAsync(write);
		}

		public void WriteLong(long value)
		{
			Span<byte> span = stackalloc byte[8];
			BinaryPrimitives.WriteInt64LittleEndian(span, value);
			BaseStream.Write(span);
		}

		public async Task WriteLongAsync(long value)
		{
			using var write = new RentedArray<byte>(sizeof(long));
			BinaryPrimitives.WriteInt64LittleEndian(write, value);
			await WriteAsync(write);
		}

		public void WriteFloat(float value)
		{
			Span<byte> span = stackalloc byte[4];
			BinaryPrimitives.WriteSingleLittleEndian(span, value);
			BaseStream.Write(span);
		}

		public async Task WriteFloatAsync(float value)
		{
			using var write = new RentedArray<byte>(sizeof(float));
			BinaryPrimitives.WriteSingleLittleEndian(write, value);
			await WriteAsync(write);
		}

		public void WriteDouble(double value)
		{
			Span<byte> span = stackalloc byte[8];
			BinaryPrimitives.WriteDoubleLittleEndian(span, value);
			BaseStream.Write(span);
		}

		public async Task WriteDoubleAsync(double value)
		{
			using var write = new RentedArray<byte>(sizeof(double));
			BinaryPrimitives.WriteDoubleLittleEndian(write, value);
			await WriteAsync(write);
		}

		public void WriteString(string value, int maxLength = short.MaxValue)
		{
			if (string.IsNullOrEmpty(value))
			{
				WriteVarInt(0);
				return;
			}

			System.Diagnostics.Debug.Assert(value.Length <= maxLength);

			using var bytes = new RentedArray<byte>(Encoding.UTF8.GetByteCount(value));
			Encoding.UTF8.GetBytes(value, bytes.Span);
			WriteVarInt(bytes.Length);
			Write(bytes);
		}

		public void WriteNullableString(string? value, int maxLength = short.MaxValue)
		{
			if (value is null)
				return;

			System.Diagnostics.Debug.Assert(value.Length <= maxLength);

			using var bytes = new RentedArray<byte>(Encoding.UTF8.GetByteCount(value));
			Encoding.UTF8.GetBytes(value, bytes.Span);
			WriteVarInt(bytes.Length);
			Write(bytes);
		}

		public async Task WriteStringAsync(string value, int maxLength = short.MaxValue)
		{
			//await Globals.PacketLogger.LogDebugAsync($"Writing String ({value})");

			ArgumentNullException.ThrowIfNull(value);

			if (value.Length > maxLength)
				throw new ArgumentException($"string ({value.Length}) exceeded maximum length ({maxLength})", nameof(value));

			using var bytes = new RentedArray<byte>(Encoding.UTF8.GetByteCount(value));
			Encoding.UTF8.GetBytes(value, bytes.Span);
			await WriteVarIntAsync(bytes.Length);
			await WriteAsync(bytes);
		}

		public void WriteVarInt(int value)
		{
			var unsigned = (uint)value;

			do
			{
				var temp = (byte)(unsigned & 127);
				unsigned >>= 7;

				if (unsigned != 0)
					temp |= 128;

				BaseStream.WriteByte(temp);
			}
			while (unsigned != 0);
		}

		public async Task WriteVarIntAsync(int value)
		{
			//await Globals.PacketLogger.LogDebugAsync($"Writing VarInt ({value})");

			var unsigned = (uint)value;

			do
			{
				var temp = (byte)(unsigned & 127);

				unsigned >>= 7;

				if (unsigned != 0)
					temp |= 128;

				await WriteUnsignedByteAsync(temp);
			}
			while (unsigned != 0);
		}

		public void WriteVarInt(Enum value)
		{
			WriteVarInt(Convert.ToInt32(value));
		}

		/// <summary>
		/// Writes a "VarInt Enum" to the specified <paramref name="stream"/>.
		/// </summary>
		public async Task WriteVarIntAsync(Enum value) => await WriteVarIntAsync(Convert.ToInt32(value));

		public void WriteLongArray(long[] values)
		{
			Span<byte> buffer = stackalloc byte[8];
			for (int i = 0; i < values.Length; i++)
			{
				BinaryPrimitives.WriteInt64LittleEndian(buffer, values[i]);
				BaseStream.Write(buffer);
			}
		}

		public async Task WriteLongArrayAsync(long[] values)
		{
			foreach (var value in values)
				await WriteLongAsync(value);
		}

		public async Task WriteLongArrayAsync(ulong[] values)
		{
			foreach (var value in values)
				await WriteLongAsync((long)value);
		}

		public void WriteVarLong(long value)
		{
			var unsigned = (ulong)value;

			do
			{
				var temp = (byte)(unsigned & 127);

				unsigned >>= 7;

				if (unsigned != 0)
					temp |= 128;


				BaseStream.WriteByte(temp);
			}
			while (unsigned != 0);
		}

		public async Task WriteVarLongAsync(long value)
		{
			var unsigned = (ulong)value;

			do
			{
				var temp = (byte)(unsigned & 127);

				unsigned >>= 7;

				if (unsigned != 0)
					temp |= 128;


				await WriteUnsignedByteAsync(temp);
			}
			while (unsigned != 0);
		}

		public void WriteByteArray(byte[] values)
		{
			BaseStream.Write(values);
		}

		public void WriteImage(Image image)
		{
			if (image is null)
			{
				WriteVarInt(0);
				return;
			}

			using var stream = new MemoryStream();
			image.SaveAsJpeg(stream);
			WriteVarInt((int)stream.Length);

			stream.Position = 0;
			stream.CopyTo(BaseStream);
		}
	}
}
