namespace Lagedra.SharedKernel.Settings;

public sealed class PlatformSetting
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    private PlatformSetting() { }

    public static PlatformSetting Create(string key, string value, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        return new PlatformSetting
        {
            Key = key,
            Value = value,
            Description = description,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string value, Guid? updatedByUserId)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }
}
