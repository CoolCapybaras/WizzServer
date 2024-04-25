using SixLabors.ImageSharp;
using System.Buffers.Binary;
using System.Text;

namespace WizzServer.Net
{
	public partial class WizzStream
	{
		public sbyte ReadSignedByte() => (sbyte)this.ReadUnsignedByte();

		public async Task<sbyte> ReadByteAsync() => (sbyte)await this.ReadUnsignedByteAsync();

		public byte ReadUnsignedByte()
		{
			Span<byte> buffer = stackalloc byte[1];
			BaseStream.ReadExactly(buffer);
			return buffer[0];
		}

		public async Task<byte> ReadUnsignedByteAsync()
		{
			var buffer = new byte[1];
			await this.ReadAsync(buffer);
			return buffer[0];
		}

		public bool ReadBoolean()
		{
			return ReadUnsignedByte() == 0x01;
		}

		public async Task<bool> ReadBooleanAsync()
		{
			var value = (int)await this.ReadByteAsync();
			return value switch
			{
				0x00 => false,
				0x01 => true,
				_ => throw new ArgumentOutOfRangeException("Byte returned by stream is out of range (0x00 or 0x01)",
					nameof(BaseStream))
			};
		}

		public ushort ReadUnsignedShort()
		{
			Span<byte> buffer = stackalloc byte[2];
			this.ReadExactly(buffer);
			return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
		}

		public async Task<ushort> ReadUnsignedShortAsync()
		{
			var buffer = new byte[2];
			await this.ReadAsync(buffer);
			return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
		}

		public short ReadShort()
		{
			Span<byte> buffer = stackalloc byte[2];
			this.ReadExactly(buffer);
			return BinaryPrimitives.ReadInt16LittleEndian(buffer);
		}

		public async Task<short> ReadShortAsync()
		{
			using var buffer = new RentedArray<byte>(sizeof(short));
			await this.ReadExactlyAsync(buffer);
			return BinaryPrimitives.ReadInt16LittleEndian(buffer);
		}

		public int ReadInt()
		{
			Span<byte> buffer = stackalloc byte[4];
			this.ReadExactly(buffer);
			return BinaryPrimitives.ReadInt32LittleEndian(buffer);
		}

		public async Task<int> ReadIntAsync()
		{
			using var buffer = new RentedArray<byte>(sizeof(int));
			await this.ReadExactlyAsync(buffer);
			return BinaryPrimitives.ReadInt32LittleEndian(buffer);
		}

		public long ReadLong()
		{
			Span<byte> buffer = stackalloc byte[8];
			this.ReadExactly(buffer);
			return BinaryPrimitives.ReadInt64LittleEndian(buffer);
		}

		public async Task<long> ReadLongAsync()
		{
			using var buffer = new RentedArray<byte>(sizeof(long));
			await this.ReadExactlyAsync(buffer);
			return BinaryPrimitives.ReadInt64LittleEndian(buffer);
		}

		public ulong ReadUnsignedLong()
		{
			Span<byte> buffer = stackalloc byte[8];
			this.ReadExactly(buffer);
			return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
		}

		public async Task<ulong> ReadUnsignedLongAsync()
		{
			using var buffer = new RentedArray<byte>(sizeof(ulong));
			await this.ReadExactlyAsync(buffer);
			return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
		}

		public float ReadFloat()
		{
			Span<byte> buffer = stackalloc byte[4];
			this.ReadExactly(buffer);
			return BinaryPrimitives.ReadSingleLittleEndian(buffer);
		}

		public async Task<float> ReadFloatAsync()
		{
			using var buffer = new RentedArray<byte>(sizeof(float));
			await this.ReadExactlyAsync(buffer);
			return BinaryPrimitives.ReadSingleLittleEndian(buffer);
		}

		public double ReadDouble()
		{
			Span<byte> buffer = stackalloc byte[8];
			this.ReadExactly(buffer);
			return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
		}

		public async Task<double> ReadDoubleAsync()
		{
			using var buffer = new RentedArray<byte>(sizeof(double));
			await this.ReadExactlyAsync(buffer);
			return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
		}

		public string ReadString(int maxLength = 32767)
		{
			var length = ReadVarInt();
			if (length == 0)
				return string.Empty;

			var buffer = new byte[length];
			this.ReadExactly(buffer);

			var value = Encoding.UTF8.GetString(buffer);
			if (maxLength > 0 && value.Length > maxLength)
			{
				throw new ArgumentException($"string ({value.Length}) exceeded maximum length ({maxLength})", nameof(value));
			}
			return value;
		}

		public async Task<string> ReadStringAsync(int maxLength = 32767)
		{
			var length = await this.ReadVarIntAsync();
			using var buffer = new RentedArray<byte>(length);
			if (BitConverter.IsLittleEndian)
			{
				buffer.Span.Reverse();
			}
			await this.ReadExactlyAsync(buffer);

			var value = Encoding.UTF8.GetString(buffer);
			if (maxLength > 0 && value.Length > maxLength)
			{
				throw new ArgumentException($"string ({value.Length}) exceeded maximum length ({maxLength})", nameof(maxLength));
			}
			return value;
		}

		public int ReadVarInt()
		{
			int numRead = 0;
			int result = 0;
			byte read;
			do
			{
				read = this.ReadUnsignedByte();
				int value = read & 0b01111111;
				result |= value << (7 * numRead);

				numRead++;
				if (numRead > 5)
				{
					throw new InvalidOperationException("VarInt is too big");
				}
			} while ((read & 0b10000000) != 0);

			return result;
		}

		public virtual async Task<int> ReadVarIntAsync()
		{
			int numRead = 0;
			int result = 0;
			byte read;
			do
			{
				read = await this.ReadUnsignedByteAsync();
				int value = read & 0b01111111;
				result |= value << (7 * numRead);

				numRead++;
				if (numRead > 5)
				{
					throw new InvalidOperationException("VarInt is too big");
				}
			} while ((read & 0b10000000) != 0);

			return result;
		}

		public byte[] ReadUInt8Array(int length = 0)
		{
			if (length == 0)
				length = ReadVarInt();

			var result = new byte[length];
			if (length == 0)
				return result;

			int n = length;
			while (true)
			{
				n -= Read(result, length - n, n);
				if (n == 0)
					break;
			}
			return result;
		}

		public async Task<byte[]> ReadUInt8ArrayAsync(int length = 0)
		{
			if (length == 0)
				length = await this.ReadVarIntAsync();

			var result = new byte[length];
			if (length == 0)
				return result;

			int n = length;
			while (true)
			{
				n -= await this.ReadAsync(result, length - n, n);
				if (n == 0)
					break;
			}
			return result;
		}

		public async Task<byte> ReadUInt8Async()
		{
			int value = await this.ReadByteAsync();
			if (value == -1)
				throw new EndOfStreamException();
			return (byte)value;
		}

		public long ReadVarLong()
		{
			int numRead = 0;
			long result = 0;
			byte read;
			do
			{
				read = this.ReadUnsignedByte();
				int value = (read & 0b01111111);
				result |= (long)value << (7 * numRead);

				numRead++;
				if (numRead > 10)
				{
					throw new InvalidOperationException("VarLong is too big");
				}
			} while ((read & 0b10000000) != 0);

			return result;
		}

		public async Task<long> ReadVarLongAsync()
		{
			int numRead = 0;
			long result = 0;
			byte read;
			do
			{
				read = await this.ReadUnsignedByteAsync();
				int value = (read & 0b01111111);
				result |= (long)value << (7 * numRead);

				numRead++;
				if (numRead > 10)
				{
					throw new InvalidOperationException("VarLong is too big");
				}
			} while ((read & 0b10000000) != 0);

			return result;
		}

		public byte[] ReadByteArray()
		{
			var length = ReadVarInt();
			return ReadUInt8Array(length);
		}

		public Image ReadImage()
		{
			var length = ReadVarInt();
			if (length == 0)
				return null!;

			var buffer = new byte[length];
			this.ReadExactly(buffer);

			return Image.Load(buffer);
		}
	}
}
