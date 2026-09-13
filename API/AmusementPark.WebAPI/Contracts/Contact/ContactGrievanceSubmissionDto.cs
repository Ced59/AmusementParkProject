namespace AmusementPark.WebAPI.Contracts.Contact;

public sealed class ContactGrievanceSubmissionDto
{
    public bool Accepted { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
}
