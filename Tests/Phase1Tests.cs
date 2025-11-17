using System;
using System.Linq;
using MHAPalletizing.Models;
using MHAPalletizing.Phase1;
using MHAPalletizing.Constraints;

namespace MHAPalletizing.Tests
{
    /// <summary>
    /// Phase 1 (Layer & Block Building) 테스트
    /// </summary>
    public class Phase1Tests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Phase 1 Tests ===\n");

            TestLayerCreation();
            TestFullLayerGeneration();
            TestMixedLayerGeneration();

            Console.WriteLine("=== Phase 1 Tests Completed ===\n");
        }

        private static void TestLayerCreation()
        {
            Console.WriteLine("--- Test: Layer Creation ---");

            var layer = new Layer(1, LayerType.Full, 1200 * 800, 200);

            var item1 = new Item("MILK", 1, 100, 80, 200, 1.5);
            item1.Place(0, 0, 0);
            layer.AddItem(item1);

            var item2 = new Item("MILK", 2, 100, 80, 200, 1.5);
            item2.Place(100, 0, 0);
            layer.AddItem(item2);

            Console.WriteLine($"Created: {layer}");
            Console.WriteLine($"Fill Rate: {layer.FillRate:P2}");
            Console.WriteLine($"Homogeneous: {layer.IsHomogeneous}");

            Console.WriteLine("✓ Layer creation test passed\n");
        }

        private static void TestFullLayerGeneration()
        {
            Console.WriteLine("--- Test: Full Layer Generation ---");

            // 동일한 크기의 아이템으로 주문 생성
            var order = new Order("TEST_ORDER_001");
            order.AddItem("MILK_1L", 100, 80, 200, 1.5, quantity: 100);

            Console.WriteLine($"Order: {order.TotalItemCount} items ({order.ProductTypeCount} types)");

            // Layer 생성
            var layers = LayerBuilder.BuildLayers(order);

            Console.WriteLine($"Generated {layers.Count} layers:");
            foreach (var layer in layers.Take(5)) // 처음 5개만 출력
            {
                Console.WriteLine($"  {layer}");
            }

            // 통계
            int fullLayers = layers.Count(l => l.Type == LayerType.Full);
            int halfLayers = layers.Count(l => l.Type == LayerType.Half);
            int quarterLayers = layers.Count(l => l.Type == LayerType.Quarter);

            Console.WriteLine($"\nLayer Distribution:");
            Console.WriteLine($"  Full: {fullLayers}, Half: {halfLayers}, Quarter: {quarterLayers}");

            int totalItemsInLayers = layers.Sum(l => l.Items.Count);
            Console.WriteLine($"  Total items in layers: {totalItemsInLayers} / {order.TotalItemCount}");
            Console.WriteLine($"  Coverage: {(double)totalItemsInLayers / order.TotalItemCount:P2}");

            Console.WriteLine("✓ Full layer generation test passed\n");
        }

        private static void TestMixedLayerGeneration()
        {
            Console.WriteLine("--- Test: Mixed Product Layer Generation ---");

            // 다양한 제품으로 주문 생성
            var order = new Order("TEST_ORDER_002");
            order.AddItem("MILK_1L", 100, 80, 200, 1.5, quantity: 50);
            order.AddItem("JUICE_500ML", 80, 60, 150, 0.8, quantity: 30);
            order.AddItem("WATER_2L", 120, 90, 250, 2.0, quantity: 20);

            Console.WriteLine($"Order: {order.TotalItemCount} items ({order.ProductTypeCount} types)");
            Console.WriteLine($"Entropy: {order.Entropy:F3}, Complexity: {order.GetComplexityClass()}");

            // Layer 생성
            var layers = LayerBuilder.BuildLayers(order);

            Console.WriteLine($"\nGenerated {layers.Count} layers:");

            // Product별 Layer 분포
            var layersByProduct = layers
                .Where(l => l.Items.Any())
                .GroupBy(l => l.Items[0].ProductId)
                .ToList();

            foreach (var group in layersByProduct)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()} layers, {group.Sum(l => l.Items.Count)} items");
            }

            int totalItemsInLayers = layers.Sum(l => l.Items.Count);
            int residuals = order.TotalItemCount - totalItemsInLayers;

            Console.WriteLine($"\nCoverage:");
            Console.WriteLine($"  Packed in layers: {totalItemsInLayers} / {order.TotalItemCount} ({(double)totalItemsInLayers / order.TotalItemCount:P2})");
            Console.WriteLine($"  Residuals: {residuals} items ({(double)residuals / order.TotalItemCount:P2})");

            Console.WriteLine("✓ Mixed product layer generation test passed\n");
        }

        private static void TestBlockGeneration()
        {
            Console.WriteLine("--- Test: Block Generation from Layers ---");

            // 주문 생성
            var order = new Order("TEST_ORDER_003");
            order.AddItem("MILK_1L", 100, 80, 200, 1.5, quantity: 120);
            order.AddItem("JUICE_500ML", 80, 60, 150, 0.8, quantity: 60);

            Console.WriteLine($"Order: {order.TotalItemCount} items ({order.ProductTypeCount} types)");

            // Layer 생성
            var layers = LayerBuilder.BuildLayers(order);
            Console.WriteLine($"Generated {layers.Count} layers");

            // Block 생성
            var blocks = BlockBuilder.BuildBlocks(layers, out var remainingLayers);

            Console.WriteLine($"\nGenerated {blocks.Count} blocks:");
            foreach (var block in blocks)
            {
                Console.WriteLine($"  {block}");
                Console.WriteLine($"    Volume Utilization: {block.Pallet.VolumeUtilization:P2}");
                Console.WriteLine($"    Stability: {(ConstraintValidator.ValidateStability(block.Pallet) ? "✓" : "✗")}");
            }

            int totalPackedItems = blocks.Sum(b => b.TotalItems);
            int residualCount = remainingLayers.Sum(l => l.Items.Count);

            Console.WriteLine($"\nPacking Summary:");
            Console.WriteLine($"  Items in blocks: {totalPackedItems} / {order.TotalItemCount}");
            Console.WriteLine($"  Residual layers: {remainingLayers.Count}, Items: {residualCount}");
            Console.WriteLine($"  Coverage: {(double)totalPackedItems / order.TotalItemCount:P2}");

            Console.WriteLine("✓ Block generation test passed\n");
        }

        public static void RunAllTestsWithBlocks()
        {
            Console.WriteLine("=== Phase 1 Complete Tests (with Blocks) ===\n");

            TestLayerCreation();
            TestFullLayerGeneration();
            TestMixedLayerGeneration();
            TestBlockGeneration();

            Console.WriteLine("=== Phase 1 Complete Tests Finished ===\n");
        }
    }
}
