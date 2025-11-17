using System;
using System.Collections.Generic;
using MHAPalletizing.Models;
using MHAPalletizing.Phase2;

namespace MHAPalletizing.Tests
{
    /// <summary>
    /// 디버깅을 위한 간단한 테스트
    /// </summary>
    public static class DebugTests
    {
        public static void TestSingleItemPlacement()
        {
            Console.WriteLine("\n=== Debug Test: Single Item Placement ===\n");

            // 빈 팔레트 생성
            var pallet = new Pallet(1);
            Console.WriteLine($"Pallet: {pallet.Length}x{pallet.Width}x{pallet.MaxHeight}mm");

            // 작은 아이템 하나 생성 (확실히 들어갈 크기)
            var item = new Item("TEST-001", 1, 100, 80, 150, 1.0);
            Console.WriteLine($"Item: {item.Length}x{item.Width}x{item.Height}mm, Weight:{item.Weight}kg");

            // PlacementStrategy로 배치 시도 (수동으로 테스트)
            var strategy = new PlacementStrategy(pallet);
            Console.WriteLine($"\nAvailable EPs: {strategy.GetAvailableEPCount()}");

            // EP (0,0,0)에서 수동 배치 테스트
            item.Place(0, 0, 0, false);
            Console.WriteLine($"\nManual placement test at (0,0,0):");
            Console.WriteLine($"  Item bounds: X[{item.MinX},{item.MaxX}], Y[{item.MinY},{item.MaxY}], Z[{item.MinZ},{item.MaxZ}]");
            Console.WriteLine($"  Pallet bounds: X[0,{pallet.Length}], Y[0,{pallet.Width}], Z[0,{pallet.MaxHeight}]");
            Console.WriteLine($"  Out of bounds? X:{item.MaxX > pallet.Length}, Y:{item.MaxY > pallet.Width}, Z:{item.MaxZ > pallet.MaxHeight}");

            // Stability 테스트
            var com = pallet.GetCenterOfMass();
            Console.WriteLine($"  Center of Mass (empty): ({com.x:F1}, {com.y:F1}, {com.z:F1})");
            Console.WriteLine($"  Pallet center: ({pallet.Length/2}, {pallet.Width/2})");
            Console.WriteLine($"  Is stable (before adding): {pallet.IsStable(0.3)}");

            pallet.AddItem(item);
            com = pallet.GetCenterOfMass();
            Console.WriteLine($"  Center of Mass (with item): ({com.x:F1}, {com.y:F1}, {com.z:F1})");
            Console.WriteLine($"  Is stable (after adding): {pallet.IsStable(0.3)}");
            pallet.RemoveItem(item);

            bool success = strategy.TryPlaceItem(item, allowRotation: true);

            if (success)
            {
                pallet.AddItem(item);
                Console.WriteLine($"\n✓ SUCCESS!");
                Console.WriteLine($"Item placed at: ({item.X}, {item.Y}, {item.Z})");
                Console.WriteLine($"Item rotated: {item.IsRotated}");
                Console.WriteLine($"Item dimensions: {item.CurrentLength}x{item.CurrentWidth}x{item.CurrentHeight}");
                Console.WriteLine($"Pallet items: {pallet.Items.Count}");
            }
            else
            {
                Console.WriteLine($"\n✗ FAILED to place item!");
                Console.WriteLine($"This should never happen for a small item in an empty pallet.");
            }
        }

        public static void TestDatasetItem()
        {
            Console.WriteLine("\n=== Debug Test: Real Dataset Item ===\n");

            // Dataset10에서 실제 아이템 크기 사용
            var pallet = new Pallet(1);
            Console.WriteLine($"Pallet: {pallet.Length}x{pallet.Width}x{pallet.MaxHeight}mm");

            // Order 16129의 첫 번째 아이템 (Product 93215: 290x240x170mm, 1.36kg)
            var item = new Item("93215", 1, 290, 240, 170, 1.36);
            Console.WriteLine($"Item: {item.Length}x{item.Width}x{item.Height}mm, Weight:{item.Weight}kg");

            var strategy = new PlacementStrategy(pallet);
            bool success = strategy.TryPlaceItem(item, allowRotation: true);

            if (success)
            {
                pallet.AddItem(item);
                Console.WriteLine($"\n✓ SUCCESS!");
                Console.WriteLine($"Item placed at: ({item.X}, {item.Y}, {item.Z})");
                Console.WriteLine($"Pallet volume utilization: {pallet.VolumeUtilization:P2}");
            }
            else
            {
                Console.WriteLine($"\n✗ FAILED!");
            }
        }

        public static void TestMultipleItems()
        {
            Console.WriteLine("\n=== Debug Test: Multiple Items ===\n");

            var pallet = new Pallet(1);
            var strategy = new PlacementStrategy(pallet);

            // 여러 아이템 배치 시도
            var items = new List<Item>
            {
                new Item("A", 1, 300, 200, 150, 2.0),
                new Item("B", 2, 250, 180, 120, 1.5),
                new Item("C", 3, 200, 150, 100, 1.0),
            };

            int placedCount = 0;
            foreach (var item in items)
            {
                if (strategy.TryPlaceItem(item, allowRotation: true))
                {
                    pallet.AddItem(item);
                    placedCount++;
                    Console.WriteLine($"✓ Placed {item.ProductId} at ({item.X}, {item.Y}, {item.Z})");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to place {item.ProductId}");
                }
            }

            Console.WriteLine($"\nPlaced: {placedCount}/{items.Count} items");
            Console.WriteLine($"Pallet utilization: {pallet.VolumeUtilization:P2}");
        }
    }
}
