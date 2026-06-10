using System.Threading.RateLimiting;

namespace Lagedra.Infrastructure.Middleware;

/// <summary>
/// Combines several <see cref="RateLimiter"/> instances so a request is only
/// permitted when every underlying limiter grants a lease. Used to enforce more
/// than one window on the same partition (for example 5/hour AND 30/day). If any
/// limiter rejects, leases already taken from earlier limiters are released and
/// the rejecting lease (with its metadata) is returned.
/// </summary>
internal sealed class CompositeRateLimiter : RateLimiter
{
    private readonly RateLimiter[] _limiters;

    public CompositeRateLimiter(params RateLimiter[] limiters)
    {
        ArgumentNullException.ThrowIfNull(limiters);
        if (limiters.Length == 0)
        {
            throw new ArgumentException("At least one limiter is required.", nameof(limiters));
        }

        _limiters = limiters;
    }

    public override RateLimiterStatistics? GetStatistics() => _limiters[0].GetStatistics();

    public override TimeSpan? IdleDuration
    {
        get
        {
            TimeSpan? min = null;
            foreach (var limiter in _limiters)
            {
                var duration = limiter.IdleDuration;
                if (duration is null)
                {
                    return null;
                }

                if (min is null || duration < min)
                {
                    min = duration;
                }
            }

            return min;
        }
    }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        var acquired = new List<RateLimitLease>(_limiters.Length);
        foreach (var limiter in _limiters)
        {
            var lease = limiter.AttemptAcquire(permitCount);
            if (!lease.IsAcquired)
            {
                ReleaseAll(acquired);
                return lease;
            }

            acquired.Add(lease);
        }

        return new CompositeLease(acquired);
    }

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(
        int permitCount,
        CancellationToken cancellationToken)
    {
        var acquired = new List<RateLimitLease>(_limiters.Length);
        foreach (var limiter in _limiters)
        {
            var lease = await limiter.AcquireAsync(permitCount, cancellationToken).ConfigureAwait(false);
            if (!lease.IsAcquired)
            {
                ReleaseAll(acquired);
                return lease;
            }

            acquired.Add(lease);
        }

        return new CompositeLease(acquired);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var limiter in _limiters)
            {
                limiter.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private static void ReleaseAll(List<RateLimitLease> leases)
    {
        foreach (var lease in leases)
        {
            lease.Dispose();
        }
    }

    private sealed class CompositeLease(List<RateLimitLease> leases) : RateLimitLease
    {
        public override bool IsAcquired => true;

        public override IEnumerable<string> MetadataNames =>
            leases.SelectMany(l => l.MetadataNames).Distinct(StringComparer.Ordinal);

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            foreach (var lease in leases)
            {
                if (lease.TryGetMetadata(metadataName, out metadata))
                {
                    return true;
                }
            }

            metadata = null;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var lease in leases)
                {
                    lease.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
