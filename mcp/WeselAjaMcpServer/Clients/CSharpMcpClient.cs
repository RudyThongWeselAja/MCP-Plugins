using ModelContextProtocol.Client;

namespace WeselAjaMcpServer.Clients;

public sealed class CSharpMcpClient
{
    public McpClient Client { get; }

    public CSharpMcpClient(McpClient client)
    {
        Client = client;
    }
}