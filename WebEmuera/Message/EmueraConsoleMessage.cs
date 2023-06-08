namespace WebEmuera.Message
{
    public class EmueraConsoleMessage
    {
        public enum ConsoleMessage
        {
            None,
            ClearDisplay
        }

        public ConsoleMessage Message { get; set; }

        public EmueraConsoleMessage(ConsoleMessage message)
        {
            Message = message;
        }
    }
}
