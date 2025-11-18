using System;
using System.Collections.Generic;
using System.Linq;
using MHAPalletizing.Models;
using MHAPalletizing.Phase1;
using MHAPalletizing.Phase2;

namespace MHAPalletizing
{
    /// <summary>
    /// Multi-Heuristic Algorithm (MHA)
    /// 논문 Section IV: 2-Phase 알고리즘 통합
    /// Phase 1: Layer/Block 기반 휴리스틱
    /// Phase 2: Residual을 위한 Genetic Algorithm
    /// </summary>
    /// <remarks>
    /// MHA는 3D 빈 패킹 문제를 2단계로 해결합니다:
    /// 1. Phase 1: 동일 제품으로 레이어를 생성하고 팔레트에 배치 (Constructive Heuristics)
    /// 2. Phase 2: 남은 아이템(Residuals)을 유전 알고리즘으로 최적 배치 (NSGA-II)
    /// </remarks>
    public class MHAAlgorithm
    {
        private Random random;

        /// <summary>
        /// MHAAlgorithm의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="seed">난수 생성기 시드. 재현 가능한 결과를 위해 사용합니다. null인 경우 랜덤 시드를 사용합니다.</param>
        /// <example>
        /// <code>
        /// // 재현 가능한 결과를 위한 시드 사용
        /// var mha = new MHAAlgorithm(seed: 42);
        ///
        /// // 랜덤 시드 사용
        /// var mhaRandom = new MHAAlgorithm();
        /// </code>
        /// </example>
        public MHAAlgorithm(int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// 주문을 처리하여 팔레트에 아이템을 최적으로 배치합니다.
        /// 논문 Algorithm 1: MHA 전체 프로세스
        /// </summary>
        /// <param name="order">처리할 주문 객체. 배치할 아이템 목록을 포함합니다.</param>
        /// <param name="maxPallets">사용 가능한 최대 팔레트 수. 기본값은 10입니다.</param>
        /// <returns>아이템이 배치된 팔레트 목록. 각 팔레트는 배치된 아이템과 통계를 포함합니다.</returns>
        /// <remarks>
        /// 이 메서드는 2단계 프로세스를 실행합니다:
        /// 1. Phase 1: LayerBuilder를 사용하여 동일 제품 레이어를 생성하고 팔레트에 배치
        /// 2. Phase 2: Phase 1에서 배치되지 않은 Residual 아이템을 Genetic Algorithm으로 배치
        ///
        /// 모든 아이템이 배치되면 성공으로 간주되며, 8가지 제약조건이 검증됩니다.
        /// </remarks>
        /// <example>
        /// <code>
        /// var order = new Order("ORDER_001");
        /// order.AddItem("MILK_1L", 100, 80, 200, 1.0, 20);
        /// order.AddItem("JUICE_500ML", 90, 70, 180, 0.8, 15);
        ///
        /// var mha = new MHAAlgorithm(seed: 42);
        /// var pallets = mha.Solve(order, maxPallets: 5);
        ///
        /// Console.WriteLine($"사용된 팔레트 수: {pallets.Count}");
        /// Console.WriteLine($"배치된 아이템 수: {pallets.Sum(p => p.Items.Count)}");
        /// </code>
        /// </example>
        public List<Pallet> Solve(Order order, int maxPallets = 10)
        {
            var result = new List<Pallet>();
            var remainingItems = new List<Item>(order.Items);

            // Phase 1: Constructive Heuristics (Layer + Block) - 현재 비활성화
            var phase1Pallets = Phase1_ConstructiveHeuristics(remainingItems, maxPallets, out var residuals);
            result.AddRange(phase1Pallets);

            // Phase 2: Genetic Algorithm for Residuals
            if (residuals.Any())
            {
                var remainingPalletCount = maxPallets - phase1Pallets.Count;
                if (remainingPalletCount > 0)
                {
                    var availablePallets = Enumerable.Range(phase1Pallets.Count + 1, remainingPalletCount)
                        .Select(id => new Pallet(id))
                        .ToList();

                    var ga = new GeneticAlgorithm(random);
                    bool success = ga.PackResiduals(residuals, availablePallets, out var phase2Pallets);

                    if (success)
                    {
                        result.AddRange(phase2Pallets);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Phase 1: 논문 Section IV-B-1,2 - Layer & Block 기반 배치
        /// </summary>
        /// <remarks>
        /// 현재 구현:
        /// - Phase 1 LayerBuilder는 메모리 최적화가 필요하여 비활성화됨
        /// - 모든 아이템이 Phase 2 GA로 전달됨
        ///
        /// TODO: LayerBuilder 최적화
        /// - Item.Clone() 호출 최소화
        /// - 메모리 효율적인 레이어 생성 알고리즘 구현
        /// </remarks>
        private List<Pallet> Phase1_ConstructiveHeuristics(List<Item> items, int maxPallets, out List<Item> residuals)
        {
            var pallets = new List<Pallet>();

            // Phase 1은 현재 비활성화: 모든 아이템을 Phase 2로 전달
            residuals = new List<Item>(items);
            return pallets;
        }

        /// <summary>
        /// 생성된 솔루션의 유효성을 검증하고 상세 통계를 출력합니다.
        /// </summary>
        /// <param name="pallets">검증할 팔레트 목록</param>
        /// <param name="order">원본 주문 객체</param>
        /// <remarks>
        /// 다음 정보를 검증하고 출력합니다:
        /// - 배치된 아이템 수 vs 주문 아이템 수
        /// - 팔레트별 공간 활용률, 높이, 무게
        /// - 팔레트별 제품 타입 분포
        /// - 누락된 아이템 경고
        /// </remarks>
        /// <example>
        /// <code>
        /// var pallets = mha.Solve(order, maxPallets: 5);
        /// mha.ValidateSolution(pallets, order);
        /// </code>
        /// </example>
        public void ValidateSolution(List<Pallet> pallets, Order order)
        {
            Console.WriteLine("\n=== Solution Validation ===");

            int totalItemsPlaced = pallets.Sum(p => p.Items.Count);
            Console.WriteLine($"Items placed: {totalItemsPlaced}/{order.Items.Count}");

            if (totalItemsPlaced < order.Items.Count)
            {
                Console.WriteLine($"⚠ Warning: {order.Items.Count - totalItemsPlaced} items not placed");
            }

            // 팔레트별 통계
            for (int palletIndex = 0; palletIndex < pallets.Count; palletIndex++)
            {
                var pallet = pallets[palletIndex];
                Console.WriteLine($"\nPallet {palletIndex + 1}:");
                Console.WriteLine($"  Items: {pallet.Items.Count}");
                Console.WriteLine($"  Volume Utilization: {pallet.VolumeUtilization:P2}");
                Console.WriteLine($"  Height: {pallet.CurrentHeight:F0}/{pallet.MaxHeight:F0}mm");
                Console.WriteLine($"  Weight: {pallet.TotalWeight:F2}kg");

                // 제품 타입별 분포
                var productCounts = pallet.Items.GroupBy(item => item.ProductId)
                    .ToDictionary(g => g.Key, g => g.Count());
                Console.WriteLine($"  Product types: {string.Join(", ", productCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            }

            Console.WriteLine();
        }
    }
}
