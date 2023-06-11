namespace Hirameku.User;

using Autofac;
using FluentValidation;
using Hirameku.Caching;
using Hirameku.Common.Service;
using Hirameku.Data;
using Hirameku.Email;

public class UserModule : Module
{
    public UserModule()
    {
    }

    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterModule<CachingModule>().IfNotRegistered(typeof(CachingModule));
        _ = builder.RegisterModule<CommonServiceModule>().IfNotRegistered(typeof(CommonServiceModule));
        _ = builder.RegisterModule<DataModule>().IfNotRegistered(typeof(DataModule));
        _ = builder.RegisterModule<EmailModule>().IfNotRegistered(typeof(EmailModule));

        _ = builder.Register(
            c =>
            {
                var modelValidator = new ChangePasswordModelValidator(c.Resolve<IPasswordValidator>());
                return new AuthenticatedValidator<ChangePasswordModel>(modelValidator);
            })
            .As<IValidator<Authenticated<ChangePasswordModel>>>();
        _ = builder.Register(
            c =>
            {
                var modelValidator = new UpdateEmailAddressModelValidator();
                return new AuthenticatedValidator<UpdateEmailAddressModel>(modelValidator);
            })
            .As<IValidator<Authenticated<UpdateEmailAddressModel>>>();
        _ = builder.Register(
            c =>
            {
                var modelValidator = new UpdateNameModelValidator();
                return new AuthenticatedValidator<UpdateNameModel>(modelValidator);
            })
            .As<IValidator<Authenticated<UpdateNameModel>>>();
        _ = builder.Register(
            c =>
            {
                var modelValidator = new UpdateUserNameModelValidator();
                return new AuthenticatedValidator<UpdateUserNameModel>(modelValidator);
            })
            .As<IValidator<Authenticated<UpdateUserNameModel>>>();

        _ = builder.RegisterType<UserProvider>().As<IUserProvider>();
    }
}
