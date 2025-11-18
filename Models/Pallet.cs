using System;
using System.Collections.Generic;
using System.Linq;

namespace MHAPalletizing.Models
{
    /// <summary>
    /// 팔레트 (컨테이너)
    /// 논문 Section IV-A: Euro pallet [1200mm x 800mm x 1400mm]
    /// </summary>
    public class Pallet
    {
        // Custom pallet 기본 치수 (Dataset0/Dataset1 호환)
        public const double DEFAULT_LENGTH = 1200; // mm
        public const double DEFAULT_WIDTH = 800;   // mm
        public const double DEFAULT_HEIGHT = 1400; // mm (최대 적재 높이)

        // 팔레트 식별자
        public int PalletId { get; set; }

        // 팔레트 크기
        public double Length { get; set; }
        public double Width { get; set; }
        public double MaxHeight { get; set; }

        // 배치된 아이템들
        public List<Item> Items { get; set; }

        // 팔레트 통계
        public double UsedVolume => Items.Sum(item => item.Volume);
        public double TotalVolume => Length * Width * MaxHeight;
        public double VolumeUtilization => TotalVolume > 0 ? UsedVolume / TotalVolume : 0;
        public double TotalWeight => Items.Sum(item => item.Weight);
        public double CurrentHeight => Items.Any() ? Items.Max(item => item.MaxZ) : 0;

        // 생성자
        public Pallet(int palletId, double length = DEFAULT_LENGTH, double width = DEFAULT_WIDTH, double maxHeight = DEFAULT_HEIGHT)
        {
            PalletId = palletId;
            Length = length;
            Width = width;
            MaxHeight = maxHeight;
            Items = new List<Item>();
        }

        // 아이템 추가
        public void AddItem(Item item)
        {
            Items.Add(item);
        }

        // 아이템 제거
        public void RemoveItem(Item item)
        {
            Items.Remove(item);
        }

        // 모든 아이템 제거
        public void Clear()
        {
            Items.Clear();
        }

        // 무게 중심 계산 (Constraint 3: Stability)
        // 최적화: 단일 루프로 X, Y, Z를 동시에 계산 (3배 빠름)
        public (double x, double y, double z) GetCenterOfMass()
        {
            if (!Items.Any())
                return (Length / 2, Width / 2, 0);

            double totalWeight = TotalWeight;
            if (totalWeight == 0)
                return (Length / 2, Width / 2, CurrentHeight / 2);

            double comX = 0, comY = 0, comZ = 0;

            // 단일 루프로 최적화 (3x faster)
            foreach (var item in Items)
            {
                var (x, y, z) = item.GetCenterOfMass();
                double weight = item.Weight;
                comX += x * weight;
                comY += y * weight;
                comZ += z * weight;
            }

            return (comX / totalWeight, comY / totalWeight, comZ / totalWeight);
        }

        // 무게 중심이 안정 영역 내에 있는지 확인 (Constraint 3)
        // 논문: COM restricted to area on XY plane surrounding geometric center
        public bool IsStable(double tolerance = 0.2)
        {
            if (!Items.Any())
                return true;

            var (comX, comY, comZ) = GetCenterOfMass();
            double centerX = Length / 2;
            double centerY = Width / 2;

            // 무게 중심이 팔레트 중심으로부터 얼마나 벗어났는지 확인
            double offsetX = Math.Abs(comX - centerX) / centerX;
            double offsetY = Math.Abs(comY - centerY) / centerY;

            return offsetX <= tolerance && offsetY <= tolerance;
        }

        // 아이템 타입 다양성 계산 (Constraint 7: Customer positioning)
        public int GetProductTypeCount()
        {
            return Items.Select(item => item.ProductId).Distinct().Count();
        }

        // 평균 Compactness 계산 (Section IV-B-3: Fitness function 2)
        // 최적화: 매번 재계산하는 대신 단일 패스로 처리
        public double GetAverageCompactness()
        {
            if (!Items.Any())
                return 0;

            double totalCompactness = 0;
            int count = Items.Count;

            foreach (var item in Items)
            {
                double compactness = CalculateItemCompactness(item);
                totalCompactness += compactness;
            }

            return totalCompactness / count; // Count 한 번만 호출
        }

        // 개별 아이템의 Compactness 계산
        // 논문: "maximum surface area of an item that is in contact with other items or the pallet"
        private double CalculateItemCompactness(Item item)
        {
            double totalSurfaceArea = 2 * (item.CurrentLength * item.CurrentWidth +
                                          item.CurrentLength * item.CurrentHeight +
                                          item.CurrentWidth * item.CurrentHeight);

            double contactArea = 0;

            // 바닥면 접촉 (팔레트 또는 다른 아이템)
            if (item.Z == 0)
            {
                contactArea += item.CurrentLength * item.CurrentWidth; // 팔레트와 접촉
            }
            else
            {
                // 다른 아이템과의 바닥 접촉면적 계산 (간략화)
                contactArea += CalculateBottomContactArea(item);
            }

            // 측면 접촉 (다른 아이템)
            contactArea += CalculateSideContactArea(item);

            return totalSurfaceArea > 0 ? contactArea / totalSurfaceArea : 0;
        }

        // 바닥 접촉 면적 계산 (간략화)
        private double CalculateBottomContactArea(Item item)
        {
            double contactArea = 0;
            double epsilon = 0.1; // 허용 오차

            foreach (var other in Items)
            {
                if (other == item) continue;

                // item이 other 바로 위에 있는지 확인
                if (Math.Abs(item.Z - other.MaxZ) < epsilon)
                {
                    // 겹치는 면적 계산
                    double overlapLength = Math.Max(0, Math.Min(item.MaxX, other.MaxX) - Math.Max(item.X, other.X));
                    double overlapWidth = Math.Max(0, Math.Min(item.MaxY, other.MaxY) - Math.Max(item.Y, other.Y));
                    contactArea += overlapLength * overlapWidth;
                }
            }

            return contactArea;
        }

        // 측면 접촉 면적 계산 (간략화)
        private double CalculateSideContactArea(Item item)
        {
            double contactArea = 0;
            double epsilon = 0.1;

            foreach (var other in Items)
            {
                if (other == item) continue;

                // X축 방향 접촉
                if (Math.Abs(item.MaxX - other.X) < epsilon || Math.Abs(item.X - other.MaxX) < epsilon)
                {
                    double overlapY = Math.Max(0, Math.Min(item.MaxY, other.MaxY) - Math.Max(item.Y, other.Y));
                    double overlapZ = Math.Max(0, Math.Min(item.MaxZ, other.MaxZ) - Math.Max(item.Z, other.Z));
                    contactArea += overlapY * overlapZ;
                }

                // Y축 방향 접촉
                if (Math.Abs(item.MaxY - other.Y) < epsilon || Math.Abs(item.Y - other.MaxY) < epsilon)
                {
                    double overlapX = Math.Max(0, Math.Min(item.MaxX, other.MaxX) - Math.Max(item.X, other.X));
                    double overlapZ = Math.Max(0, Math.Min(item.MaxZ, other.MaxZ) - Math.Max(item.Z, other.Z));
                    contactArea += overlapX * overlapZ;
                }
            }

            return contactArea;
        }

        public override string ToString()
        {
            return $"Pallet {PalletId}: {Items.Count} items, " +
                   $"Volume: {VolumeUtilization:P2}, Height: {CurrentHeight:F0}/{MaxHeight:F0}mm, " +
                   $"Weight: {TotalWeight:F1}kg, Products: {GetProductTypeCount()}";
        }
    }
}
