using System.Buffers.Binary;
using System.Text;

namespace WizzServer.Net
{
	public partial class WizzStream
	{
		public async Task WriteByteAsync(byte value)
		{
			await WriteAsync([value]);
		}

		public void WriteBoolean(bool value)
		{
			BaseStream.WriteByte((byte)(value ? 0x01 : 0x00));
		}

		public async Task WriteBooleanAsync(bool value)
		{
			await WriteByteAsync((byte)(value ? 0x01 : 0x00));
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
				WriteByte(0);
				return;
			}

			System.Diagnostics.Debug.Assert(value.Length <= maxLength);

			using var bytes = new RentedArray<byte>(Encoding.UTF8.GetByteCount(value));
			Encoding.UTF8.GetBytes(value, bytes.Span);
			WriteVarInt(bytes.Length);
			Write(bytes);
		}

		public async Task WriteStringAsync(string value, int maxLength = short.MaxValue)
		{
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
			var unsigned = (uint)value;

			do
			{
				var temp = (byte)(unsigned & 127);

				unsigned >>= 7;

				if (unsigned != 0)
					temp |= 128;

				await WriteByteAsync(temp);
			}
			while (unsigned != 0);
		}

		public void WriteVarInt(Enum value)
		{
			WriteVarInt(Convert.ToInt32(value));
		}

		public async Task WriteVarIntAsync(Enum value) => await WriteVarIntAsync(Convert.ToInt32(value));

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


				await WriteByteAsync(temp);
			}
			while (unsigned != 0);
		}

		public void WriteByteArray(byte[] values)
		{
			BaseStream.Write(values);
		}

		public void WriteImage(byte[] image)
		{
			if (image is null)
			{
				WriteByte(0);
				return;
			}

			WriteVarInt(image.Length);
			WriteByteArray(image);
		}
	}
}
