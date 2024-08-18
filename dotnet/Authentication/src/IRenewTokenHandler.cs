namespace Hirameku.Authentication;

public interface IRenewTokenHandler
{
    Task<RenewTokenResult> RenewToken(
        AuthenticationData<RenewTokenModel> data,
        CancellationToken cancellationToken = default);
}
