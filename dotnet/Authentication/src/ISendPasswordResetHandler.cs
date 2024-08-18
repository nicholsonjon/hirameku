namespace Hirameku.Authentication;

public interface ISendPasswordResetHandler
{
    Task SendPasswordReset(
        SendPasswordResetModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default);
}
