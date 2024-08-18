namespace Hirameku.Authentication;

public interface ISignInHandler
{
    Task<SignInResult> SignIn(AuthenticationData<SignInModel> data, CancellationToken cancellationToken = default);
}
