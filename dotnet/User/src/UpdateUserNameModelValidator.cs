namespace Hirameku.User;

using FluentValidation;
using Hirameku.Common;

public class UpdateUserNameModelValidator : AbstractValidator<UpdateUserNameModel>
{
    public UpdateUserNameModelValidator()
    {
        _ = this.RuleFor(u => u.UserName)
            .Matches(Regexes.UserName);
    }
}
