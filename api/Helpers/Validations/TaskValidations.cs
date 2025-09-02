using api.Models.Entities;
using FluentValidation;

namespace api.Helpers.Validations;

public class TaskValidations : AbstractValidator<TaskItem>
{
    public TaskValidations()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required");
    }
}