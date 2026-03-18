using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

// Extend the default config with additional statistical columns so CI exports
// contain the full detail: Min, Median, Max on top of the default Mean/Error/StdDev.
// The [MemoryDiagnoser] and [RankColumn] attributes on each benchmark class still
// supply Gen0/Gen1/Gen2, Allocated, Alloc Ratio, and Rank.
var config = DefaultConfig.Instance
    .AddColumn(StatisticColumn.Min)
    .AddColumn(StatisticColumn.Median)
    .AddColumn(StatisticColumn.Max);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
