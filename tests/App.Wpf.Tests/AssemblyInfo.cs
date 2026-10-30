using Xunit;

// Strings.Culture and the UI culture are process-wide: LocalizationService tests change them,
// so tests run one at a time to keep every message assertion deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
