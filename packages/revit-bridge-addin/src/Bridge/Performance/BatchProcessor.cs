using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace RevitBridge.Bridge.Performance
{
    /// <summary>
    /// Optimized batch processor for executing multiple commands efficiently
    /// Reduces transaction overhead and improves throughput
    /// </summary>
    public class BatchProcessor
    {
        private readonly UIApplication _app;
        private readonly ResponseCache _cache;

        public BatchProcessor(UIApplication app, ResponseCache cache = null)
        {
            _app = app;
            _cache = cache;
        }

        /// <summary>
        /// Execute multiple commands in a single transaction (when possible)
        /// </summary>
        public object ExecuteBatch(JsonElement batchPayload)
        {
            if (!batchPayload.TryGetProperty("commands", out var commandsArray))
            {
                throw new ArgumentException("Batch payload must contain 'commands' array");
            }

            var commands = commandsArray.EnumerateArray().ToList();
            bool useTransaction = batchPayload.TryGetProperty("use_transaction", out var ut) && ut.GetBoolean();

            var results = new List<object>();
            var errors = new List<object>();
            var stopwatch = Stopwatch.StartNew();

            if (useTransaction)
            {
                // Execute all commands in single transaction
                results = ExecuteInTransaction(commands, errors);
            }
            else
            {
                // Execute commands independently (better for reads)
                results = ExecuteIndependently(commands, errors);
            }

            stopwatch.Stop();

            return new
            {
                success = errors.Count == 0,
                totalCommands = commands.Count,
                successfulCommands = results.Count,
                failedCommands = errors.Count,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                averageTimePerCommandMs = stopwatch.ElapsedMilliseconds / (double)commands.Count,
                results = results,
                errors = errors
            };
        }

        /// <summary>
        /// Execute commands in a single transaction for maximum efficiency
        /// </summary>
        private List<object> ExecuteInTransaction(List<JsonElement> commands, List<object> errors)
        {
            var results = new List<object>();
            var doc = _app.ActiveUIDocument?.Document;

            if (doc == null)
            {
                errors.Add(new { error = "No active document" });
                return results;
            }

            using (var trans = new Autodesk.Revit.DB.Transaction(doc, "Batch Operation"))
            {
                trans.Start();

                try
                {
                    for (int i = 0; i < commands.Count; i++)
                    {
                        var cmd = commands[i];
                        try
                        {
                            string tool = cmd.GetProperty("tool").GetString();
                            var payload = cmd.TryGetProperty("payload", out var p) ? p : new JsonElement();

                            var result = BridgeCommandFactory.Execute(_app, tool, payload);
                            results.Add(new
                            {
                                index = i,
                                tool = tool,
                                result = result
                            });
                        }
                        catch (Exception ex)
                        {
                            errors.Add(new
                            {
                                index = i,
                                tool = cmd.TryGetProperty("tool", out var t) ? t.GetString() : "unknown",
                                error = ex.Message
                            });
                        }
                    }

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    errors.Add(new { error = $"Transaction failed: {ex.Message}" });
                }
            }

            return results;
        }

        /// <summary>
        /// Execute commands independently (better for read operations)
        /// </summary>
        private List<object> ExecuteIndependently(List<JsonElement> commands, List<object> errors)
        {
            var results = new List<object>();

            for (int i = 0; i < commands.Count; i++)
            {
                var cmd = commands[i];
                try
                {
                    string tool = cmd.GetProperty("tool").GetString();
                    var payload = cmd.TryGetProperty("payload", out var p) ? p : new JsonElement();

                    // Check cache first
                    object result = null;
                    string cacheKey = null;

                    if (_cache != null && IsReadOnlyCommand(tool))
                    {
                        cacheKey = ResponseCache.GenerateKey(tool, payload.ToString());
                        if (_cache.TryGet(cacheKey, out result))
                        {
                            results.Add(new
                            {
                                index = i,
                                tool = tool,
                                result = result,
                                cached = true
                            });
                            continue;
                        }
                    }

                    // Execute command
                    result = BridgeCommandFactory.Execute(_app, tool, payload);

                    // Cache read-only results
                    if (_cache != null && cacheKey != null)
                    {
                        _cache.Set(cacheKey, result);
                    }

                    results.Add(new
                    {
                        index = i,
                        tool = tool,
                        result = result,
                        cached = false
                    });
                }
                catch (Exception ex)
                {
                    errors.Add(new
                    {
                        index = i,
                        tool = cmd.TryGetProperty("tool", out var t) ? t.GetString() : "unknown",
                        error = ex.Message
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Determine if a command is read-only (safe to cache)
        /// </summary>
        private bool IsReadOnlyCommand(string tool)
        {
            return tool.StartsWith("revit.get_") ||
                   tool.StartsWith("revit.list_") ||
                   tool == "revit.health" ||
                   tool.Contains("_info") ||
                   tool.Contains("_statistics") ||
                   tool.Contains("_summary");
        }

        /// <summary>
        /// Parallel batch execution for independent read operations
        /// </summary>
        public object ExecuteParallelBatch(JsonElement batchPayload)
        {
            if (!batchPayload.TryGetProperty("commands", out var commandsArray))
            {
                throw new ArgumentException("Batch payload must contain 'commands' array");
            }

            var commands = commandsArray.EnumerateArray().ToList();
            var stopwatch = Stopwatch.StartNew();

            var results = new System.Collections.Concurrent.ConcurrentBag<object>();
            var errors = new System.Collections.Concurrent.ConcurrentBag<object>();

            Parallel.For(0, commands.Count, i =>
            {
                var cmd = commands[i];
                try
                {
                    string tool = cmd.GetProperty("tool").GetString();
                    var payload = cmd.TryGetProperty("payload", out var p) ? p : new JsonElement();

                    // Only parallelize read-only commands
                    if (!IsReadOnlyCommand(tool))
                    {
                        errors.Add(new
                        {
                            index = i,
                            tool = tool,
                            error = "Only read-only commands can be executed in parallel"
                        });
                        return;
                    }

                    var result = BridgeCommandFactory.Execute(_app, tool, payload);
                    results.Add(new
                    {
                        index = i,
                        tool = tool,
                        result = result
                    });
                }
                catch (Exception ex)
                {
                    errors.Add(new
                    {
                        index = i,
                        tool = cmd.TryGetProperty("tool", out var t) ? t.GetString() : "unknown",
                        error = ex.Message
                    });
                }
            });

            stopwatch.Stop();

            return new
            {
                success = errors.Count == 0,
                totalCommands = commands.Count,
                successfulCommands = results.Count,
                failedCommands = errors.Count,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                averageTimePerCommandMs = stopwatch.ElapsedMilliseconds / (double)commands.Count,
                parallel = true,
                results = results.OrderBy(r => ((dynamic)r).index),
                errors = errors.OrderBy(e => ((dynamic)e).index)
            };
        }
    }
}
