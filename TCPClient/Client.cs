using System.Net.Sockets;
using System.Text;
using Shared;

namespace TCPClient
{
    internal class Client : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly TcpClient _client = new();
        private NetworkStream? _stream;
        private Task _receiveTask = Task.CompletedTask;

        // Shown again after each incoming message so the user's prompt stays at the bottom.
        public string Prompt { get; set; } = "";

        public Client(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task ConnectAsync(string username)
        {
            await _client.ConnectAsync(_host, _port);
            _stream = _client.GetStream();

            // Send the handshake
            await SendAsync(Protocol.HandshakePrefix + username);

            // Receive in the background so incoming messages don't block typing.
            _receiveTask = ReceiveLoopAsync(_stream);
        }

        // Messages are newline-delimited UTF-8 text.
        public async Task SendAsync(string message)
        {
            if (_stream == null) throw new InvalidOperationException("Not connected.");
            await _stream.WriteAsync(Encoding.UTF8.GetBytes(message + "\n"));
        }

        public async Task DisconnectAsync()
        {
            _client.Close();
            await _receiveTask;
        }

        private async Task ReceiveLoopAsync(NetworkStream stream)
        {
            try
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    // Erase the current prompt line, print the message, then redraw the prompt.
                    Console.Write("\r\e[2K");
                    Console.WriteLine(line);
                    Console.Write(Prompt);
                }
                Console.WriteLine("Disconnected from server.");
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                // Connection closed locally or by the server.
            }
        }

        public void Dispose() => _client.Dispose();
    }
}
