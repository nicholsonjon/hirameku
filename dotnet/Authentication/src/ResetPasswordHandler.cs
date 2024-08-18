namespace Hirameku.Authentication;

using AutoMapper;
using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common;
using Hirameku.Common.Properties;
using Hirameku.Data;
using Hirameku.Email;
using Hirameku.Recaptcha;
using NLog;
using System;
using System.Threading;

public class ResetPasswordHandler : IResetPasswordHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ResetPasswordHandler(
        ICachedValueDao cachedValueDao,
        IEmailTokenSerializer emailTokenSerializer,
        IMapper mapper,
        IPasswordDao passwordDao,
        IRecaptchaResponseValidator recaptchaResponseValidator,
        IValidator<ResetPasswordModel> resetPasswordModelValidator,
        IDocumentDao<UserDocument> userDao,
        IVerificationDao verificationDao)
    {
        this.CachedValueDao = cachedValueDao;
        this.EmailTokenSerializer = emailTokenSerializer;
        this.Mapper = mapper;
        this.PasswordDao = passwordDao;
        this.RecaptchaResponseValidator = recaptchaResponseValidator;
        this.ResetPasswordModelValidator = resetPasswordModelValidator;
        this.UserDao = userDao;
        this.VerificationDao = verificationDao;
    }

    private ICachedValueDao CachedValueDao { get; }

    private IEmailTokenSerializer EmailTokenSerializer { get; }

    private IMapper Mapper { get; }

    private IPasswordDao PasswordDao { get; }

    private IRecaptchaResponseValidator RecaptchaResponseValidator { get; }

    private IValidator<ResetPasswordModel> ResetPasswordModelValidator { get; }

    private IDocumentDao<UserDocument> UserDao { get; }

    private IVerificationDao VerificationDao { get; }

    public async Task<ResetPasswordResult> ResetPassword(
        ResetPasswordModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken = default)
    {
        Log.ForTraceEvent()
            .Property(LogProperties.Parameters, new { model, action, remoteIP, cancellationToken })
            .Message(LogMessages.EnteringMethod)
            .Log();

        ArgumentNullException.ThrowIfNull(model);

        await this.Validate(model, action, remoteIP, cancellationToken).ConfigureAwait(false);

        var (pepper, token, userName) = this.EmailTokenSerializer.Deserialize(model.SerializedToken);
        var user = await this.UserDao.GetUserByUserName(userName, cancellationToken).ConfigureAwait(false);
        var userId = user.Id;
        var verificationResult = await this.VerificationDao.VerifyToken(
            userId,
            user.EmailAddress,
            VerificationType.PasswordReset,
            token,
            pepper,
            cancellationToken)
            .ConfigureAwait(false);

        if (verificationResult == VerificationTokenVerificationResult.Verified)
        {
            await this.PasswordDao.SavePassword(userId, model.Password, cancellationToken).ConfigureAwait(false);

            if (user.UserStatus == UserStatus.PasswordChangeRequired)
            {
                await this.CachedValueDao.SetUserStatus(userId, UserStatus.OK).ConfigureAwait(false);
            }
            else if (user.UserStatus == UserStatus.EmailNotVerifiedAndPasswordChangeRequired)
            {
                await this.CachedValueDao.SetUserStatus(userId, UserStatus.EmailNotVerified).ConfigureAwait(false);
            }
        }

        var resetResult = this.Mapper.Map<ResetPasswordResult>(verificationResult);

        Log.ForTraceEvent()
            .Property(LogProperties.ReturnValue, resetResult)
            .Message(LogMessages.ExitingMethod)
            .Log();

        return resetResult;
    }

    private async Task Validate(
        ResetPasswordModel model,
        string action,
        string remoteIP,
        CancellationToken cancellationToken)
    {
        await this.ResetPasswordModelValidator.ValidateAndThrowAsync(model, cancellationToken)
            .ConfigureAwait(false);
        await this.RecaptchaResponseValidator.ValidateAndThrow(
            model.RecaptchaResponse,
            action,
            remoteIP,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
