namespace WizzServer.Net
{
	public partial class WizzStream : Stream
	{
		private bool disposed;

		public Stream BaseStream { get; set; }

		public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);

		public override bool CanRead => BaseStream.CanRead;

		public override bool CanSeek => BaseStream.CanSeek;

		public override bool CanWrite => BaseStream.CanWrite;

		public override long Length => BaseStream.Length;

		public override long Position
		{
			get => BaseStream.Position;
			set => BaseStream.Position = value;
		}

		public WizzStream()
		{
			this.BaseStream = new MemoryStream();
		}

		public WizzStream(Stream stream)
		{
			this.BaseStream = stream;
		}

		public WizzStream(byte[] data)
		{
			this.BaseStream = new MemoryStream(data);
		}

		public override void Flush() => this.BaseStream.Flush();

		public override int Read(byte[] buffer, int offset, int count) => this.BaseStream.Read(buffer, offset, count);

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			try
			{
				var read = await BaseStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

				return read;
			}
			catch (Exception)
			{
				return 0;
			}//TODO better handling of this
		}

		public virtual async Task<int> ReadAsync(byte[] buffer, CancellationToken cancellationToken = default)
		{
			try
			{
				var read = await this.BaseStream.ReadAsync(buffer, cancellationToken);

				return read;
			}
			catch (Exception)
			{
				return 0;
			}//TODO better handling of this
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			await BaseStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
		}

		public virtual async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
		{
			await this.BaseStream.WriteAsync(buffer, cancellationToken);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			this.BaseStream.Write(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) => this.BaseStream.Seek(offset, origin);

		public override void SetLength(long value) => this.BaseStream.SetLength(value);

		protected override void Dispose(bool disposing)
		{
			if (this.disposed)
				return;

			if (disposing)
			{
				this.BaseStream.Dispose();
				this.Lock.Dispose();
			}

			this.disposed = true;
		}
	}
}
