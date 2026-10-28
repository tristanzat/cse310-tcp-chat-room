using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Shared;

namespace TCPServer
{
    internal class Server
    {
        public const int Port = 5000;
        private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(5);

        // Listen on all network interfaces because it's easy. We'll use a handshake to ensure
        // only our TCP client can send messages.
        private readonly TcpListener _listener = new(IPAddress.Any, Port);
        // ConcurrentDictionary because client handlers run on multiple threads.
        private readonly ConcurrentDictionary<TcpClient, byte> _clients = new();

        public async Task RunAsync()
        {
            _listener.Start();
            Console.WriteLine($"Server listening on port {Port}");

            while (true)
            {
                // Get our TCP client
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine($"Connection from {client.Client.RemoteEndPoint}");

                // Fire and forget: each client is served independently.
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);

                string? username = await ReadHandshakeAsync(reader);
                if (username == null)
                {
                    Console.WriteLine("Rejected: invalid or missing handshake");
                    return;
                }

                // Add client after the handshake so those who don't pass never receive broadcasts.
                _clients.TryAdd(client, 0);
                Console.WriteLine($"Client connected: {username}");

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
                Console.WriteLine("Connection closed");
            }
        }

        // Returns the username, or null if the first line is missing, malformed or late.
        private static async Task<string?> ReadHandshakeAsync(StreamReader reader)
        {
            using var cts = new CancellationTokenSource(HandshakeTimeout);
            try
            {
                string? first = await reader.ReadLineAsync(cts.Token);
                if (first == null || !first.StartsWith(Protocol.HandshakePrefix, StringComparison.Ordinal))
                    return null;

                string username = first[Protocol.HandshakePrefix.Length..].Trim();
                return username.Length == 0 ? null : username;
            }
            catch (OperationCanceledException)
            {
                return null;
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
