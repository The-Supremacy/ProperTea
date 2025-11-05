using ProperTea.ProperSagas;

namespace Examples.Sagas;

/// <summary>
/// Example saga for GDPR data deletion workflow
/// This is a reference example - adapt to your needs
/// </summary>
public class GDPRDeletionSaga : SagaBase
{
    public GDPRDeletionSaga()
    {
        Steps = new List<SagaStep>
        {
            new() { Name = "ValidateLeases", Status = SagaStepStatus.Pending },
            new() { Name = "ValidateInvoices", Status = SagaStepStatus.Pending },
            new() { Name = "AnonymizeContact", Status = SagaStepStatus.Pending },
            new() { Name = "DeactivateUser", Status = SagaStepStatus.Pending },
            new() { Name = "RemoveGroupMemberships", Status = SagaStepStatus.Pending }
        };
    }

    // Strongly-typed helper methods
    public void SetUserId(Guid userId) => SetData("userId", userId);
    public Guid GetUserId() => GetData<Guid>("userId");
    
    public void SetContactId(Guid contactId) => SetData("contactId", contactId);
    public Guid? GetContactId() => GetData<Guid?>("contactId");
    
    public void SetOrganizationId(Guid organizationId) => SetData("organizationId", organizationId);
    public Guid GetOrganizationId() => GetData<Guid>("organizationId");
}

