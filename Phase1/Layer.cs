using System;
using System.Collections.Generic;
using MHAPalletizing.Models;

namespace MHAPalletizing.Phase1
{
    /// <summary>
    /// Layer 타입 (논문 Section IV-B-2)
    /// Full: 1200 x 800, Half: 600 x 800, Quarter: 600 x 400
    /// </summary>
    public enum LayerType
    {
        Full,    // 전체 팔레트 면적
        Half,    // 1/2 면적
        Quarter  // 1/4 면적
    }

    /// <summary>
    /// 동일 또는 유사한 높이의 아이템들로 구성된 Layer
    /// 논문 Section IV-B-2: "homogeneous product layers"
    /// </summary>
    public class Layer
    {
        public int LayerId { get; set; }
        public LayerType Type { get; set; }
        public List<Item> Items { get; set; }
        public double Height { get; set; }
        public double BaseArea { get; set; }
        public double OccupiedArea { get; set; }
        public double FillRate => BaseArea > 0 ? OccupiedArea / BaseArea : 0;

        // Layer가 배치될 위치 (Block 생성 시 사용)
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        // 동종 레이어 여부 (같은 ProductId)
        public bool IsHomogeneous => Items.TrueForAll(item =>
            item.ProductId == Items[0].ProductId);

        public Layer(int layerId, LayerType type, double baseArea, double height = 0)
        {
            LayerId = layerId;
            Type = type;
            BaseArea = baseArea;
            Height = height;
            Items = new List<Item>();
            OccupiedArea = 0;
            X = 0;
            Y = 0;
            Z = 0;
        }

        public void AddItem(Item item)
        {
            Items.Add(item);
            OccupiedArea += item.CurrentLength * item.CurrentWidth;

            // Height 업데이트
            if (Height == 0 || Math.Abs(item.CurrentHeight - Height) < 0.1)
            {
                Height = item.CurrentHeight;
            }
        }

        public void RemoveItem(Item item)
        {
            Items.Remove(item);
            OccupiedArea -= item.CurrentLength * item.CurrentWidth;
        }

        public void Clear()
        {
            Items.Clear();
            OccupiedArea = 0;
        }

        /// <summary>
        /// Layer를 특정 위치에 배치
        /// </summary>
        public void PlaceAt(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;

            // 레이어 내 아이템들의 Z 좌표 업데이트
            foreach (var item in Items)
            {
                item.Z = z;
            }
        }

        public Layer Clone()
        {
            var clone = new Layer(LayerId, Type, BaseArea, Height)
            {
                X = this.X,
                Y = this.Y,
                Z = this.Z,
                OccupiedArea = this.OccupiedArea
            };

            foreach (var item in Items)
            {
                clone.Items.Add(item.Clone());
            }

            return clone;
        }

        public override string ToString()
        {
            string productInfo = IsHomogeneous && Items.Count > 0
                ? $"Product: {Items[0].ProductId}"
                : $"{Items.Count} mixed items";

            return $"Layer {LayerId} ({Type}): {Items.Count} items, " +
                   $"Fill: {FillRate:P2}, Height: {Height:F0}mm, {productInfo}";
        }
    }
}
