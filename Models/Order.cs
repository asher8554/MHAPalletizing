using System;
using System.Collections.Generic;
using System.Linq;

namespace MHAPalletizing.Models
{
    /// <summary>
    /// 고객 주문
    /// 논문 Section IV-A: Order consists of I set of items
    /// </summary>
    public class Order
    {
        public string OrderId { get; set; }
        public List<Item> Items { get; set; }
        public List<Pallet> Pallets { get; set; }

        // 주문 통계
        public int TotalItemCount => Items.Count;
        public int ProductTypeCount => Items.Select(i => i.ProductId).Distinct().Count();
        public double TotalVolume => Items.Sum(i => i.Volume);
        public double TotalWeight => Items.Sum(i => i.Weight);

        // Entropy 계산 (논문 Section III)
        public double Entropy
        {
            get
            {
                var productGroups = Items.GroupBy(i => i.ProductId)
                                        .Select(g => g.Count())
                                        .ToList();

                if (!productGroups.Any())
                    return 0;

                int total = productGroups.Sum();
                double entropy = 0;

                foreach (var count in productGroups)
                {
                    double p = (double)count / total;
                    if (p > 0)
                        entropy -= p * Math.Log(p, 2);
                }

                // Normalize to [0, 1]
                double maxEntropy = Math.Log(productGroups.Count, 2);
                return maxEntropy > 0 ? entropy / maxEntropy : 0;
            }
        }

        // Complexity 클래스 (논문 Section V-B)
        public string GetComplexityClass()
        {
            double e = Entropy;
            if (e <= 0.2) return "Interval1_0.0-0.2";
            if (e <= 0.4) return "Interval2_0.2-0.4";
            if (e <= 0.6) return "Interval3_0.4-0.6";
            if (e <= 0.8) return "Interval4_0.6-0.8";
            return "Interval5_0.8-1.0";
        }

        // 주문 크기 분류 (논문 Section V-B)
        public string GetSizeClass()
        {
            if (TotalItemCount < 600) return "Small";
            if (TotalItemCount < 1300) return "Medium";
            return "Large";
        }

        // 생성자
        public Order(string orderId)
        {
            OrderId = orderId;
            Items = new List<Item>();
            Pallets = new List<Pallet>();
        }

        // 아이템 추가
        public void AddItem(string productId, double length, double width, double height, double weight, int quantity = 1)
        {
            for (int i = 0; i < quantity; i++)
            {
                int itemId = Items.Count + 1;
                Items.Add(new Item(productId, itemId, length, width, height, weight));
            }
        }

        // 팔레트 추가
        public Pallet AddPallet()
        {
            int palletId = Pallets.Count + 1;
            var pallet = new Pallet(palletId);
            Pallets.Add(pallet);
            return pallet;
        }

        // 모든 팔레트 초기화
        public void ClearPallets()
        {
            Pallets.Clear();
        }

        // 전체 평균 Volume Utilization
        public double GetAverageVolumeUtilization()
        {
            if (!Pallets.Any())
                return 0;

            return Pallets.Average(p => p.VolumeUtilization);
        }

        // 전체 평균 Compactness
        public double GetAverageCompactness()
        {
            if (!Pallets.Any())
                return 0;

            return Pallets.Average(p => p.GetAverageCompactness());
        }

        // 모든 아이템이 배치되었는지 확인 (Constraint 6: Complete shipment)
        public bool IsCompletelyPacked()
        {
            int packedItems = Pallets.Sum(p => p.Items.Count);
            return packedItems == TotalItemCount;
        }

        public override string ToString()
        {
            return $"Order {OrderId}: {TotalItemCount} items, {ProductTypeCount} types, " +
                   $"Entropy: {Entropy:F3} ({GetComplexityClass()}), Size: {GetSizeClass()}, " +
                   $"Pallets: {Pallets.Count}";
        }
    }
}
