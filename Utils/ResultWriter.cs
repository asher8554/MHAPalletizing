using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MHAPalletizing.Models;

namespace MHAPalletizing.Utils
{
    /// <summary>
    /// 팔레타이징 결과를 CSV 파일로 출력하는 유틸리티
    /// 논문 형식에 맞춰 결과를 기록합니다.
    /// </summary>
    public class ResultWriter
    {
        /// <summary>
        /// 단일 주문의 결과를 CSV에 추가합니다.
        /// </summary>
        public static void AppendOrderResult(string outputPath, Order order, List<Pallet> pallets,
            double executionTimeMs, string algorithm = "MHA")
        {
            bool fileExists = File.Exists(outputPath);

            using (var writer = new StreamWriter(outputPath, append: true))
            {
                // 헤더 작성 (파일이 새로 만들어진 경우만)
                if (!fileExists)
                {
                    writer.WriteLine("OrderId,Algorithm,ItemCount,ProductTypes,Entropy,Complexity," +
                        "PalletsUsed,ItemsPlaced,ItemsUnplaced,AvgVolumeUtilization," +
                        "AvgHeightUtilization,TotalWeight,AvgHeterogeneity,AvgCompactness," +
                        "ExecutionTimeMs");
                }

                // 결과 계산
                int itemsPlaced = pallets.Sum(p => p.Items.Count);
                int itemsUnplaced = order.Items.Count - itemsPlaced;
                double avgVolumeUtilization = pallets.Any() ? pallets.Average(p => p.VolumeUtilization) : 0;
                double avgHeightUtilization = pallets.Any() ?
                    pallets.Average(p => p.CurrentHeight / p.MaxHeight) : 0;
                double totalWeight = pallets.Sum(p => p.TotalWeight);

                // Heterogeneity와 Compactness 계산
                double avgHeterogeneity = 0;
                double avgCompactness = 0;
                if (pallets.Any())
                {
                    foreach (var pallet in pallets)
                    {
                        // Heterogeneity: 팔레트 내 제품 타입 수 / 전체 제품 타입 수
                        int productTypesInPallet = pallet.Items.Select(i => i.ProductId).Distinct().Count();
                        avgHeterogeneity += (double)productTypesInPallet / order.ProductTypeCount;

                        // Compactness: 평균 사용 밀도
                        avgCompactness += pallet.GetAverageCompactness();
                    }
                    avgHeterogeneity /= pallets.Count;
                    avgCompactness /= pallets.Count;
                }

                // CSV 라인 작성
                writer.WriteLine($"{order.OrderId},{algorithm},{order.TotalItemCount}," +
                    $"{order.ProductTypeCount},{order.Entropy:F4},{order.GetComplexityClass()}," +
                    $"{pallets.Count},{itemsPlaced},{itemsUnplaced}," +
                    $"{avgVolumeUtilization:F4},{avgHeightUtilization:F4},{totalWeight:F2}," +
                    $"{avgHeterogeneity:F4},{avgCompactness:F4},{executionTimeMs:F2}");
            }

            Console.WriteLine($"✓ Result appended to {outputPath}");
        }

        /// <summary>
        /// 상세한 팔레트별 결과를 CSV로 출력합니다.
        /// </summary>
        public static void WriteDetailedResults(string outputPath, Order order, List<Pallet> pallets)
        {
            using (var writer = new StreamWriter(outputPath))
            {
                // 헤더
                writer.WriteLine("OrderId,PalletId,ItemCount,ProductTypes,VolumeUtilization," +
                    "HeightUtilization,Weight,Heterogeneity,Compactness,Products");

                foreach (var pallet in pallets)
                {
                    int productTypes = pallet.Items.Select(i => i.ProductId).Distinct().Count();
                    double volumeUtil = pallet.VolumeUtilization;
                    double heightUtil = pallet.CurrentHeight / pallet.MaxHeight;
                    double weight = pallet.TotalWeight;
                    double heterogeneity = (double)productTypes / order.ProductTypeCount;
                    double compactness = pallet.GetAverageCompactness();

                    // 제품별 수량
                    var productCounts = pallet.Items.GroupBy(i => i.ProductId)
                        .Select(g => $"{g.Key}({g.Count()})")
                        .ToList();
                    string productsStr = string.Join(";", productCounts);

                    writer.WriteLine($"{order.OrderId},{pallet.PalletId},{pallet.Items.Count}," +
                        $"{productTypes},{volumeUtil:F4},{heightUtil:F4},{weight:F2}," +
                        $"{heterogeneity:F4},{compactness:F4},\"{productsStr}\"");
                }
            }

            Console.WriteLine($"✓ Detailed results written to {outputPath}");
        }

        /// <summary>
        /// 아이템별 배치 결과를 CSV로 출력합니다.
        /// </summary>
        public static void WriteItemPlacements(string outputPath, Order order, List<Pallet> pallets)
        {
            using (var writer = new StreamWriter(outputPath))
            {
                // 헤더 (Pallet 크기 정보 포함)
                writer.WriteLine("OrderId,PalletId,ItemId,ProductId,X,Y,Z," +
                    "Length,Width,Height,Weight,IsRotated,PalletLength,PalletWidth,PalletMaxHeight");

                foreach (var pallet in pallets)
                {
                    foreach (var item in pallet.Items)
                    {
                        writer.WriteLine($"{order.OrderId},{pallet.PalletId},{item.ItemId}," +
                            $"{item.ProductId},{item.X:F2},{item.Y:F2},{item.Z:F2}," +
                            $"{item.CurrentLength:F2},{item.CurrentWidth:F2},{item.CurrentHeight:F2}," +
                            $"{item.Weight:F2},{item.IsRotated}," +
                            $"{pallet.Length:F2},{pallet.Width:F2},{pallet.MaxHeight:F2}");
                    }
                }
            }

            Console.WriteLine($"✓ Item placements written to {outputPath}");
        }

        /// <summary>
        /// 결과 요약을 콘솔에 출력합니다.
        /// </summary>
        public static void PrintSummary(Order order, List<Pallet> pallets, double executionTimeMs)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"주문 ID: {order.OrderId}");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"총 아이템 수: {order.TotalItemCount}");
            Console.WriteLine($"제품 타입 수: {order.ProductTypeCount}");
            Console.WriteLine($"Entropy: {order.Entropy:F4} ({order.GetComplexityClass()})");
            Console.WriteLine($"사용 팔레트: {pallets.Count}개");

            int itemsPlaced = pallets.Sum(p => p.Items.Count);
            Console.WriteLine($"배치된 아이템: {itemsPlaced}/{order.TotalItemCount} " +
                $"({(double)itemsPlaced / order.TotalItemCount:P1})");

            if (pallets.Any())
            {
                Console.WriteLine($"평균 부피 활용률: {pallets.Average(p => p.VolumeUtilization):P2}");
                Console.WriteLine($"평균 높이 활용률: {pallets.Average(p => p.CurrentHeight / p.MaxHeight):P2}");
                Console.WriteLine($"총 무게: {pallets.Sum(p => p.TotalWeight):F2}kg");
            }

            Console.WriteLine($"실행 시간: {executionTimeMs:F2}ms ({executionTimeMs / 1000:F2}초)");
            Console.WriteLine(new string('=', 60) + "\n");
        }
    }
}
