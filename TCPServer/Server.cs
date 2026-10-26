using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TCPServer
{
    internal class Server
    {
        public const int Port = 5000;

        private readonly TcpListener _listener = new(IPAddress.Any, Port);
        // ConcurrentDictionary because client handlers run on multiple threads.
        private readonly ConcurrentDictionary<TcpClient, byte> _clients = new();

        public async Task RunAsync()
        {
            _listener.Start();
            Console.WriteLine($"Server listening on port {Port}");

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
                _clients.TryAdd(client, 0);

                // Fire and forget: each client is served independently.
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);

                // Messages are newline-delimited UTF-8 text; null means the client disconnected.
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    Console.WriteLine($"Received: {line}");
                    await BroadcastAsync(line, sender: client);
                }
            }
            catch (IOException)
            {
                // Connection dropped abruptly; treat as a disconnect.
            }
            finally
            {
                _clients.TryRemove(client, out _);
                client.Dispose();
                Console.WriteLine("Client disconnected");
            }
        }

        private async Task BroadcastAsync(string message, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");

            foreach (TcpClient client in _clients.Keys)
            {
                if (client == sender) continue;

                try
                {
                    await client.GetStream().WriteAsync(data);
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
                {
                    // That client's own handler will clean it up.
                }
            }
        }
    }
}
