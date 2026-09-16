using Xunit;

// Each test class here boots its own WebApplicationFactory<Program>, and startup runs EF Core
// migrations against a single shared Postgres instance. xUnit's default parallelization runs
// different test classes concurrently, so two factories booting at once race on
// __EFMigrationsHistory and fail with "column already exists". Run this assembly's tests
// sequentially instead — there are only a handful, so the cost is negligible.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
