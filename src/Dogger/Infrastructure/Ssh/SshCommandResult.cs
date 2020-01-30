namespace Dogger.Infrastructure.Ssh
{
    public struct SshCommandResult
    {
        public int ExitCode { get; set; }
        public string Text { get; set; }
    }
}
