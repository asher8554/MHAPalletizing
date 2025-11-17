using System;
using System.Collections.Generic;
using System.Linq;
using MHAPalletizing.Models;

namespace MHAPalletizing.Phase1
{
    /// <summary>
    /// Layer 생성 알고리즘
    /// 논문 Section IV-B-2: "generate homogeneous product layers"
    /// </summary>
    /// <remarks>
    /// LayerBuilder는 동일 제품(homogeneous product)으로 구성된 레이어를 생성합니다.
    /// 3가지 타입의 레이어를 지원:
    /// - Full Layer: 1200mm × 800mm (전체 팔레트 면적)
    /// - Half Layer: 600mm × 800mm (1/2 면적)
    /// - Quarter Layer: 600mm × 400mm (1/4 면적)
    ///
    /// 각 레이어는 최소 Fill Rate 기준을 만족해야 합니다.
    /// </remarks>
    public class LayerBuilder
    {
        // 최소 Fill Rate (논문에서 경험적으로 도출)
        private const double FULL_LAYER_MIN_FILL_RATE = 0.90;
        private const double HALF_LAYER_MIN_FILL_RATE = 0.90;
        private const double QUARTER_LAYER_MIN_FILL_RATE = 0.85;

        // Euro Pallet 치수
        private const double PALLET_LENGTH = 1200;
        private const double PALLET_WIDTH = 800;

        /// <summary>
        /// 주문의 모든 아이템으로부터 가능한 모든 레이어를 생성합니다.
        /// </summary>
        /// <param name="order">레이어를 생성할 주문 객체</param>
        /// <returns>생성된 레이어 목록. Fill Rate가 높은 순으로 정렬됩니다.</returns>
        /// <remarks>
        /// 각 제품 타입(ProductId)별로 다음을 수행:
        /// 1. Full Layer 생성 (Fill Rate ≥ 90%)
        /// 2. Half Layer 생성 (Fill Rate ≥ 90%)
        /// 3. Quarter Layer 생성 (Fill Rate ≥ 85%)
        ///
        /// 각 레이어는 기본 방향과 회전 방향을 모두 시도하여 최적 배치를 선택합니다.
        /// </remarks>
        /// <example>
        /// <code>
        /// var order = new Order("TEST");
        /// order.AddItem("MILK", 100, 80, 200, 1.0, 50);
        ///
        /// var layers = LayerBuilder.BuildLayers(order);
        /// foreach (var layer in layers)
        /// {
        ///     Console.WriteLine($"{layer.Type} Layer: {layer.Items.Count} items, Fill Rate: {layer.FillRate:P2}");
        /// }
        /// </code>
        /// </example>
        public static List<Layer> BuildLayers(Order order)
        {
            var layers = new List<Layer>();

            // Product ID별로 그룹화
            var productGroups = order.Items
                .GroupBy(item => item.ProductId)
                .ToList();

            int layerId = 1;

            foreach (var group in productGroups)
            {
                string productId = group.Key;
                var items = group.ToList();

                // Full Layers 생성
                layers.AddRange(CreateFullLayers(items, ref layerId));

                // Half Layers 생성
                layers.AddRange(CreateHalfLayers(items, ref layerId));

                // Quarter Layers 생성
                layers.AddRange(CreateQuarterLayers(items, ref layerId));
            }

            return layers;
        }

        #region Full Layer Creation
        private static List<Layer> CreateFullLayers(List<Item> items, ref int layerId)
        {
            var layers = new List<Layer>();
            var remainingItems = new List<Item>(items);

            while (remainingItems.Any())
            {
                var layer = CreateSingleFullLayer(remainingItems, layerId);

                if (layer == null || layer.FillRate < FULL_LAYER_MIN_FILL_RATE)
                    break; // Fill rate 부족

                layers.Add(layer);
                layerId++;

                // 사용된 아이템 제거
                foreach (var item in layer.Items)
                {
                    remainingItems.Remove(item);
                }
            }

            return layers;
        }

        private static Layer CreateSingleFullLayer(List<Item> availableItems, int layerId)
        {
            if (!availableItems.Any())
                return null;

            double baseArea = PALLET_LENGTH * PALLET_WIDTH;
            var layer = new Layer(layerId, LayerType.Full, baseArea);

            // 대표 아이템 (첫 번째)
            var sampleItem = availableItems[0];
            double itemLength = sampleItem.Length;
            double itemWidth = sampleItem.Width;
            double itemHeight = sampleItem.Height;

            // 두 가지 방향으로 배치 시도
            var pattern1 = TryFillLayer(PALLET_LENGTH, PALLET_WIDTH, itemLength, itemWidth, itemHeight, availableItems, defaultOrientation: false);
            var pattern2 = TryFillLayer(PALLET_LENGTH, PALLET_WIDTH, itemLength, itemWidth, itemHeight, availableItems, defaultOrientation: true);

            // 더 많은 아이템을 담는 패턴 선택
            LayerPattern selectedPattern;
            if (pattern1.ItemCount > pattern2.ItemCount)
                selectedPattern = pattern1;
            else if (pattern2.ItemCount > pattern1.ItemCount)
                selectedPattern = pattern2;
            else
                // 같은 개수면 기본 방향이 더 많은 것 선택
                selectedPattern = pattern1.DefaultOrientationCount >= pattern2.DefaultOrientationCount ? pattern1 : pattern2;

            // Layer에 아이템 배치
            PlaceItemsInLayer(layer, selectedPattern, availableItems);

            return layer;
        }
        #endregion

        #region Half Layer Creation
        private static List<Layer> CreateHalfLayers(List<Item> items, ref int layerId)
        {
            var layers = new List<Layer>();
            var remainingItems = new List<Item>(items);

            while (remainingItems.Any())
            {
                var layer = CreateSingleHalfLayer(remainingItems, layerId);

                if (layer == null || layer.FillRate < HALF_LAYER_MIN_FILL_RATE)
                    break;

                layers.Add(layer);
                layerId++;

                foreach (var item in layer.Items)
                {
                    remainingItems.Remove(item);
                }
            }

            return layers;
        }

        private static Layer CreateSingleHalfLayer(List<Item> availableItems, int layerId)
        {
            if (!availableItems.Any())
                return null;

            double halfLength = PALLET_LENGTH / 2;
            double baseArea = halfLength * PALLET_WIDTH;
            var layer = new Layer(layerId, LayerType.Half, baseArea);

            var sampleItem = availableItems[0];
            var pattern = TryFillLayer(halfLength, PALLET_WIDTH, sampleItem.Length, sampleItem.Width, sampleItem.Height, availableItems, false);

            PlaceItemsInLayer(layer, pattern, availableItems);
            return layer;
        }
        #endregion

        #region Quarter Layer Creation
        private static List<Layer> CreateQuarterLayers(List<Item> items, ref int layerId)
        {
            var layers = new List<Layer>();
            var remainingItems = new List<Item>(items);

            while (remainingItems.Any())
            {
                var layer = CreateSingleQuarterLayer(remainingItems, layerId);

                if (layer == null || layer.FillRate < QUARTER_LAYER_MIN_FILL_RATE)
                    break;

                layers.Add(layer);
                layerId++;

                foreach (var item in layer.Items)
                {
                    remainingItems.Remove(item);
                }
            }

            return layers;
        }

        private static Layer CreateSingleQuarterLayer(List<Item> availableItems, int layerId)
        {
            if (!availableItems.Any())
                return null;

            double quarterLength = PALLET_LENGTH / 2;
            double quarterWidth = PALLET_WIDTH / 2;
            double baseArea = quarterLength * quarterWidth;
            var layer = new Layer(layerId, LayerType.Quarter, baseArea);

            var sampleItem = availableItems[0];
            var pattern = TryFillLayer(quarterLength, quarterWidth, sampleItem.Length, sampleItem.Width, sampleItem.Height, availableItems, false);

            PlaceItemsInLayer(layer, pattern, availableItems);
            return layer;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 특정 영역을 아이템으로 채우는 패턴 계산
        /// 논문 Section IV-B-2: "tests placement in one orientation, evaluates unused area,
        /// attempts to place additional items in second orientation"
        /// </summary>
        private static LayerPattern TryFillLayer(double areaLength, double areaWidth,
            double itemLength, double itemWidth, double itemHeight,
            List<Item> availableItems, bool defaultOrientation)
        {
            var pattern = new LayerPattern { ItemHeight = itemHeight };

            // 기본 방향 배치
            double currentLength = defaultOrientation ? itemLength : itemWidth;
            double currentWidth = defaultOrientation ? itemWidth : itemLength;

            int countX = (int)(areaLength / currentLength);
            int countY = (int)(areaWidth / currentWidth);
            int primaryCount = countX * countY;

            pattern.PrimaryCount = primaryCount;
            pattern.PrimaryRotated = !defaultOrientation;

            // 남은 공간에 회전된 아이템 배치
            double remainingLength = areaLength - (countX * currentLength);
            double remainingWidth = areaWidth - (countY * currentWidth);

            int secondaryCount = 0;

            // 남은 길이 방향에 회전 배치
            if (remainingLength > 0)
            {
                double rotatedLength = defaultOrientation ? itemWidth : itemLength;
                double rotatedWidth = defaultOrientation ? itemLength : itemWidth;

                int countSecondaryX = (int)(remainingLength / rotatedLength);
                int countSecondaryY = (int)(areaWidth / rotatedWidth);
                secondaryCount += countSecondaryX * countSecondaryY;
            }

            // 남은 너비 방향에 회전 배치
            if (remainingWidth > 0)
            {
                double rotatedLength = defaultOrientation ? itemWidth : itemLength;
                double rotatedWidth = defaultOrientation ? itemLength : itemWidth;

                int countSecondaryX = (int)(areaLength / rotatedLength);
                int countSecondaryY = (int)(remainingWidth / rotatedWidth);
                secondaryCount += countSecondaryX * countSecondaryY;
            }

            pattern.SecondaryCount = secondaryCount;
            pattern.SecondaryRotated = defaultOrientation;

            // 사용 가능한 아이템 수로 제한
            int totalNeeded = pattern.PrimaryCount + pattern.SecondaryCount;
            int available = availableItems.Count;

            if (totalNeeded > available)
            {
                int deficit = totalNeeded - available;
                if (pattern.SecondaryCount >= deficit)
                    pattern.SecondaryCount -= deficit;
                else
                {
                    deficit -= pattern.SecondaryCount;
                    pattern.SecondaryCount = 0;
                    pattern.PrimaryCount -= deficit;
                }
            }

            return pattern;
        }

        /// <summary>
        /// Layer에 패턴대로 아이템 배치
        /// 논문 Section IV-B-2: "dynamic shifting approach" (중심으로 밀기)
        /// </summary>
        private static void PlaceItemsInLayer(Layer layer, LayerPattern pattern, List<Item> availableItems)
        {
            double areaLength = layer.Type == LayerType.Full ? PALLET_LENGTH :
                               layer.Type == LayerType.Half ? PALLET_LENGTH / 2 :
                               PALLET_LENGTH / 2;

            double areaWidth = layer.Type == LayerType.Quarter ? PALLET_WIDTH / 2 : PALLET_WIDTH;

            int itemIndex = 0;

            // Primary orientation 배치
            for (int i = 0; i < pattern.PrimaryCount && itemIndex < availableItems.Count; i++)
            {
                var item = availableItems[itemIndex].Clone();
                item.IsRotated = pattern.PrimaryRotated;
                layer.AddItem(item);
                itemIndex++;
            }

            // Secondary orientation 배치
            for (int i = 0; i < pattern.SecondaryCount && itemIndex < availableItems.Count; i++)
            {
                var item = availableItems[itemIndex].Clone();
                item.IsRotated = pattern.SecondaryRotated;
                layer.AddItem(item);
                itemIndex++;
            }

            // Dynamic shifting (중심 정렬) - 논문 Figure 7
            ApplyDynamicShifting(layer, areaLength, areaWidth);
        }

        /// <summary>
        /// Dynamic Shifting: 아이템을 중심으로 밀어서 안정성 향상
        /// 논문: "pushes items to extremities of pallet, improves stability and interlocking"
        /// </summary>
        private static void ApplyDynamicShifting(Layer layer, double areaLength, double areaWidth)
        {
            if (!layer.Items.Any())
                return;

            // 간단한 구현: Grid 기반 배치 후 중심으로 간격 분산
            // 실제로는 더 복잡한 최적화 필요

            // 현재는 기본 Grid 배치만 수행
            double x = 0, y = 0;
            double maxHeightInRow = 0;

            foreach (var item in layer.Items)
            {
                if (x + item.CurrentLength > areaLength)
                {
                    // 다음 행으로
                    x = 0;
                    y += maxHeightInRow;
                    maxHeightInRow = 0;
                }

                item.X = x;
                item.Y = y;

                x += item.CurrentLength;
                maxHeightInRow = Math.Max(maxHeightInRow, item.CurrentWidth);
            }
        }
        #endregion

        /// <summary>
        /// Layer 패턴 정보
        /// </summary>
        private class LayerPattern
        {
            public int PrimaryCount { get; set; }
            public bool PrimaryRotated { get; set; }
            public int SecondaryCount { get; set; }
            public bool SecondaryRotated { get; set; }
            public double ItemHeight { get; set; }

            public int ItemCount => PrimaryCount + SecondaryCount;
            public int DefaultOrientationCount => (PrimaryRotated ? SecondaryCount : PrimaryCount) +
                                                  (PrimaryRotated ? 0 : SecondaryCount);
        }
    }
}
