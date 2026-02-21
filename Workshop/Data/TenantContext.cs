namespace Workshop.Data;

public sealed class TenantContext
{
    public int TenantId { get; private set; } = 1;

    public void SetTenantId(int tenantId)
    {
        if (tenantId > 0)
            TenantId = tenantId;
    }
}
