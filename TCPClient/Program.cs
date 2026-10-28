namespace TCPClient
{
    internal class Program
    {
        private const string Host = "localhost";
        private const int Port = 5000;

        public static async Task Main()
        {
            Console.Write("Username: ");
            string username = Console.ReadLine()?.Trim() ?? "";
            if (username.Length == 0)
            {
                Console.WriteLine("Username is required.");
                return;
            }

            using var client = new Client(Host, Port);
            await client.ConnectAsync(username);
            Console.WriteLine($"Connected to {Host}:{Port}. Type a message and press Enter.");

            string? input;
            bool connected = true;
            while (connected)
            {
                Console.Write($"{username}: ");
                input = Console.ReadLine();
                if (input != null)
                    await client.SendAsync($"{username}: {input}");
                else
                    connected = false;
            }

            await client.DisconnectAsync();
        }
    }
}