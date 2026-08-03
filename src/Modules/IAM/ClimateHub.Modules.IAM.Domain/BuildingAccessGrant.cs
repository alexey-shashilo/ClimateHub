namespace ClimateHub.Modules.IAM.Domain;

public class BuildingAccessGrant
{
    public Guid UserId { get; private set; }
    public Guid BuildingId { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public Guid GrantedBy { get; private set; }

    private BuildingAccessGrant() { }

    public BuildingAccessGrant(Guid userId, Guid buildingId, Guid grantedBy)
    {
        UserId = userId;
        BuildingId = buildingId;
        GrantedAt = DateTime.UtcNow;
        GrantedBy = grantedBy;
    }
}