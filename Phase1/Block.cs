using System;
using System.Collections.Generic;
using System.Linq;
using MHAPalletizing.Models;

namespace MHAPalletizing.Phase1
{
    /// <summary>
    /// Layer들을 적층한 Block
    /// 논문 Section IV-B-2: "layers are stacked to produce blocks"
    /// </summary>
    public class Block
    {
        public int BlockId { get; set; }
        public List<Layer> Layers { get; set; }
        public Pallet Pallet { get; set; }

        // Block 통계
        public double CurrentHeight => Layers.Any() ? Layers.Sum(l => l.Height) : 0;
        public double RemainingHeight => Pallet.MaxHeight - CurrentHeight;
        public int TotalItems => Layers.Sum(l => l.Items.Count);
        public int ProductTypeCount => Layers.SelectMany(l => l.Items)
                                             .Select(i => i.ProductId)
                                             .Distinct()
                                             .Count();

        public Block(int blockId, Pallet pallet)
        {
            BlockId = blockId;
            Pallet = pallet;
            Layers = new List<Layer>();
        }

        /// <summary>
        /// Layer를 Block에 추가
        /// </summary>
        public bool AddLayer(Layer layer)
        {
            // 높이 초과 확인
            if (CurrentHeight + layer.Height > Pallet.MaxHeight)
                return false;

            // Layer를 현재 높이에 배치
            double z = CurrentHeight;
            layer.PlaceAt(0, 0, z);

            Layers.Add(layer);

            // Pallet에 아이템 추가
            foreach (var item in layer.Items)
            {
                Pallet.AddItem(item);
            }

            return true;
        }

        /// <summary>
        /// 특정 위치(Half/Quarter)에 Layer 추가
        /// </summary>
        public bool AddLayerAtPosition(Layer layer, double x, double y)
        {
            if (CurrentHeight + layer.Height > Pallet.MaxHeight)
                return false;

            double z = CurrentHeight;
            layer.PlaceAt(x, y, z);

            Layers.Add(layer);

            foreach (var item in layer.Items)
            {
                // 아이템 위치를 Layer 위치에 맞게 조정
                item.X += x;
                item.Y += y;
                item.Z = z;
                Pallet.AddItem(item);
            }

            return true;
        }

        /// <summary>
        /// Block의 4개 사분면 (Quarter Layer 배치용)
        /// 논문 Section IV-B-2 & Figure 9
        /// </summary>
        public enum Quadrant
        {
            First = 0,    // (0, 0)
            Second = 1,   // (600, 0)
            Third = 2,    // (600, 400)
            Fourth = 3    // (0, 400)
        }

        /// <summary>
        /// Quadrant 위치 좌표 반환
        /// </summary>
        public (double x, double y) GetQuadrantPosition(Quadrant quadrant)
        {
            double halfLength = Pallet.Length / 2;
            double halfWidth = Pallet.Width / 2;

            switch (quadrant)
            {
                case Quadrant.First:
                    return (0, 0);
                case Quadrant.Second:
                    return (halfLength, 0);
                case Quadrant.Third:
                    return (halfLength, halfWidth);
                case Quadrant.Fourth:
                    return (0, halfWidth);
                default:
                    return (0, 0);
            }
        }

        /// <summary>
        /// 각 Quadrant의 현재 높이 추적
        /// </summary>
        public Dictionary<Quadrant, double> QuadrantHeights { get; set; } = new Dictionary<Quadrant, double>
        {
            { Quadrant.First, 0 },
            { Quadrant.Second, 0 },
            { Quadrant.Third, 0 },
            { Quadrant.Fourth, 0 }
        };

        /// <summary>
        /// 가장 낮은 Quadrant 반환
        /// </summary>
        public Quadrant GetLowestQuadrant()
        {
            return QuadrantHeights.OrderBy(kv => kv.Value).First().Key;
        }

        /// <summary>
        /// Half의 현재 높이 추적
        /// </summary>
        public Dictionary<int, double> HalfHeights { get; set; } = new Dictionary<int, double>
        {
            { 0, 0 }, // First half
            { 1, 0 }  // Second half
        };

        /// <summary>
        /// 가장 낮은 Half 반환
        /// </summary>
        public int GetLowestHalf()
        {
            return HalfHeights.OrderBy(kv => kv.Value).First().Key;
        }

        public override string ToString()
        {
            return $"Block {BlockId}: {Layers.Count} layers, {TotalItems} items, " +
                   $"Height: {CurrentHeight:F0}/{Pallet.MaxHeight:F0}mm, " +
                   $"Products: {ProductTypeCount}";
        }
    }
}
