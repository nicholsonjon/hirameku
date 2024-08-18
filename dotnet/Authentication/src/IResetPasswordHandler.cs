namespace Hirameku.Authentication;

public interface IResetPasswordHandler
{
    Task<ResetPasswordResult> ResetPassword(
        ResetPasswordModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default);
}
