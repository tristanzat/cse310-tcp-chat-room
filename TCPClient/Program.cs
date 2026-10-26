namespace TCPClient
{
    internal class Program
    {
        private const string Host = "localhost";
        private const int Port = 5000;

        public static async Task Main()
        {
            using var client = new Client(Host, Port);
            await client.ConnectAsync();
            Console.WriteLine($"Connected to {Host}:{Port}. Type a message and press Enter.");

            string? input;
            while ((input = Console.ReadLine()) != null)
            {
                await client.SendAsync(input);
            }

            await client.DisconnectAsync();
        }
    }
}