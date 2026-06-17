using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Contact.Contracts;

namespace AmusementPark.Application.Features.Contact.Commands;

public sealed record SubmitContactGrievanceCommand(ContactGrievanceSubmission Submission)
    : ICommand<ApplicationResult<ContactGrievanceSubmissionResult>>;
