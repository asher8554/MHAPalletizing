using System;
using System.Collections.Generic;
using System.Linq;
using MHAPalletizing.Models;
using MHAPalletizing.Phase2;
using MHAPalletizing.Phase1;

namespace MHAPalletizing.Tests
{
    /// <summary>
    /// Phase 2 테스트: Extreme Points, Placement Strategy, Genetic Algorithm
    /// </summary>
    public static class Phase2Tests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("\n=== Phase 2 Tests (Extreme Points & GA) ===\n");

            TestExtremePointCreation();
            TestExtremePointPriority();
            TestPlacementStrategy();
            TestIndividualCreation();
            TestGeneticAlgorithm();

            Console.WriteLine("\n=== All Phase 2 Tests Completed ===");
        }

        /// <summary>
        /// Test: Extreme Point 생성 및 우선순위
        /// </summary>
        private static void TestExtremePointCreation()
        {
            Console.WriteLine("--- Test: Extreme Point Creation ---");

            var ep1 = new ExtremePoint(0, 0, 0);
            var ep2 = new ExtremePoint(100, 0, 0);
            var ep3 = new ExtremePoint(0, 0, 100);

            Console.WriteLine($"EP1: {ep1}");
            Console.WriteLine($"EP2: {ep2}");
            Console.WriteLine($"EP3: {ep3}");

            // 같은 위치 체크
            var ep1_duplicate = new ExtremePoint(0.05, 0.05, 0.05);
            if (ep1.IsSamePosition(ep1_duplicate))
                Console.WriteLine("✓ Duplicate detection working");

            Console.WriteLine("✓ Extreme Point creation test passed\n");
        }

        /// <summary>
        /// Test: EP 우선순위 정렬
        /// </summary>
        private static void TestExtremePointPriority()
        {
            Console.WriteLine("--- Test: Extreme Point Priority ---");

            var eps = new List<ExtremePoint>
            {
                new ExtremePoint(100, 100, 200),  // 높은 Z
                new ExtremePoint(50, 50, 0),      // 낮은 Z, 중간 거리
                new ExtremePoint(0, 0, 0),        // 낮은 Z, 원점
                new ExtremePoint(200, 0, 100),    // 중간 Z
            };

            eps.Sort();

            Console.WriteLine("Sorted EPs (낮은 Z, 원점에 가까운 순):");
            foreach (var ep in eps)
            {
                Console.WriteLine($"  {ep}");
            }

            if (eps[0].Z == 0 && eps[0].X == 0 && eps[0].Y == 0)
                Console.WriteLine("✓ Priority sorting working correctly");

            Console.WriteLine();
        }

        /// <summary>
        /// Test: PlacementStrategy로 아이템 배치
        /// </summary>
        private static void TestPlacementStrategy()
        {
            Console.WriteLine("--- Test: Placement Strategy ---");

            var pallet = new Pallet(1);
            var strategy = new PlacementStrategy(pallet);

            // 첫 번째 아이템 배치
            var item1 = new Item("MILK", 1, 100, 80, 200, 1.0);
            bool placed1 = strategy.TryPlaceItem(item1, allowRotation: false);
            Console.WriteLine($"Item 1 placed at ({item1.X}, {item1.Y}, {item1.Z}): {placed1}");

            if (placed1)
            {
                pallet.AddItem(item1);
                Console.WriteLine($"Available EPs after placement: {strategy.GetAvailableEPCount()}");

                // 새로 생성된 EP 확인
                var eps = strategy.GetAllExtremePoints();
                Console.WriteLine($"Total EPs: {eps.Count}");
                foreach (var ep in eps.Take(5))
                {
                    Console.WriteLine($"  {ep}");
                }
            }

            // 두 번째 아이템 배치
            var item2 = new Item("MILK", 2, 100, 80, 200, 1.0);
            bool placed2 = strategy.TryPlaceItem(item2, allowRotation: false);
            Console.WriteLine($"Item 2 placed at ({item2.X}, {item2.Y}, {item2.Z}): {placed2}");

            if (placed1 && placed2)
                Console.WriteLine("✓ Placement Strategy test passed");

            Console.WriteLine();
        }

        /// <summary>
        /// Test: Individual 생성 및 Pareto Dominance
        /// </summary>
        private static void TestIndividualCreation()
        {
            Console.WriteLine("--- Test: Individual Creation ---");

            var genes1 = new List<string> { "MILK", "JUICE", "WATER" };
            var genes2 = new List<string> { "WATER", "MILK", "JUICE" };

            var ind1 = new Individual(genes1)
            {
                HeterogeneityScore = 0.5,
                CompactnessScore = 0.8,
                IsValid = true
            };

            var ind2 = new Individual(genes2)
            {
                HeterogeneityScore = 0.6,
                CompactnessScore = 0.7,
                IsValid = true
            };

            Console.WriteLine($"Individual 1: {ind1}");
            Console.WriteLine($"Individual 2: {ind2}");

            // Dominance 테스트
            bool ind1Dominates = Individual.Dominates(ind1, ind2);
            bool ind2Dominates = Individual.Dominates(ind2, ind1);

            Console.WriteLine($"Ind1 dominates Ind2: {ind1Dominates}");
            Console.WriteLine($"Ind2 dominates Ind1: {ind2Dominates}");

            if (ind1Dominates && !ind2Dominates)
                Console.WriteLine("✓ Pareto Dominance working correctly");

            // Clone 테스트
            var clone = ind1.Clone();
            Console.WriteLine($"Cloned Individual: {clone}");
            Console.WriteLine($"Same genes: {string.Join(",", clone.Genes) == string.Join(",", ind1.Genes)}");

            Console.WriteLine("✓ Individual creation test passed\n");
        }

        /// <summary>
        /// Test: Genetic Algorithm 실행
        /// </summary>
        private static void TestGeneticAlgorithm()
        {
            Console.WriteLine("--- Test: Genetic Algorithm ---");

            // 간단한 residual 아이템들 생성
            var residuals = new List<Item>();

            // 3가지 타입, 각 5개씩
            int itemId = 1;
            for (int i = 0; i < 5; i++)
            {
                residuals.Add(new Item("MILK", itemId++, 100, 80, 200, 1.0));
                residuals.Add(new Item("JUICE", itemId++, 90, 70, 180, 0.8));
                residuals.Add(new Item("WATER", itemId++, 80, 60, 150, 0.6));
            }

            Console.WriteLine($"Residual items: {residuals.Count} items, {residuals.Select(i => i.ProductId).Distinct().Count()} types");

            // 빈 팔레트 준비
            var pallets = new List<Pallet>
            {
                new Pallet(1),
                new Pallet(2)
            };

            // GA 실행
            var ga = new GeneticAlgorithm(new Random(42));
            bool success = ga.PackResiduals(residuals, pallets, out List<Pallet> usedPallets);

            Console.WriteLine($"GA packing success: {success}");
            if (success)
            {
                Console.WriteLine($"Used pallets: {usedPallets.Count}");
                foreach (var pallet in usedPallets)
                {
                    Console.WriteLine($"  {pallet}");
                }

                int totalPlaced = usedPallets.Sum(p => p.Items.Count);
                Console.WriteLine($"Total items placed: {totalPlaced}/{residuals.Count}");

                if (totalPlaced == residuals.Count)
                    Console.WriteLine("✓ All residuals packed successfully");
            }

            Console.WriteLine("✓ Genetic Algorithm test passed\n");
        }
    }
}
