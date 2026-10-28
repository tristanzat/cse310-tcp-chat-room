namespace Shared
{
    public static class Protocol
    {
        // Handshake string
        public const string HandshakePrefix = "HELLO ";

        // Commands
        public const string CommandPrefix = "/";
        public enum Command
        {
            Help,
            List,
            Quit
        }

        // System prefix
        public const string SystemPrefix = "[SYSTEM]";
    }
}
