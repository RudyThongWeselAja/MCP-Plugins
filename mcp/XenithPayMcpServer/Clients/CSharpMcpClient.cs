using ModelContextProtocol.Client;

namespace XenithPayMcpServer.Clients;

public sealed class CSharpMcpClient
{
    public McpClient Client { get; }

    public CSharpMcpClient(McpClient client)
    {
        Client = client;
    }
}