namespace Dogger.Infrastructure.Secrets
{
    public interface ISecretsScanner
    {
        void Scan(string content);
    }
}
