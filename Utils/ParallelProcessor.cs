using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using MHAPalletizing.Models;

namespace MHAPalletizing.Utils
{
    /// <summary>
    /// 대규모 데이터셋을 병렬로 처리하는 유틸리티
    /// 멀티스레드를 활용하여 여러 주문을 동시에 처리합니다.
    /// </summary>
    public class ParallelProcessor
    {
        private readonly int maxDegreeOfParallelism;
        private readonly object lockObject = new object();
        private int completedCount = 0;
        private int totalCount = 0;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="maxThreads">최대 스레드 수 (0 = CPU 코어 수만큼 사용)</param>
        public ParallelProcessor(int maxThreads = 0)
        {
            if (maxThreads <= 0)
            {
                // CPU 코어 수 기반 최적 스레드 수 계산
                // 물리적 코어 수를 사용 (하이퍼스레딩 고려)
                int coreCount = Environment.ProcessorCount;
                maxDegreeOfParallelism = Math.Max(2, Math.Min(coreCount, 8));
            }
            else
            {
                maxDegreeOfParallelism = maxThreads;
            }

            Console.WriteLine($"Parallel Processor initialized with {maxDegreeOfParallelism} threads");
        }

        /// <summary>
        /// 여러 주문을 병렬로 처리합니다.
        /// </summary>
        public void ProcessOrdersParallel(
            List<Order> orders,
            string resultsPath,
            int maxPalletsPerOrder = 10,
            int? seed = null)
        {
            totalCount = orders.Count;
            completedCount = 0;

            var startTime = DateTime.Now;
            Console.WriteLine($"\n╔{'═'.Repeat(60)}╗");
            Console.WriteLine($"║  Parallel Processing {totalCount} orders with {maxDegreeOfParallelism} threads");
            Console.WriteLine($"╚{'═'.Repeat(60)}╝\n");

            // 결과 폴더 생성
            if (!System.IO.Directory.Exists(resultsPath))
            {
                System.IO.Directory.CreateDirectory(resultsPath);
            }

            // 결과 파일 경로
            string summaryPath = System.IO.Path.Combine(resultsPath, "summary_results.csv");
            string detailedPathTemplate = System.IO.Path.Combine(resultsPath, "detailed_results_{0}.csv");
            string placementsPathTemplate = System.IO.Path.Combine(resultsPath, "item_placements_{0}.csv");

            // 기존 요약 파일 삭제
            if (System.IO.File.Exists(summaryPath))
            {
                System.IO.File.Delete(summaryPath);
            }

            // 스레드 안전한 결과 수집기
            var results = new ConcurrentBag<OrderResult>();
            var errors = new ConcurrentBag<string>();

            // Parallel.ForEach로 병렬 처리
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            Parallel.ForEach(orders, parallelOptions, order =>
            {
                try
                {
                    // 각 스레드마다 고유한 seed 사용 (재현성 유지)
                    int threadSeed = seed.HasValue ? seed.Value + order.OrderId.GetHashCode() : order.OrderId.GetHashCode();

                    var stopwatch = Stopwatch.StartNew();
                    var mha = new MHAAlgorithm(seed: threadSeed);

                    // 최대 팔레트 수: 아이템 수에 따라 동적 결정
                    int maxPallets = Math.Max(maxPalletsPerOrder, order.TotalItemCount / 50);

                    List<Pallet> pallets = mha.Solve(order, maxPallets);
                    stopwatch.Stop();

                    double executionTimeMs = stopwatch.Elapsed.TotalMilliseconds;

                    // 결과 저장
                    var result = new OrderResult
                    {
                        Order = order,
                        Pallets = pallets,
                        ExecutionTimeMs = executionTimeMs,
                        Success = true
                    };
                    results.Add(result);

                    // 진행률 업데이트 (스레드 안전)
                    lock (lockObject)
                    {
                        completedCount++;
                        double progress = (double)completedCount / totalCount * 100;
                        Console.WriteLine($"[{completedCount}/{totalCount}] ({progress:F1}%) " +
                            $"Order {order.OrderId}: {pallets.Count} pallets, " +
                            $"{pallets.Sum(p => p.Items.Count)}/{order.TotalItemCount} items, " +
                            $"{executionTimeMs:F0}ms");
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        completedCount++;
                        errors.Add($"Order {order.OrderId}: {ex.Message}");
                        Console.WriteLine($"[{completedCount}/{totalCount}] ⚠ Order {order.OrderId} failed: {ex.Message}");
                    }
                }
            });

            // 모든 처리 완료 후 결과 저장
            Console.WriteLine("\n" + new string('═', 60));
            Console.WriteLine("Saving results to CSV files...");

            // 결과를 OrderId 순으로 정렬
            var sortedResults = results.OrderBy(r => r.Order.OrderId).ToList();

            // 스레드 안전하게 파일에 기록
            foreach (var result in sortedResults)
            {
                // 요약 결과 추가
                ResultWriter.AppendOrderResult(summaryPath, result.Order, result.Pallets, result.ExecutionTimeMs);

                // 상세 결과 저장
                string detailedFile = string.Format(detailedPathTemplate, result.Order.OrderId);
                ResultWriter.WriteDetailedResults(detailedFile, result.Order, result.Pallets);

                // 아이템 배치 결과 저장
                string placementsFile = string.Format(placementsPathTemplate, result.Order.OrderId);
                ResultWriter.WriteItemPlacements(placementsFile, result.Order, result.Pallets);
            }

            // 최종 통계
            var totalTime = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine(new string('═', 60));
            Console.WriteLine($"\n✓ Parallel Processing Complete!");
            Console.WriteLine($"  Total Orders: {totalCount}");
            Console.WriteLine($"  Successful: {results.Count}");
            Console.WriteLine($"  Failed: {errors.Count}");
            Console.WriteLine($"  Total Time: {totalTime:F2}s");
            Console.WriteLine($"  Avg Time per Order: {totalTime / totalCount:F2}s");
            Console.WriteLine($"  Speedup: ~{maxDegreeOfParallelism}x (with {maxDegreeOfParallelism} threads)");

            if (errors.Any())
            {
                Console.WriteLine($"\n⚠ Errors:");
                foreach (var error in errors.Take(10))
                {
                    Console.WriteLine($"  - {error}");
                }
                if (errors.Count > 10)
                {
                    Console.WriteLine($"  ... and {errors.Count - 10} more errors");
                }
            }

            Console.WriteLine($"\nResults saved to: {resultsPath}");
        }

        /// <summary>
        /// 배치 단위로 주문을 처리합니다 (메모리 절약)
        /// </summary>
        public void ProcessOrdersInBatches(
            List<Order> orders,
            string resultsPath,
            int batchSize = 10,
            int maxPalletsPerOrder = 10,
            int? seed = null)
        {
            Console.WriteLine($"\n╔{'═'.Repeat(60)}╗");
            Console.WriteLine($"║  Batch Processing {orders.Count} orders (batch size: {batchSize})");
            Console.WriteLine($"╚{'═'.Repeat(60)}╝\n");

            int totalBatches = (int)Math.Ceiling((double)orders.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                int startIndex = batchIndex * batchSize;
                int count = Math.Min(batchSize, orders.Count - startIndex);
                var batch = orders.GetRange(startIndex, count);

                Console.WriteLine($"\n--- Batch {batchIndex + 1}/{totalBatches} ({batch.Count} orders) ---");
                ProcessOrdersParallel(batch, resultsPath, maxPalletsPerOrder, seed);

                // 메모리 정리
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// 주문 처리 결과를 담는 클래스
        /// </summary>
        private class OrderResult
        {
            public Order Order { get; set; }
            public List<Pallet> Pallets { get; set; }
            public double ExecutionTimeMs { get; set; }
            public bool Success { get; set; }
        }
    }

    /// <summary>
    /// String extension for repeat
    /// </summary>
    internal static class StringExtensions
    {
        public static string Repeat(this char c, int count)
        {
            return new string(c, count);
        }
    }
}
