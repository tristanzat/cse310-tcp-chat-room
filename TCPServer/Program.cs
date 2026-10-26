namespace TCPServer
{
    internal class Program
    {
        public static async Task Main()
        {
            var server = new Server();
            await server.RunAsync();
        }
    }
}