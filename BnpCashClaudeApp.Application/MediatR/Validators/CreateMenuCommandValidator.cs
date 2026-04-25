using BnpCashClaudeApp.Application.MediatR.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.MediatR.Validators
{
    public class CreateMenuCommandValidator : AbstractValidator<CreateMenuCommand>
    {
        public CreateMenuCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Path).NotEmpty();
        }
    }
}
