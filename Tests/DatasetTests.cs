using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MHAPalletizing.Models;
using MHAPalletizing.Utils;

namespace MHAPalletizing.Tests
{
    /// <summary>
    /// Dataset10.csv를 사용한 실제 데이터 테스트
    /// </summary>
    public static class DatasetTests
    {
        private const string DATASET_PATH = @"E:\Github\MHAPalletizing\3DBPP-master\Dataset10.csv";
        private const string RESULTS_PATH = @"E:\Github\MHAPalletizing\Results\";

        /// <summary>
        /// 병렬 처리로 Dataset10 전체 테스트 (빠른 처리)
        /// </summary>
        public static void RunDatasetTestsParallel(int maxThreads = 0)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║      Dataset10 MHA Algorithm Test (Parallel Mode)        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

            // 결과 폴더 생성
            if (!System.IO.Directory.Exists(RESULTS_PATH))
            {
                System.IO.Directory.CreateDirectory(RESULTS_PATH);
                Console.WriteLine($"✓ Created results directory: {RESULTS_PATH}");
            }

            // CSV에서 주문 읽기
            Console.WriteLine($"Reading orders from: {DATASET_PATH}");
            var orders = CsvReader.ReadOrdersFromCsv(DATASET_PATH);
            Console.WriteLine($"✓ Loaded {orders.Count} orders\n");

            // 병렬 처리기 생성
            var processor = new ParallelProcessor(maxThreads);

            // 병렬로 처리
            processor.ProcessOrdersParallel(orders, RESULTS_PATH, maxPalletsPerOrder: 10, seed: 42);

            Console.WriteLine("\n✓ All processing completed!");
        }

        /// <summary>
        /// 배치 단위로 Dataset10 처리 (메모리 절약)
        /// </summary>
        public static void RunDatasetTestsInBatches(int batchSize = 10, int maxThreads = 0)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║      Dataset10 MHA Algorithm Test (Batch Mode)           ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

            // 결과 폴더 생성
            if (!System.IO.Directory.Exists(RESULTS_PATH))
            {
                System.IO.Directory.CreateDirectory(RESULTS_PATH);
            }

            // CSV에서 주문 읽기
            Console.WriteLine($"Reading orders from: {DATASET_PATH}");
            var orders = CsvReader.ReadOrdersFromCsv(DATASET_PATH);
            Console.WriteLine($"✓ Loaded {orders.Count} orders\n");

            // 병렬 처리기 생성
            var processor = new ParallelProcessor(maxThreads);

            // 배치로 처리
            processor.ProcessOrdersInBatches(orders, RESULTS_PATH, batchSize, maxPalletsPerOrder: 10, seed: 42);

            Console.WriteLine("\n✓ All batch processing completed!");
        }

        /// <summary>
        /// 순차 처리로 Dataset10 전체 테스트 (기존 방식)
        /// </summary>
        public static void RunDatasetTests()
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         Dataset10 MHA Algorithm Test                     ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

            // 결과 폴더 생성
            if (!System.IO.Directory.Exists(RESULTS_PATH))
            {
                System.IO.Directory.CreateDirectory(RESULTS_PATH);
                Console.WriteLine($"✓ Created results directory: {RESULTS_PATH}");
            }

            // CSV에서 주문 읽기
            Console.WriteLine($"Reading orders from: {DATASET_PATH}");
            var orders = CsvReader.ReadOrdersFromCsv(DATASET_PATH);
            Console.WriteLine($"✓ Loaded {orders.Count} orders\n");

            // 결과 파일 경로
            string summaryPath = RESULTS_PATH + "summary_results.csv";
            string detailedPath = RESULTS_PATH + "detailed_results_{0}.csv";
            string placementsPath = RESULTS_PATH + "item_placements_{0}.csv";

            // 기존 요약 파일 삭제 (새로 시작)
            if (System.IO.File.Exists(summaryPath))
            {
                System.IO.File.Delete(summaryPath);
            }

            // 각 주문에 대해 MHA 실행
            int processedCount = 0;
            foreach (var order in orders)
            {
                processedCount++;
                Console.WriteLine($"\n[{processedCount}/{orders.Count}] Processing Order: {order.OrderId}");
                Console.WriteLine($"  Items: {order.TotalItemCount}, Product Types: {order.ProductTypeCount}");
                Console.WriteLine($"  Entropy: {order.Entropy:F4} ({order.GetComplexityClass()})");

                // MHA 알고리즘 실행
                var stopwatch = Stopwatch.StartNew();
                var mha = new MHAAlgorithm(seed: 42);

                // 최대 팔레트 수: 아이템 수에 따라 동적 결정
                int maxPallets = Math.Max(5, order.TotalItemCount / 50);

                List<Pallet> pallets;
                try
                {
                    pallets = mha.Solve(order, maxPallets);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ⚠ Error processing order {order.OrderId}: {ex.Message}");
                    continue;
                }

                stopwatch.Stop();
                double executionTimeMs = stopwatch.Elapsed.TotalMilliseconds;

                // 결과 요약 출력
                ResultWriter.PrintSummary(order, pallets, executionTimeMs);

                // CSV에 결과 기록
                ResultWriter.AppendOrderResult(summaryPath, order, pallets, executionTimeMs);

                // 상세 결과 저장
                string detailedFile = string.Format(detailedPath, order.OrderId);
                ResultWriter.WriteDetailedResults(detailedFile, order, pallets);

                // 아이템 배치 결과 저장
                string placementsFile = string.Format(placementsPath, order.OrderId);
                ResultWriter.WriteItemPlacements(placementsFile, order, pallets);

                Console.WriteLine($"  ✓ Order {order.OrderId} completed in {executionTimeMs:F2}ms\n");
            }

            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  All {processedCount} orders processed successfully!                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine($"\nResults saved to: {RESULTS_PATH}");
            Console.WriteLine($"  - Summary: summary_results.csv");
            Console.WriteLine($"  - Detailed: detailed_results_[OrderId].csv");
            Console.WriteLine($"  - Placements: item_placements_[OrderId].csv");
        }

        /// <summary>
        /// 특정 주문만 테스트
        /// </summary>
        public static void RunSingleOrderTest(string orderId)
        {
            Console.WriteLine($"\n=== Testing Single Order: {orderId} ===\n");

            var order = CsvReader.ReadSingleOrder(DATASET_PATH, orderId);
            if (order == null)
            {
                Console.WriteLine($"⚠ Order {orderId} not found in dataset!");
                return;
            }

            Console.WriteLine($"Order: {order}");

            var stopwatch = Stopwatch.StartNew();
            var mha = new MHAAlgorithm(seed: 42);
            int maxPallets = Math.Max(5, order.TotalItemCount / 50);
            var pallets = mha.Solve(order, maxPallets);
            stopwatch.Stop();

            double executionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            ResultWriter.PrintSummary(order, pallets, executionTimeMs);

            // 결과 저장
            if (!System.IO.Directory.Exists(RESULTS_PATH))
            {
                System.IO.Directory.CreateDirectory(RESULTS_PATH);
            }

            string summaryPath = RESULTS_PATH + "single_order_result.csv";
            ResultWriter.AppendOrderResult(summaryPath, order, pallets, executionTimeMs);

            string detailedPath = RESULTS_PATH + $"detailed_{orderId}.csv";
            ResultWriter.WriteDetailedResults(detailedPath, order, pallets);

            string placementsPath = RESULTS_PATH + $"placements_{orderId}.csv";
            ResultWriter.WriteItemPlacements(placementsPath, order, pallets);

            Console.WriteLine($"\n✓ Results saved to {RESULTS_PATH}");
        }

        /// <summary>
        /// 데이터셋 통계 출력
        /// </summary>
        public static void PrintDatasetStatistics()
        {
            Console.WriteLine("\n=== Dataset10 Statistics ===\n");

            var orders = CsvReader.ReadOrdersFromCsv(DATASET_PATH);

            Console.WriteLine($"Total Orders: {orders.Count}");
            Console.WriteLine($"Total Items: {orders.Sum(o => o.TotalItemCount)}");
            Console.WriteLine($"Avg Items per Order: {orders.Average(o => o.TotalItemCount):F1}");
            Console.WriteLine($"Min Items: {orders.Min(o => o.TotalItemCount)}");
            Console.WriteLine($"Max Items: {orders.Max(o => o.TotalItemCount)}");

            Console.WriteLine($"\nAvg Product Types: {orders.Average(o => o.ProductTypeCount):F1}");
            Console.WriteLine($"Avg Entropy: {orders.Average(o => o.Entropy):F4}");

            // Complexity 분포
            var complexityGroups = orders.GroupBy(o => o.GetComplexityClass())
                .OrderBy(g => g.Key);
            Console.WriteLine("\nComplexity Distribution:");
            foreach (var group in complexityGroups)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()} orders");
            }

            Console.WriteLine();
        }
    }
}
