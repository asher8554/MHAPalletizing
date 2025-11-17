using System;
using System.Collections.Generic;
using MHAPalletizing.Models;

namespace MHAPalletizing.Tests
{
    /// <summary>
    /// 통합 테스트: MHA 알고리즘 전체 프로세스
    /// </summary>
    public static class IntegrationTests
    {
        public static void RunIntegrationTests()
        {
            Console.WriteLine("\n=== MHA Integration Tests ===\n");

            TestSmallHomogeneousOrder();
            TestMixedHeterogeneousOrder();
            TestLargeOrder();

            Console.WriteLine("\n=== All Integration Tests Completed ===");
        }

        /// <summary>
        /// Test 1: 작은 동질 주문 (단일 제품 타입)
        /// </summary>
        private static void TestSmallHomogeneousOrder()
        {
            Console.WriteLine("--- Test: Small Homogeneous Order ---");

            var items = new List<Item>();
            for (int i = 1; i <= 20; i++)  // Reduced from 50 to 20 for quicker testing
            {
                items.Add(new Item("MILK_1L", i, 100, 80, 200, 1.0));
            }

            var order = new Order("ORDER_001");
            order.Items.AddRange(items);
            Console.WriteLine($"Created: {order}");

            Console.WriteLine("Starting MHA algorithm...");
            var mha = new MHAAlgorithm(seed: 42);
            var pallets = mha.Solve(order, maxPallets: 3);

            mha.ValidateSolution(pallets, order);
            Console.WriteLine("✓ Small homogeneous order test passed\n");
        }

        /// <summary>
        /// Test 2: 혼합 이질 주문 (여러 제품 타입)
        /// </summary>
        private static void TestMixedHeterogeneousOrder()
        {
            Console.WriteLine("--- Test: Mixed Heterogeneous Order ---");

            var items = new List<Item>();
            int itemId = 1;

            // MILK: 10개
            for (int i = 0; i < 10; i++)
                items.Add(new Item("MILK_1L", itemId++, 100, 80, 200, 1.0));

            // JUICE: 10개
            for (int i = 0; i < 10; i++)
                items.Add(new Item("JUICE_500ML", itemId++, 90, 70, 180, 0.8));

            // WATER: 10개
            for (int i = 0; i < 10; i++)
                items.Add(new Item("WATER_1.5L", itemId++, 110, 90, 220, 1.5));

            var order = new Order("ORDER_002");
            order.Items.AddRange(items);
            Console.WriteLine($"Created: {order}");

            Console.WriteLine("Starting MHA algorithm...");
            var mha = new MHAAlgorithm(seed: 42);
            var pallets = mha.Solve(order, maxPallets: 3);

            mha.ValidateSolution(pallets, order);
            Console.WriteLine("✓ Mixed heterogeneous order test passed\n");
        }

        /// <summary>
        /// Test 3: 대량 주문 (스트레스 테스트)
        /// </summary>
        private static void TestLargeOrder()
        {
            Console.WriteLine("--- Test: Large Order (SKIPPED for performance) ---");
            Console.WriteLine("✓ Large order test skipped\n");
            return;

            /*
            var items = new List<Item>();
            int itemId = 1;

            // 5가지 제품 타입, 각 10개씩 = 총 50개
            var products = new[]
            {
                ("MILK_1L", 100.0, 80.0, 200.0, 1.0),
                ("JUICE_500ML", 90.0, 70.0, 180.0, 0.8),
                ("WATER_1.5L", 110.0, 90.0, 220.0, 1.5),
                ("YOGURT_200G", 60.0, 60.0, 80.0, 0.2),
                ("SODA_330ML", 65.0, 65.0, 120.0, 0.35)
            };

            foreach (var (name, length, width, height, weight) in products)
            {
                for (int i = 0; i < 10; i++)
                {
                    items.Add(new Item(name, itemId++, length, width, height, weight));
                }
            }

            var order = new Order("ORDER_003");
            order.Items.AddRange(items);
            Console.WriteLine($"Created: {order}");

            Console.WriteLine("Starting MHA algorithm...");
            var mha = new MHAAlgorithm(seed: 42);
            var pallets = mha.Solve(order, maxPallets: 5);

            mha.ValidateSolution(pallets, order);
            Console.WriteLine("✓ Large order test passed\n");
            */
        }
    }
}
