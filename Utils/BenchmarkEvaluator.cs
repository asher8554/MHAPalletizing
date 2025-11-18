using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MHAPalletizing.Models;

namespace MHAPalletizing.Utils
{
    /// <summary>
    /// 벤치마크 평가를 위한 성능 지표 계산 및 CSV 출력
    /// 논문 형식의 벤치마크 결과 테이블 생성
    /// </summary>
    public class BenchmarkEvaluator
    {
        /// <summary>
        /// 단일 주문에 대한 벤치마크 결과
        /// </summary>
        public class BenchmarkResult
        {
            // 기본 정보
            public string OrderId { get; set; }
            public string Algorithm { get; set; }
            public int ItemCount { get; set; }
            public int ProductTypes { get; set; }
            public double Entropy { get; set; }
            public string Complexity { get; set; }

            // 팔레타이징 성능
            public int PalletsUsed { get; set; }
            public int ItemsPlaced { get; set; }
            public int ItemsUnplaced { get; set; }
            public double PlacementRate { get; set; }  // 배치 성공률 (%)

            // 공간 활용률
            public double AvgVolumeUtilization { get; set; }
            public double MaxVolumeUtilization { get; set; }
            public double MinVolumeUtilization { get; set; }
            public double AvgHeightUtilization { get; set; }

            // 무게 및 밀도
            public double TotalWeight { get; set; }
            public double AvgWeightPerPallet { get; set; }
            public double AvgDensity { get; set; }  // kg/m³

            // 품질 지표
            public double AvgHeterogeneity { get; set; }  // 팔레트당 평균 제품 타입 비율
            public double AvgCompactness { get; set; }    // 아이템 간 평균 밀집도
            public double AvgStability { get; set; }      // 무게 중심 안정성

            // 실행 시간
            public double ExecutionTimeMs { get; set; }
            public double ExecutionTimeSec { get; set; }

            // 효율성 지표
            public double TimePerItem { get; set; }       // 아이템당 처리 시간 (ms)
            public double TimePerPallet { get; set; }     // 팔레트당 처리 시간 (ms)
        }

        /// <summary>
        /// 주문과 팔레트 결과로부터 벤치마크 결과를 계산합니다.
        /// </summary>
        public static BenchmarkResult CalculateBenchmark(Order order, List<Pallet> pallets,
            double executionTimeMs, string algorithm = "MHA")
        {
            var result = new BenchmarkResult
            {
                OrderId = order.OrderId,
                Algorithm = algorithm,
                ItemCount = order.TotalItemCount,
                ProductTypes = order.ProductTypeCount,
                Entropy = order.Entropy,
                Complexity = order.GetComplexityClass(),
                ExecutionTimeMs = executionTimeMs,
                ExecutionTimeSec = executionTimeMs / 1000.0
            };

            // 배치 성능
            result.ItemsPlaced = pallets.Sum(p => p.Items.Count);
            result.ItemsUnplaced = order.Items.Count - result.ItemsPlaced;
            result.PlacementRate = order.Items.Count > 0
                ? (double)result.ItemsPlaced / order.Items.Count * 100.0
                : 0;

            result.PalletsUsed = pallets.Count;

            if (pallets.Any())
            {
                // 공간 활용률
                var volumeUtils = pallets.Select(p => p.VolumeUtilization).ToList();
                result.AvgVolumeUtilization = volumeUtils.Average();
                result.MaxVolumeUtilization = volumeUtils.Max();
                result.MinVolumeUtilization = volumeUtils.Min();
                result.AvgHeightUtilization = pallets.Average(p => p.CurrentHeight / p.MaxHeight);

                // 무게 및 밀도
                result.TotalWeight = pallets.Sum(p => p.TotalWeight);
                result.AvgWeightPerPallet = result.TotalWeight / pallets.Count;

                // 밀도 계산 (kg/m³)
                double totalVolumeM3 = pallets.Sum(p =>
                    (p.Length / 1000.0) * (p.Width / 1000.0) * (p.CurrentHeight / 1000.0));
                result.AvgDensity = totalVolumeM3 > 0 ? result.TotalWeight / totalVolumeM3 : 0;

                // 품질 지표
                double totalHeterogeneity = 0;
                double totalCompactness = 0;
                double totalStability = 0;

                foreach (var pallet in pallets)
                {
                    // Heterogeneity: 팔레트 내 제품 타입 수 / 전체 제품 타입 수
                    int productTypesInPallet = pallet.Items.Select(i => i.ProductId).Distinct().Count();
                    totalHeterogeneity += (double)productTypesInPallet / order.ProductTypeCount;

                    // Compactness
                    totalCompactness += pallet.GetAverageCompactness();

                    // Stability: 무게중심이 팔레트 중심으로부터의 거리 (정규화)
                    var com = pallet.GetCenterOfMass();
                    double centerX = pallet.Length / 2.0;
                    double centerY = pallet.Width / 2.0;
                    double distance = Math.Sqrt(
                        Math.Pow(com.x - centerX, 2) +
                        Math.Pow(com.y - centerY, 2)
                    );
                    double maxDistance = Math.Sqrt(
                        Math.Pow(pallet.Length / 2.0, 2) +
                        Math.Pow(pallet.Width / 2.0, 2)
                    );
                    double stabilityScore = maxDistance > 0 ? 1.0 - (distance / maxDistance) : 1.0;
                    totalStability += stabilityScore;
                }

                result.AvgHeterogeneity = totalHeterogeneity / pallets.Count;
                result.AvgCompactness = totalCompactness / pallets.Count;
                result.AvgStability = totalStability / pallets.Count;
            }

            // 효율성 지표
            result.TimePerItem = result.ItemCount > 0
                ? executionTimeMs / result.ItemCount
                : 0;
            result.TimePerPallet = result.PalletsUsed > 0
                ? executionTimeMs / result.PalletsUsed
                : 0;

            return result;
        }

        /// <summary>
        /// 벤치마크 결과를 CSV에 추가합니다.
        /// </summary>
        public static void AppendBenchmarkResult(string outputPath, BenchmarkResult result)
        {
            bool fileExists = File.Exists(outputPath);

            using (var writer = new StreamWriter(outputPath, append: true))
            {
                // 헤더 작성
                if (!fileExists)
                {
                    writer.WriteLine(
                        "OrderId,Algorithm,ItemCount,ProductTypes,Entropy,Complexity," +
                        "PalletsUsed,ItemsPlaced,ItemsUnplaced,PlacementRate," +
                        "AvgVolumeUtil,MaxVolumeUtil,MinVolumeUtil,AvgHeightUtil," +
                        "TotalWeight,AvgWeightPerPallet,AvgDensity," +
                        "AvgHeterogeneity,AvgCompactness,AvgStability," +
                        "ExecutionTimeMs,ExecutionTimeSec,TimePerItem,TimePerPallet"
                    );
                }

                // 데이터 작성
                writer.WriteLine(
                    $"{result.OrderId},{result.Algorithm}," +
                    $"{result.ItemCount},{result.ProductTypes},{result.Entropy:F4},{result.Complexity}," +
                    $"{result.PalletsUsed},{result.ItemsPlaced},{result.ItemsUnplaced},{result.PlacementRate:F2}," +
                    $"{result.AvgVolumeUtilization:F4},{result.MaxVolumeUtilization:F4}," +
                    $"{result.MinVolumeUtilization:F4},{result.AvgHeightUtilization:F4}," +
                    $"{result.TotalWeight:F2},{result.AvgWeightPerPallet:F2},{result.AvgDensity:F2}," +
                    $"{result.AvgHeterogeneity:F4},{result.AvgCompactness:F4},{result.AvgStability:F4}," +
                    $"{result.ExecutionTimeMs:F2},{result.ExecutionTimeSec:F3}," +
                    $"{result.TimePerItem:F3},{result.TimePerPallet:F2}"
                );
            }
        }

        /// <summary>
        /// 여러 벤치마크 결과의 요약 통계를 CSV로 출력합니다.
        /// </summary>
        public static void WriteSummaryStatistics(string outputPath, List<BenchmarkResult> results)
        {
            if (!results.Any())
            {
                Console.WriteLine("⚠ No results to summarize");
                return;
            }

            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("=== Benchmark Summary Statistics ===\n");
                writer.WriteLine($"Total Orders Processed,{results.Count}");
                writer.WriteLine($"Total Items,{results.Sum(r => r.ItemCount)}");
                writer.WriteLine($"Total Pallets Used,{results.Sum(r => r.PalletsUsed)}");
                writer.WriteLine();

                // 복잡도별 통계
                writer.WriteLine("=== Performance by Complexity ===");
                writer.WriteLine("Complexity,Orders,AvgPallets,AvgVolumeUtil,AvgPlacementRate,AvgTimeMs");

                var byComplexity = results.GroupBy(r => r.Complexity)
                    .OrderBy(g => g.Key);

                foreach (var group in byComplexity)
                {
                    var groupResults = group.ToList();
                    writer.WriteLine(
                        $"{group.Key},{groupResults.Count}," +
                        $"{groupResults.Average(r => r.PalletsUsed):F2}," +
                        $"{groupResults.Average(r => r.AvgVolumeUtilization):F4}," +
                        $"{groupResults.Average(r => r.PlacementRate):F2}," +
                        $"{groupResults.Average(r => r.ExecutionTimeMs):F2}"
                    );
                }
                writer.WriteLine();

                // 전체 평균 성능
                writer.WriteLine("=== Overall Performance Metrics ===");
                writer.WriteLine("Metric,Average,Min,Max,StdDev");

                WriteStatLine(writer, "PalletsUsed", results.Select(r => (double)r.PalletsUsed));
                WriteStatLine(writer, "PlacementRate(%)", results.Select(r => r.PlacementRate));
                WriteStatLine(writer, "VolumeUtilization", results.Select(r => r.AvgVolumeUtilization));
                WriteStatLine(writer, "HeightUtilization", results.Select(r => r.AvgHeightUtilization));
                WriteStatLine(writer, "Heterogeneity", results.Select(r => r.AvgHeterogeneity));
                WriteStatLine(writer, "Compactness", results.Select(r => r.AvgCompactness));
                WriteStatLine(writer, "Stability", results.Select(r => r.AvgStability));
                WriteStatLine(writer, "ExecutionTime(ms)", results.Select(r => r.ExecutionTimeMs));
                WriteStatLine(writer, "TimePerItem(ms)", results.Select(r => r.TimePerItem));
                writer.WriteLine();

                // 제품 타입별 성능
                writer.WriteLine("=== Performance by Product Types ===");
                writer.WriteLine("ProductTypes,Orders,AvgPallets,AvgVolumeUtil,AvgHeterogeneity");

                var byProductTypes = results.GroupBy(r => r.ProductTypes)
                    .OrderBy(g => g.Key);

                foreach (var group in byProductTypes)
                {
                    var groupResults = group.ToList();
                    writer.WriteLine(
                        $"{group.Key},{groupResults.Count}," +
                        $"{groupResults.Average(r => r.PalletsUsed):F2}," +
                        $"{groupResults.Average(r => r.AvgVolumeUtilization):F4}," +
                        $"{groupResults.Average(r => r.AvgHeterogeneity):F4}"
                    );
                }
            }

            Console.WriteLine($"✓ Summary statistics written to {outputPath}");
        }

        /// <summary>
        /// 통계 라인을 작성합니다 (평균, 최소, 최대, 표준편차).
        /// </summary>
        private static void WriteStatLine(StreamWriter writer, string metric, IEnumerable<double> values)
        {
            var valueList = values.ToList();
            if (!valueList.Any())
            {
                writer.WriteLine($"{metric},N/A,N/A,N/A,N/A");
                return;
            }

            double avg = valueList.Average();
            double min = valueList.Min();
            double max = valueList.Max();
            double stdDev = CalculateStdDev(valueList);

            writer.WriteLine($"{metric},{avg:F4},{min:F4},{max:F4},{stdDev:F4}");
        }

        /// <summary>
        /// 표준편차를 계산합니다.
        /// </summary>
        private static double CalculateStdDev(List<double> values)
        {
            if (values.Count <= 1) return 0;

            double avg = values.Average();
            double sumSquaredDiff = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sumSquaredDiff / values.Count);
        }

        /// <summary>
        /// 벤치마크 결과를 콘솔에 출력합니다.
        /// </summary>
        public static void PrintBenchmarkResult(BenchmarkResult result)
        {
            Console.WriteLine("\n" + new string('═', 70));
            Console.WriteLine($"  BENCHMARK RESULT: {result.OrderId}");
            Console.WriteLine(new string('═', 70));

            Console.WriteLine($"  Algorithm: {result.Algorithm}");
            Console.WriteLine($"  Items: {result.ItemCount} | Product Types: {result.ProductTypes}");
            Console.WriteLine($"  Complexity: {result.Complexity} (Entropy: {result.Entropy:F4})");
            Console.WriteLine(new string('─', 70));

            Console.WriteLine($"  Pallets Used: {result.PalletsUsed}");
            Console.WriteLine($"  Placement Rate: {result.PlacementRate:F2}% ({result.ItemsPlaced}/{result.ItemCount})");
            Console.WriteLine($"  Volume Utilization: {result.AvgVolumeUtilization:P2} " +
                $"(Min: {result.MinVolumeUtilization:P2}, Max: {result.MaxVolumeUtilization:P2})");
            Console.WriteLine($"  Height Utilization: {result.AvgHeightUtilization:P2}");
            Console.WriteLine(new string('─', 70));

            Console.WriteLine($"  Quality Metrics:");
            Console.WriteLine($"    Heterogeneity: {result.AvgHeterogeneity:F4}");
            Console.WriteLine($"    Compactness: {result.AvgCompactness:F4}");
            Console.WriteLine($"    Stability: {result.AvgStability:F4}");
            Console.WriteLine(new string('─', 70));

            Console.WriteLine($"  Execution Time: {result.ExecutionTimeSec:F3}s ({result.ExecutionTimeMs:F2}ms)");
            Console.WriteLine($"  Time per Item: {result.TimePerItem:F3}ms");
            Console.WriteLine($"  Time per Pallet: {result.TimePerPallet:F2}ms");
            Console.WriteLine(new string('═', 70) + "\n");
        }
    }
}
