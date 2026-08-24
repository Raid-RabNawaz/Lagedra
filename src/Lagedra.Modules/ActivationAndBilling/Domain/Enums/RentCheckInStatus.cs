namespace Lagedra.Modules.ActivationAndBilling.Domain.Enums;

public enum RentCheckInStatus
{
    /// <summary>Waiting for the host to confirm whether rent arrived.</summary>
    Pending,

    /// <summary>Host confirmed the rent was received.</summary>
    Received,

    /// <summary>Host reported the rent was not received (compliance signal raised).</summary>
    Missed,
}
