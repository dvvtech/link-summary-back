using FluentValidation;
using LinkSummary.Api.Models;

namespace LinkSummary.Api.Validators
{
    public class SummarizeRequestValidator : AbstractValidator<SummarizeRequest>
    {
        public SummarizeRequestValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty()
                .WithMessage("URL не может быть пустым.")
                .Must(BeValidHttpUrl)
                .WithMessage("Некорректный формат URL.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Номер страницы должен быть не менее 1.");
        }

        private static bool BeValidHttpUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
