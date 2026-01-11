using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace RevitBridge.Bridge.Performance
{
    /// <summary>
    /// Performance monitoring and metrics collection
    /// Tracks command execution times, throughput, and resource usage
    /// </summary>
    public class PerformanceMonitor
    {
        private class CommandMetrics
        {
            public string Tool { get; set; }
            public long ExecutionTimeMs { get; set; }
            public DateTime Timestamp { get; set; }
            public bool Success { get; set; }
            public string Error { get; set; }
        }

        private readonly ConcurrentBag<CommandMetrics> _metrics;
        private readonly Stopwatch _uptime;
        private long _totalCommands;
        private long _successfulCommands;
        private long _failedCommands;

        public PerformanceMonitor()
        {
            _metrics = new ConcurrentBag<CommandMetrics>();
            _uptime = Stopwatch.StartNew();
            _totalCommands = 0;
            _successfulCommands = 0;
            _failedCommands = 0;
        }

        /// <summary>
        /// Record command execution
        /// </summary>
        public void RecordCommand(string tool, long executionTimeMs, bool success, string error = null)
        {
            System.Threading.Interlocked.Increment(ref _totalCommands);

            if (success)
                System.Threading.Interlocked.Increment(ref _successfulCommands);
            else
                System.Threading.Interlocked.Increment(ref _failedCommands);

            _metrics.Add(new CommandMetrics
            {
                Tool = tool,
                ExecutionTimeMs = executionTimeMs,
                Timestamp = DateTime.UtcNow,
                Success = success,
                Error = error
            });

            // Keep only last 1000 metrics
            if (_metrics.Count > 1000)
            {
                CleanupOldMetrics();
            }
        }

        /// <summary>
        /// Get comprehensive performance statistics
        /// </summary>
        public object GetStatistics()
        {
            var recentMetrics = _metrics
                .Where(m => m.Timestamp > DateTime.UtcNow.AddMinutes(-5))
                .ToList();

            var commandBreakdown = recentMetrics
                .GroupBy(m => m.Tool)
                .Select(g => new
                {
                    tool = g.Key,
                    count = g.Count(),
                    averageMs = g.Average(m => m.ExecutionTimeMs),
                    minMs = g.Min(m => m.ExecutionTimeMs),
                    maxMs = g.Max(m => m.ExecutionTimeMs),
                    successRate = g.Count(m => m.Success) / (double)g.Count() * 100
                })
                .OrderByDescending(s => s.count)
                .Take(10)
                .ToList();

            var slowestCommands = recentMetrics
                .OrderByDescending(m => m.ExecutionTimeMs)
                .Take(5)
                .Select(m => new
                {
                    tool = m.Tool,
                    executionTimeMs = m.ExecutionTimeMs,
                    timestamp = m.Timestamp,
                    success = m.Success
                })
                .ToList();

            return new
            {
                uptime = new
                {
                    totalSeconds = _uptime.Elapsed.TotalSeconds,
                    formatted = FormatTimeSpan(_uptime.Elapsed)
                },
                totalCommands = _totalCommands,
                successfulCommands = _successfulCommands,
                failedCommands = _failedCommands,
                successRate = _totalCommands > 0 ? (_successfulCommands / (double)_totalCommands * 100) : 0,
                recent = new
                {
                    last5Minutes = recentMetrics.Count,
                    averageExecutionTimeMs = recentMetrics.Any() ? recentMetrics.Average(m => m.ExecutionTimeMs) : 0,
                    commandsPerMinute = recentMetrics.Count / 5.0
                },
                topCommands = commandBreakdown,
                slowestCommands = slowestCommands,
                memoryUsageMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
                gcCollections = new
                {
                    gen0 = GC.CollectionCount(0),
                    gen1 = GC.CollectionCount(1),
                    gen2 = GC.CollectionCount(2)
                }
            };
        }

        /// <summary>
        /// Get real-time throughput metrics
        /// </summary>
        public object GetThroughput()
        {
            var now = DateTime.UtcNow;
            var last1Min = _metrics.Count(m => m.Timestamp > now.AddMinutes(-1));
            var last5Min = _metrics.Count(m => m.Timestamp > now.AddMinutes(-5));
            var last15Min = _metrics.Count(m => m.Timestamp > now.AddMinutes(-15));

            return new
            {
                commandsPerMinute = new
                {
                    last1Min = last1Min,
                    last5Min = last5Min / 5.0,
                    last15Min = last15Min / 15.0
                },
                currentLoad = last1Min switch
                {
                    < 10 => "Low",
                    < 30 => "Medium",
                    < 60 => "High",
                    _ => "Very High"
                }
            };
        }

        /// <summary>
        /// Get command-specific statistics
        /// </summary>
        public object GetCommandStats(string tool)
        {
            var commandMetrics = _metrics
                .Where(m => m.Tool == tool)
                .ToList();

            if (!commandMetrics.Any())
            {
                return new
                {
                    tool = tool,
                    found = false,
                    message = "No metrics found for this command"
                };
            }

            return new
            {
                tool = tool,
                found = true,
                totalExecutions = commandMetrics.Count,
                successRate = commandMetrics.Count(m => m.Success) / (double)commandMetrics.Count * 100,
                averageExecutionTimeMs = commandMetrics.Average(m => m.ExecutionTimeMs),
                minExecutionTimeMs = commandMetrics.Min(m => m.ExecutionTimeMs),
                maxExecutionTimeMs = commandMetrics.Max(m => m.ExecutionTimeMs),
                medianExecutionTimeMs = GetMedian(commandMetrics.Select(m => m.ExecutionTimeMs)),
                lastExecution = commandMetrics.Max(m => m.Timestamp),
                recentErrors = commandMetrics
                    .Where(m => !m.Success && m.Timestamp > DateTime.UtcNow.AddMinutes(-15))
                    .Select(m => new { timestamp = m.Timestamp, error = m.Error })
                    .Take(5)
                    .ToList()
            };
        }

        /// <summary>
        /// Reset all metrics
        /// </summary>
        public void Reset()
        {
            _metrics.Clear();
            _uptime.Restart();
            _totalCommands = 0;
            _successfulCommands = 0;
            _failedCommands = 0;
        }

        private void CleanupOldMetrics()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-15);
            var recentMetrics = _metrics
                .Where(m => m.Timestamp > cutoff)
                .ToList();

            _metrics.Clear();
            foreach (var metric in recentMetrics)
            {
                _metrics.Add(metric);
            }
        }

        private double GetMedian(System.Collections.Generic.IEnumerable<long> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            int count = sorted.Count;

            if (count == 0) return 0;
            if (count % 2 == 1)
                return sorted[count / 2];
            else
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }

        private string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
            if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }
    }
}
