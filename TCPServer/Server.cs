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
        // Maps each connected client to its username.
        private readonly ConcurrentDictionary<TcpClient, string> _clients = new();

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
            string? username = null;
            try
            {
                using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);

                username = await ReadHandshakeAsync(reader);
                if (username == null)
                {
                    Console.WriteLine("Rejected: invalid or missing handshake");
                    return;
                }

                // Add client after the handshake so those who don't pass never receive broadcasts.
                _clients.TryAdd(client, username);
                Console.WriteLine($"Client connected: {username}");
                await BroadcastAsync($"{Protocol.SystemPrefix} {username} joined the chat.", sender: client);

                // Messages are newline-delimited UTF-8 text; null means the client disconnected.
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    Console.WriteLine($"Received: {line}");
                    // Check for commands coming in and handle accordingly
                    if (line.StartsWith(Protocol.CommandPrefix))
                    {
                        string command = line[Protocol.CommandPrefix.Length..].Trim();
                        if (Enum.TryParse(command, ignoreCase: true, out Protocol.Command result))
                        {
                            switch (result)
                            {
                                case Protocol.Command.Quit:
                                    await MessageClientAsync($"{Protocol.SystemPrefix} Goodbye.", client);
                                    break;
                                case Protocol.Command.List:
                                    string users = string.Join(", ", _clients.Values);
                                    await MessageClientAsync($"{Protocol.SystemPrefix} Online: {users}", client);
                                    break;
                                case Protocol.Command.Help:
                                    List<string> commands = [.. Enum.GetNames<Protocol.Command>()];
                                    string message = "Valid commands: " + string.Join(", ", commands).ToLower();
                                    await MessageClientAsync($"{Protocol.SystemPrefix} {message}", client);
                                    break;
                            }
                        }
                        else
                        {
                            await MessageClientAsync($"{Protocol.SystemPrefix} Error: '{command}'" +
                                " is not a valid command. Try '/help' for a list of commands.", client);
                        }
                        continue;
                    }
                    await BroadcastAsync($"{username}: {line}", sender: client);
                }
            }
            catch (IOException)
            {
                // Connection dropped abruptly; treat as a disconnect.
            }
            finally
            {
                bool wasRegistered = _clients.TryRemove(client, out _);
                client.Dispose();
                if (wasRegistered)
                    await BroadcastAsync($"{Protocol.SystemPrefix} {username} left the chat.", sender: client);
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

        // This method is very similar to BroadcastAsync but it sends a message
        // back to the sender instead of to every client. I use this method for
        // server messages (e.g. command feedback).
        private static async Task MessageClientAsync(string message, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");

            try
            {
                await sender.GetStream().WriteAsync(data);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                // Client will clean up on its own
            }
        }

        private async Task BroadcastAsync(string message, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");

            foreach (TcpClient client in _clients.Keys)
            {
                // Don't send message back to sender
                if (client == sender)
                    continue;

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
