namespace Dogger.Infrastructure.Ssh
{
    public enum SshRetryPolicy
    {
        AllowRetries,
        ProhibitRetries
    }

    public enum SshResponseSensitivity
    {
        MayContainSensitiveData,
        ContainsNoSensitiveData
    }
}
