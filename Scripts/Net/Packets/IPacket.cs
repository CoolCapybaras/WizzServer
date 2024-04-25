using WizzServer;
using WizzServer.Net;

namespace Net.Packets
{
	public interface IPacket
	{
		public int Id { get; }

		public void Populate(byte[] data);
		public void Serialize(WizzStream stream);
		public ValueTask HandleAsync(Server server, Client client);
	}
}
