﻿using Net.Packets;
using Net.Packets.Clientbound;
using Net.Packets.Serverbound;
using SixLabors.ImageSharp;
using System.Net;
using System.Net.Sockets;
using WizzServer.Net;

namespace WizzServer
{
	public class Client : IDisposable
	{
		private Server server;
		private Socket socket;
		private NetworkStream networkStream;
		private WizzStream wizzStream;
		private bool disposed;

		public int Id { get; set; }
		public bool IsAuthed { get; set; }
		public string Name { get; set; }
		public Image Image { get; set; }
		public Room Room { get; set; }

		public Client(Server server, Socket socket, int id)
		{
			this.socket = socket;
			this.server = server;
			this.Id = id;

			this.networkStream = new NetworkStream(this.socket);
			this.wizzStream = new WizzStream(this.networkStream);
		}

		public async Task Start()
		{
			while (true)
			{
				(var id, var data) = await GetNextPacketAsync();

				if (id == 0)
					break;

				switch (id)
				{
					case 2:
						await HandleFromPoolAsync<AuthPacket>(data, this);
						break;
					case 3:
						await HandleFromPoolAsync<SearchPacket>(data, this);
						break;
					case 4:
						await HandleFromPoolAsync<CreateLobbyPacket>(data, this);
						break;
					case 5:
						await HandleFromPoolAsync<JoinLobbyPacket>(data, this);
						break;
					case 6:
						await HandleFromPoolAsync<LeaveLobbyPacket>(data, this);
						break;
					case 7:
						await HandleFromPoolAsync<StartGamePacket>(data, this);
						break;
					case 8:
						await HandleFromPoolAsync<AnswerGamePacket>(data, this);
						break;
					case 9:
						await HandleFromPoolAsync<UpdateProfilePacket>(data, this);
						break;
					case 10:
						await HandleFromPoolAsync<EditQuizPacket>(data, this);
						break;
				}
			}

			OnDisconnect();
			this.Dispose();
		}

		private async Task<(int id, byte[] data)> GetNextPacketAsync()
		{
			var length = await wizzStream.ReadVarIntAsync();
			var receivedData = new byte[length];

			await wizzStream.ReadExactlyAsync(receivedData.AsMemory(0, length));

			var packetId = 0;
			var packetData = Array.Empty<byte>();

			await using (var packetStream = new WizzStream(receivedData))
			{
				try
				{
					packetId = await packetStream.ReadVarIntAsync();
					var arlen = 0;

					if (length - packetId.GetVarIntLength() > -1)
						arlen = length - packetId.GetVarIntLength();

					packetData = new byte[arlen];
					_ = await packetStream.ReadAsync(packetData.AsMemory(0, packetData.Length));
				}
				catch
				{
					throw;
				}
			}

			return (packetId, packetData);
		}

		private async Task HandleFromPoolAsync<T>(byte[] data, Client client) where T : IPacket, new()
		{
			var packet = ObjectPool<T>.Shared.Rent();
			try
			{
				packet.Populate(data);
				await packet.HandleAsync(server, this);
			}
			catch (Exception e)
			{
				Logger.LogError(e.ToString());
			}
			ObjectPool<T>.Shared.Return(packet);
		}

		public void OnConnect()
		{
			server.Clients.TryAdd(Id, this);
		}

		public void Disconnect() => wizzStream.Dispose();

		public void OnDisconnect()
		{
			server.AuthTokenManager.RemoveToken(this);
			Room?.OnClientLeave(this);
			server.Clients.TryRemove(Id, out _);

			if (!IsAuthed) return;

			Logger.LogInfo($"{Name} has left the server!");
		}

		public void SendPacket(IPacket packet)
		{
			packet.Serialize(wizzStream);
		}

		public void SendMessage(string text, int type = 0)
		{
			SendPacket(new MessagePacket(type, text));
		}

		public string GetIP() => ((IPEndPoint)socket.RemoteEndPoint!).Address.ToString();

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;

			wizzStream.Dispose();
			socket.Dispose();
			Image?.Dispose();

			GC.SuppressFinalize(this);
		}
	}
}
