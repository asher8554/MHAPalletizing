using System;
using System.Collections.Generic;
using MHAPalletizing.Models;
using MHAPalletizing.Constraints;

namespace MHAPalletizing.Tests
{
    /// <summary>
    /// 기본 데이터 구조 및 제약조건 검증 테스트
    /// </summary>
    public class BasicTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== MHA Palletizing - Basic Tests ===\n");

            TestItemCreation();
            TestPalletCreation();
            TestOrderCreation();
            TestConstraint1_Orientation();
            TestConstraint2_NonCollision();
            TestConstraint3_Stability();
            TestConstraint4_Support();

            Console.WriteLine("\n=== All Basic Tests Completed ===");
        }

        #region Item Tests
        private static void TestItemCreation()
        {
            Console.WriteLine("--- Test: Item Creation ---");

            var item = new Item("MILK_1L", 1, 100, 80, 200, 1.5);
            Console.WriteLine($"Created: {item}");

            // 회전 테스트
            item.Place(10, 20, 0, rotated: true);
            Console.WriteLine($"After rotation: Length={item.CurrentLength}, Width={item.CurrentWidth}");
            Console.WriteLine($"Position: ({item.X}, {item.Y}, {item.Z}), Rotated={item.IsRotated}");

            // 무게 중심
            var (cx, cy, cz) = item.GetCenterOfMass();
            Console.WriteLine($"Center of Mass: ({cx:F1}, {cy:F1}, {cz:F1})");

            Console.WriteLine("✓ Item creation test passed\n");
        }
        #endregion

        #region Pallet Tests
        private static void TestPalletCreation()
        {
            Console.WriteLine("--- Test: Pallet Creation ---");

            var pallet = new Pallet(1);
            Console.WriteLine($"Created: {pallet}");
            Console.WriteLine($"Dimensions: {pallet.Length} x {pallet.Width} x {pallet.MaxHeight}");
            Console.WriteLine($"Total Volume: {pallet.TotalVolume} mm³");

            // 아이템 추가
            var item1 = new Item("MILK_1L", 1, 100, 80, 200, 1.5);
            item1.Place(0, 0, 0);
            pallet.AddItem(item1);

            var item2 = new Item("JUICE_500ML", 2, 80, 60, 150, 0.8);
            item2.Place(100, 0, 0);
            pallet.AddItem(item2);

            Console.WriteLine($"After adding 2 items: {pallet}");
            Console.WriteLine($"Volume Utilization: {pallet.VolumeUtilization:P4}");

            var (comX, comY, comZ) = pallet.GetCenterOfMass();
            Console.WriteLine($"Center of Mass: ({comX:F1}, {comY:F1}, {comZ:F1})");

            Console.WriteLine("✓ Pallet creation test passed\n");
        }
        #endregion

        #region Order Tests
        private static void TestOrderCreation()
        {
            Console.WriteLine("--- Test: Order Creation ---");

            var order = new Order("ORDER_001");

            // 다양한 제품 추가
            order.AddItem("MILK_1L", 100, 80, 200, 1.5, quantity: 50);
            order.AddItem("JUICE_500ML", 80, 60, 150, 0.8, quantity: 30);
            order.AddItem("WATER_2L", 120, 90, 250, 2.0, quantity: 20);

            Console.WriteLine($"Created: {order}");
            Console.WriteLine($"Total Items: {order.TotalItemCount}");
            Console.WriteLine($"Product Types: {order.ProductTypeCount}");
            Console.WriteLine($"Entropy: {order.Entropy:F4}");
            Console.WriteLine($"Complexity: {order.GetComplexityClass()}");
            Console.WriteLine($"Size Class: {order.GetSizeClass()}");

            Console.WriteLine("✓ Order creation test passed\n");
        }
        #endregion

        #region Constraint Tests
        private static void TestConstraint1_Orientation()
        {
            Console.WriteLine("--- Test: Constraint 1 - Item Orientation ---");

            var item1 = new Item("TEST", 1, 100, 80, 200, 1.0);
            item1.Place(0, 0, 0, rotated: false); // 0도
            bool valid1 = ConstraintValidator.ValidateOrientation(item1);
            Console.WriteLine($"Item with 0° rotation: {(valid1 ? "✓ Valid" : "✗ Invalid")}");

            var item2 = new Item("TEST", 2, 100, 80, 200, 1.0);
            item2.Place(0, 0, 0, rotated: true); // 90도
            bool valid2 = ConstraintValidator.ValidateOrientation(item2);
            Console.WriteLine($"Item with 90° rotation: {(valid2 ? "✓ Valid" : "✗ Invalid")}");

            Console.WriteLine();
        }

        private static void TestConstraint2_NonCollision()
        {
            Console.WriteLine("--- Test: Constraint 2 - Non-Collision ---");

            var existingItems = new List<Item>();
            var item1 = new Item("MILK", 1, 100, 80, 200, 1.5);
            item1.Place(0, 0, 0);
            existingItems.Add(item1);

            // 충돌하지 않는 아이템
            var item2 = new Item("JUICE", 2, 80, 60, 150, 0.8);
            item2.Place(100, 0, 0); // X축으로 100mm 떨어짐
            bool valid1 = ConstraintValidator.ValidateNonCollision(item2, existingItems);
            Console.WriteLine($"Item at (100, 0, 0): {(valid1 ? "✓ No collision" : "✗ Collision detected")}");

            // 충돌하는 아이템
            var item3 = new Item("WATER", 3, 120, 90, 250, 2.0);
            item3.Place(50, 0, 0); // 충돌 위치
            bool valid2 = ConstraintValidator.ValidateNonCollision(item3, existingItems);
            Console.WriteLine($"Item at (50, 0, 0): {(valid2 ? "✓ No collision" : "✗ Collision detected (Expected)")}");

            Console.WriteLine();
        }

        private static void TestConstraint3_Stability()
        {
            Console.WriteLine("--- Test: Constraint 3 - Stability ---");

            var pallet = new Pallet(1);

            // 중앙에 균형있게 배치
            var item1 = new Item("MILK", 1, 100, 80, 200, 1.5);
            item1.Place(550, 360, 0); // 중앙 근처
            pallet.AddItem(item1);

            var item2 = new Item("MILK", 2, 100, 80, 200, 1.5);
            item2.Place(550, 360, 200); // 위에 적층
            pallet.AddItem(item2);

            bool stable = ConstraintValidator.ValidateStability(pallet, tolerance: 0.3);
            var (comX, comY, comZ) = pallet.GetCenterOfMass();
            Console.WriteLine($"Center of Mass: ({comX:F1}, {comY:F1}, {comZ:F1})");
            Console.WriteLine($"Pallet Center: ({pallet.Length / 2}, {pallet.Width / 2})");
            Console.WriteLine($"Stability: {(stable ? "✓ Stable" : "✗ Unstable")}");

            Console.WriteLine();
        }

        private static void TestConstraint4_Support()
        {
            Console.WriteLine("--- Test: Constraint 4 - Support ---");

            var pallet = new Pallet(1);

            // 바닥에 아이템 배치
            var baseItem = new Item("BASE", 1, 200, 200, 100, 2.0);
            baseItem.Place(0, 0, 0);
            pallet.AddItem(baseItem);

            // 위에 잘 지지되는 아이템
            var topItem = new Item("TOP", 2, 150, 150, 100, 1.5);
            topItem.Place(25, 25, 100); // 중앙에 배치

            bool supported = ConstraintValidator.ValidateSupport(topItem, pallet);
            Console.WriteLine($"Item centered on base: {(supported ? "✓ Supported" : "✗ Not supported")}");

            // 공중에 떠있는 아이템
            var floatingItem = new Item("FLOAT", 3, 100, 100, 100, 1.0);
            floatingItem.Place(300, 300, 100); // 지지 없음

            bool notSupported = ConstraintValidator.ValidateSupport(floatingItem, pallet);
            Console.WriteLine($"Floating item: {(notSupported ? "✓ Supported" : "✗ Not supported (Expected)")}");

            Console.WriteLine();
        }
        #endregion
    }
}
