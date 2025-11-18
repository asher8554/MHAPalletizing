using System;
using System.Collections.Generic;
using System.Linq;
using MHAPalletizing.Models;

namespace MHAPalletizing.Phase2
{
    /// <summary>
    /// GA의 개체 (Individual)
    /// 논문 Section IV-B-3 & Figure 12: "individuals encode order of item types"
    /// </summary>
    public class Individual : IComparable<Individual>
    {
        // 유전자: 아이템 타입의 배치 순서
        public List<string> Genes { get; set; }

        // Fitness 값 (3-목적 최적화)
        public double HeterogeneityScore { get; set; }      // 낮을수록 좋음 (Minimize)
        public double CompactnessScore { get; set; }        // 높을수록 좋음 (Maximize)
        public double VolumeUtilizationScore { get; set; }  // 높을수록 좋음 (Maximize) - OPTION 3

        // 메타데이터: 실제 배치 결과 (GA 평가 시 저장)
        public Dictionary<string, ItemPlacementInfo> PlacementMetadata { get; set; }

        // 배치 성공 여부
        public bool IsValid { get; set; }

        // Pareto Rank (NSGA-II)
        public int Rank { get; set; }
        public double CrowdingDistance { get; set; }

        public Individual(List<string> genes)
        {
            Genes = new List<string>(genes);
            PlacementMetadata = new Dictionary<string, ItemPlacementInfo>();
            IsValid = false;
            HeterogeneityScore = double.MaxValue;
            CompactnessScore = 0;
            VolumeUtilizationScore = 0;  // OPTION 3
            Rank = int.MaxValue;
            CrowdingDistance = 0;
        }

        /// <summary>
        /// 개체 복사
        /// </summary>
        public Individual Clone()
        {
            var clone = new Individual(new List<string>(Genes))
            {
                HeterogeneityScore = this.HeterogeneityScore,
                CompactnessScore = this.CompactnessScore,
                VolumeUtilizationScore = this.VolumeUtilizationScore,  // OPTION 3
                IsValid = this.IsValid,
                Rank = this.Rank,
                CrowdingDistance = this.CrowdingDistance
            };

            foreach (var kvp in PlacementMetadata)
            {
                clone.PlacementMetadata[kvp.Key] = kvp.Value.Clone();
            }

            return clone;
        }

        /// <summary>
        /// Pareto Dominance 비교 (3-목적 최적화)
        /// individual1이 individual2를 dominate하면 true
        /// </summary>
        public static bool Dominates(Individual ind1, Individual ind2)
        {
            // OPTION 3: 3-목적 최적화
            // Fitness 1: Heterogeneity (최소화)
            // Fitness 2: Compactness (최대화)
            // Fitness 3: Volume Utilization (최대화)

            bool betterInOne = false;

            // Heterogeneity: 낮을수록 좋음
            if (ind1.HeterogeneityScore < ind2.HeterogeneityScore)
                betterInOne = true;
            else if (ind1.HeterogeneityScore > ind2.HeterogeneityScore)
                return false;

            // Compactness: 높을수록 좋음
            if (ind1.CompactnessScore > ind2.CompactnessScore)
                betterInOne = true;
            else if (ind1.CompactnessScore < ind2.CompactnessScore)
                return false;

            // Volume Utilization: 높을수록 좋음 (OPTION 3)
            if (ind1.VolumeUtilizationScore > ind2.VolumeUtilizationScore)
                betterInOne = true;
            else if (ind1.VolumeUtilizationScore < ind2.VolumeUtilizationScore)
                return false;

            return betterInOne;
        }

        /// <summary>
        /// Crowding Distance 기준 정렬용
        /// </summary>
        public int CompareTo(Individual other)
        {
            if (other == null) return 1;

            // Rank가 낮을수록 우선
            if (Rank != other.Rank)
                return Rank.CompareTo(other.Rank);

            // 같은 Rank면 Crowding Distance가 클수록 우선
            return -CrowdingDistance.CompareTo(other.CrowdingDistance);
        }

        public override string ToString()
        {
            return $"Individual: Genes={Genes.Count}, Valid={IsValid}, " +
                   $"Heterogeneity={HeterogeneityScore:F4}, Compactness={CompactnessScore:F4}, " +
                   $"VolumeUtil={VolumeUtilizationScore:F4}, " +  // OPTION 3
                   $"Rank={Rank}, CD={CrowdingDistance:F4}";
        }
    }

    /// <summary>
    /// 아이템 타입별 배치 정보 (메타데이터)
    /// </summary>
    public class ItemPlacementInfo
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public List<ItemPosition> Positions { get; set; }

        public ItemPlacementInfo()
        {
            Positions = new List<ItemPosition>();
        }

        public ItemPlacementInfo Clone()
        {
            var clone = new ItemPlacementInfo
            {
                ProductId = this.ProductId,
                Quantity = this.Quantity,
                Length = this.Length,
                Width = this.Width,
                Height = this.Height,
                Positions = new List<ItemPosition>()
            };

            foreach (var pos in Positions)
            {
                clone.Positions.Add(pos.Clone());
            }

            return clone;
        }
    }

    /// <summary>
    /// 개별 아이템의 배치 위치 및 방향
    /// </summary>
    public class ItemPosition
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public bool IsRotated { get; set; }

        public ItemPosition Clone()
        {
            return new ItemPosition
            {
                X = this.X,
                Y = this.Y,
                Z = this.Z,
                IsRotated = this.IsRotated
            };
        }
    }
}
