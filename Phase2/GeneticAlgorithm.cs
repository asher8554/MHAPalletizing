using System;
using System.Collections.Generic;
using System.Linq;
using MHAPalletizing.Models;
using MHAPalletizing.Constraints;

namespace MHAPalletizing.Phase2
{
    /// <summary>
    /// Genetic Algorithm for residual packing
    /// 논문 Section IV-B-3 & IV-C: "Mu plus Lambda with NSGA-II"
    /// </summary>
    /// <remarks>
    /// Phase 1에서 배치되지 않은 Residual 아이템을 NSGA-II 기반 유전 알고리즘으로 최적 배치합니다.
    ///
    /// 주요 특징:
    /// - Mu + Lambda 전략 (μ=15, λ=30)
    /// - NSGA-II를 통한 다목적 최적화
    /// - Fitness 1: Heterogeneity (최소화) - 같은 제품끼리 그룹화
    /// - Fitness 2: Compactness (최대화) - 아이템 간 접촉 면적 최대화
    /// - Extreme Points 기반 배치 전략
    ///
    /// 개체 인코딩:
    /// - 유전자(Gene): 제품 타입(ProductId)의 배치 순서
    /// - 10개의 Custom Individuals + 90개의 Random Individuals
    /// </remarks>
    public class GeneticAlgorithm
    {
        // GA 파라미터 (논문 Section IV-C)
        private const int MU = 15;                  // 부모 선택 개체 수
        private const int LAMBDA = 30;              // 자손 개체 수
        private const int POPULATION_SIZE = 100;    // 초기 개체군 크기
        private const double CROSSOVER_PROB = 0.7;  // 교배 확률
        private const double MUTATION_PROB = 0.3;   // 돌연변이 확률
        private const int MAX_GENERATIONS = 30;     // 최대 세대 수 (품질 우선)
        private const int MAX_STAGNATION = 8;       // 정체 허용 세대 수

        private Random random;
        private List<Pallet> availablePallets;
        private Dictionary<string, List<Item>> residualsByType;

        /// <summary>
        /// GeneticAlgorithm의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="random">난수 생성기. null인 경우 새 인스턴스를 생성합니다.</param>
        public GeneticAlgorithm(Random random = null)
        {
            this.random = random ?? new Random();
        }

        /// <summary>
        /// Phase 1에서 배치되지 않은 Residual 아이템들을 유전 알고리즘으로 배치합니다.
        /// </summary>
        /// <param name="residuals">배치할 Residual 아이템 목록</param>
        /// <param name="pallets">사용 가능한 빈 팔레트 목록</param>
        /// <param name="usedPallets">아이템이 배치된 팔레트 목록 (출력 파라미터)</param>
        /// <returns>모든 Residual이 성공적으로 배치되면 true, 그렇지 않으면 false</returns>
        /// <remarks>
        /// GA 실행 과정:
        /// 1. 초기 개체군 생성 (100개)
        /// 2. 개체 평가 (Extreme Points로 배치 시도)
        /// 3. NSGA-II 선택 (Mu=15개)
        /// 4. 교배/돌연변이로 자손 생성 (Lambda=30개)
        /// 5. Mu + Lambda 전략으로 다음 세대 구성
        /// 6. 최대 30세대 또는 5세대 정체 시 종료
        ///
        /// Fitness 함수:
        /// - Heterogeneity: 팔레트당 제품 타입 다양성 (최소화)
        /// - Compactness: 아이템 간 접촉 면적 비율 (최대화)
        /// </remarks>
        /// <example>
        /// <code>
        /// var residuals = new List&lt;Item&gt; { /* Phase 1에서 남은 아이템들 */ };
        /// var pallets = new List&lt;Pallet&gt; { new Pallet(1), new Pallet(2) };
        ///
        /// var ga = new GeneticAlgorithm(new Random(42));
        /// bool success = ga.PackResiduals(residuals, pallets, out var usedPallets);
        ///
        /// if (success)
        ///     Console.WriteLine($"성공! {usedPallets.Count}개 팔레트 사용");
        /// else
        ///     Console.WriteLine("배치 실패");
        /// </code>
        /// </example>
        public bool PackResiduals(List<Item> residuals, List<Pallet> pallets, out List<Pallet> usedPallets)
        {
            availablePallets = new List<Pallet>(pallets);
            usedPallets = new List<Pallet>();

            // Residual을 ProductId별로 그룹화
            residualsByType = residuals.GroupBy(item => item.ProductId)
                                      .ToDictionary(g => g.Key, g => g.ToList());

            // 초기 개체군 생성
            var population = InitializePopulation();

            // 진화 - 진행률 표시 (한 줄로 업데이트)
            Individual bestSolution = null;
            int stagnationCount = 0;
            double previousBestFitness = double.MaxValue;
            int lastGeneration = 0;

            for (int generation = 0; generation < MAX_GENERATIONS; generation++)
            {
                lastGeneration = generation;

                // 개체 평가
                EvaluatePopulation(population);

                // 유효한 개체가 있는지 확인
                var validIndividuals = population.Where(ind => ind.IsValid).ToList();

                if (validIndividuals.Any())
                {
                    // OPTION 3: 가장 좋은 개체 선택 (Volume Utilization 추가, 우선순위: VolumeUtil > Compactness > Heterogeneity)
                    var currentBest = validIndividuals.OrderByDescending(ind => ind.VolumeUtilizationScore)
                                                     .ThenByDescending(ind => ind.CompactnessScore)
                                                     .ThenBy(ind => ind.HeterogeneityScore)
                                                     .First();

                    double currentFitness = -currentBest.VolumeUtilizationScore - currentBest.CompactnessScore + currentBest.HeterogeneityScore;

                    // 진행률 출력 (한 줄로 덮어쓰기) - ParallelProcessor에서 출력하므로 주석 처리
                    int placedCount = currentBest.PlacementMetadata.Sum(p => p.Value.Positions.Count);

                    // Console.Write($"\r  GA Gen {generation + 1}/{MAX_GENERATIONS} | Items: {placedCount}/{residuals.Count} | " +
                    //              $"Vol: {currentBest.VolumeUtilizationScore:F3} | Compact: {currentBest.CompactnessScore:F3} | Hetero: {currentBest.HeterogeneityScore:F2} | Stag: {stagnationCount}/{MAX_STAGNATION}   ");

                    if (Math.Abs(currentFitness - previousBestFitness) < 0.0001)
                    {
                        stagnationCount++;
                    }
                    else
                    {
                        stagnationCount = 0;
                        previousBestFitness = currentFitness;
                        bestSolution = currentBest;
                    }

                    // Early stopping
                    if (stagnationCount >= MAX_STAGNATION)
                    {
                        Console.WriteLine($"\r  ✓ GA converged at Gen {generation + 1} | Items: {placedCount}/{residuals.Count}                                           ");
                        break;
                    }
                }

                // 선택
                var selected = SelectNSGAII(population, MU);

                // 교배 및 돌연변이
                var offspring = GenerateOffspring(selected, LAMBDA);

                // Mu + Lambda
                population = selected.Concat(offspring).ToList();
            }

            // 최적 해 적용
            if (bestSolution != null && bestSolution.IsValid)
            {
                if (stagnationCount < MAX_STAGNATION)
                {
                    int finalPlaced = bestSolution.PlacementMetadata.Sum(p => p.Value.Positions.Count);
                    Console.WriteLine($"\r  ✓ GA completed {lastGeneration + 1} generations | Items: {finalPlaced}/{residuals.Count}                                    ");
                }
                ApplySolution(bestSolution, residuals, out usedPallets);
                return true;
            }

            Console.WriteLine($"\r  ✗ GA failed to find valid solution                                                                    ");
            return false; // 배치 실패
        }

        #region Population Initialization
        private List<Individual> InitializePopulation()
        {
            var population = new List<Individual>();

            var productIds = residualsByType.Keys.ToList();

            // Custom individuals (논문 Table 5)
            population.AddRange(CreateCustomIndividuals(productIds));

            // 랜덤 개체로 나머지 채우기
            while (population.Count < POPULATION_SIZE)
            {
                var shuffled = productIds.OrderBy(x => random.Next()).ToList();
                population.Add(new Individual(shuffled));
            }

            return population;
        }

        /// <summary>
        /// 논문 Table 5: 10개의 custom individuals
        /// </summary>
        private List<Individual> CreateCustomIndividuals(List<string> productIds)
        {
            var customs = new List<Individual>();

            // 0: decreasing weight
            customs.Add(new Individual(productIds.OrderByDescending(id =>
                residualsByType[id].Average(item => item.Weight)).ToList()));

            // 1: increasing weight
            customs.Add(new Individual(productIds.OrderBy(id =>
                residualsByType[id].Average(item => item.Weight)).ToList()));

            // 2: decreasing quantity
            customs.Add(new Individual(productIds.OrderByDescending(id =>
                residualsByType[id].Count).ToList()));

            // 3: increasing quantity
            customs.Add(new Individual(productIds.OrderBy(id =>
                residualsByType[id].Count).ToList()));

            // 4: decreasing base surface area
            customs.Add(new Individual(productIds.OrderByDescending(id =>
                residualsByType[id].Average(item => item.Length * item.Width)).ToList()));

            // 5: increasing base surface area
            customs.Add(new Individual(productIds.OrderBy(id =>
                residualsByType[id].Average(item => item.Length * item.Width)).ToList()));

            // 6: decreasing volume
            customs.Add(new Individual(productIds.OrderByDescending(id =>
                residualsByType[id].Average(item => item.Volume)).ToList()));

            // 7: increasing volume
            customs.Add(new Individual(productIds.OrderBy(id =>
                residualsByType[id].Average(item => item.Volume)).ToList()));

            // 8: decreasing volume × quantity
            customs.Add(new Individual(productIds.OrderByDescending(id =>
                residualsByType[id].Sum(item => item.Volume)).ToList()));

            // 9: increasing volume × quantity
            customs.Add(new Individual(productIds.OrderBy(id =>
                residualsByType[id].Sum(item => item.Volume)).ToList()));

            return customs;
        }
        #endregion

        #region Evaluation
        private void EvaluatePopulation(List<Individual> population)
        {
            foreach (var individual in population)
            {
                EvaluateIndividual(individual);
            }
        }

        private void EvaluateIndividual(Individual individual)
        {
            // Placement Strategy로 배치 시도
            var testPallets = availablePallets.Select(p => new Pallet(p.PalletId, p.Length, p.Width, p.MaxHeight)).ToList();

            // 현재 팔레트 인덱스
            int currentPalletIndex = 0;
            var strategy = new PlacementStrategy(testPallets[currentPalletIndex]);

            bool allPlaced = true;

            // Gene 순서대로 아이템 배치
            foreach (var productId in individual.Genes)
            {
                if (!residualsByType.ContainsKey(productId))
                    continue;

                var items = residualsByType[productId];

                foreach (var originalItem in items)
                {
                    var item = originalItem.Clone();
                    bool placed = false;

                    // 현재 팔레트에 배치 시도
                    while (currentPalletIndex < testPallets.Count && !placed)
                    {
                        if (strategy.TryPlaceItem(item, allowRotation: true))
                        {
                            testPallets[currentPalletIndex].AddItem(item);
                            placed = true;

                            // 메타데이터 저장
                            if (!individual.PlacementMetadata.ContainsKey(productId))
                            {
                                individual.PlacementMetadata[productId] = new ItemPlacementInfo
                                {
                                    ProductId = productId,
                                    Length = item.Length,
                                    Width = item.Width,
                                    Height = item.Height
                                };
                            }

                            individual.PlacementMetadata[productId].Positions.Add(new ItemPosition
                            {
                                X = item.X,
                                Y = item.Y,
                                Z = item.Z,
                                IsRotated = item.IsRotated
                            });
                        }
                        else
                        {
                            // 다음 팔레트로
                            currentPalletIndex++;
                            if (currentPalletIndex < testPallets.Count)
                            {
                                strategy = new PlacementStrategy(testPallets[currentPalletIndex]);
                            }
                        }
                    }

                    if (!placed)
                    {
                        allPlaced = false;
                        break;
                    }
                }

                if (!allPlaced)
                    break;
            }

            individual.IsValid = allPlaced;

            if (allPlaced)
            {
                // Fitness 계산
                var usedPallets = testPallets.Take(currentPalletIndex + 1).ToList();

                // Fitness 1: Heterogeneity (최소화)
                individual.HeterogeneityScore = ConstraintValidator.CalculateAverageHeterogeneity(usedPallets);

                // Fitness 2: Compactness (최대화)
                individual.CompactnessScore = usedPallets.Average(p => p.GetAverageCompactness());

                // OPTION 3: Fitness 3: Volume Utilization (최대화)
                individual.VolumeUtilizationScore = usedPallets.Average(p => p.VolumeUtilization);
            }
        }
        #endregion

        #region NSGA-II Selection
        private List<Individual> SelectNSGAII(List<Individual> population, int selectCount)
        {
            // Fast non-dominated sorting
            var fronts = FastNonDominatedSort(population);

            // Crowding distance 계산
            foreach (var front in fronts)
            {
                CalculateCrowdingDistance(front);
            }

            // 선택
            var selected = new List<Individual>();
            int frontIndex = 0;

            while (selected.Count < selectCount && frontIndex < fronts.Count)
            {
                var front = fronts[frontIndex];

                if (selected.Count + front.Count <= selectCount)
                {
                    selected.AddRange(front);
                }
                else
                {
                    // Crowding distance로 정렬하여 일부만 선택
                    var sorted = front.OrderByDescending(ind => ind.CrowdingDistance).ToList();
                    selected.AddRange(sorted.Take(selectCount - selected.Count));
                }

                frontIndex++;
            }

            return selected;
        }

        private List<List<Individual>> FastNonDominatedSort(List<Individual> population)
        {
            var fronts = new List<List<Individual>>();
            var dominatedCount = new Dictionary<Individual, int>();
            var dominates = new Dictionary<Individual, List<Individual>>();

            foreach (var p in population)
            {
                dominatedCount[p] = 0;
                dominates[p] = new List<Individual>();
            }

            // Pareto front 구축
            foreach (var p in population)
            {
                foreach (var q in population)
                {
                    if (p == q) continue;

                    if (Individual.Dominates(p, q))
                    {
                        dominates[p].Add(q);
                    }
                    else if (Individual.Dominates(q, p))
                    {
                        dominatedCount[p]++;
                    }
                }

                if (dominatedCount[p] == 0)
                {
                    p.Rank = 0;
                    if (fronts.Count == 0)
                        fronts.Add(new List<Individual>());
                    fronts[0].Add(p);
                }
            }

            // 나머지 front 구축
            int currentRank = 0;
            while (currentRank < fronts.Count)
            {
                var nextFront = new List<Individual>();

                foreach (var p in fronts[currentRank])
                {
                    foreach (var q in dominates[p])
                    {
                        dominatedCount[q]--;
                        if (dominatedCount[q] == 0)
                        {
                            q.Rank = currentRank + 1;
                            nextFront.Add(q);
                        }
                    }
                }

                if (nextFront.Any())
                {
                    fronts.Add(nextFront);
                }

                currentRank++;
            }

            return fronts;
        }

        private void CalculateCrowdingDistance(List<Individual> front)
        {
            int size = front.Count;

            foreach (var ind in front)
            {
                ind.CrowdingDistance = 0;
            }

            if (size <= 2)
            {
                foreach (var ind in front)
                {
                    ind.CrowdingDistance = double.MaxValue;
                }
                return;
            }

            // OPTION 3: 3-목적 최적화 Crowding Distance

            // Objective 1: Heterogeneity
            var sortedByObj1 = front.OrderBy(ind => ind.HeterogeneityScore).ToList();
            sortedByObj1[0].CrowdingDistance = double.MaxValue;
            sortedByObj1[size - 1].CrowdingDistance = double.MaxValue;

            double range1 = sortedByObj1[size - 1].HeterogeneityScore - sortedByObj1[0].HeterogeneityScore;
            if (range1 > 0)
            {
                for (int i = 1; i < size - 1; i++)
                {
                    sortedByObj1[i].CrowdingDistance +=
                        (sortedByObj1[i + 1].HeterogeneityScore - sortedByObj1[i - 1].HeterogeneityScore) / range1;
                }
            }

            // Objective 2: Compactness
            var sortedByObj2 = front.OrderBy(ind => ind.CompactnessScore).ToList();
            sortedByObj2[0].CrowdingDistance = double.MaxValue;
            sortedByObj2[size - 1].CrowdingDistance = double.MaxValue;

            double range2 = sortedByObj2[size - 1].CompactnessScore - sortedByObj2[0].CompactnessScore;
            if (range2 > 0)
            {
                for (int i = 1; i < size - 1; i++)
                {
                    sortedByObj2[i].CrowdingDistance +=
                        (sortedByObj2[i + 1].CompactnessScore - sortedByObj2[i - 1].CompactnessScore) / range2;
                }
            }

            // Objective 3: Volume Utilization (OPTION 3)
            var sortedByObj3 = front.OrderBy(ind => ind.VolumeUtilizationScore).ToList();
            sortedByObj3[0].CrowdingDistance = double.MaxValue;
            sortedByObj3[size - 1].CrowdingDistance = double.MaxValue;

            double range3 = sortedByObj3[size - 1].VolumeUtilizationScore - sortedByObj3[0].VolumeUtilizationScore;
            if (range3 > 0)
            {
                for (int i = 1; i < size - 1; i++)
                {
                    sortedByObj3[i].CrowdingDistance +=
                        (sortedByObj3[i + 1].VolumeUtilizationScore - sortedByObj3[i - 1].VolumeUtilizationScore) / range3;
                }
            }
        }
        #endregion

        #region Genetic Operators
        private List<Individual> GenerateOffspring(List<Individual> parents, int offspringCount)
        {
            var offspring = new List<Individual>();

            while (offspring.Count < offspringCount)
            {
                if (random.NextDouble() < CROSSOVER_PROB)
                {
                    // Crossover
                    var parent1 = parents[random.Next(parents.Count)];
                    var parent2 = parents[random.Next(parents.Count)];
                    var child = Crossover(parent1, parent2);
                    offspring.Add(child);
                }
                else
                {
                    // Mutation
                    var parent = parents[random.Next(parents.Count)];
                    var child = Mutate(parent.Clone());
                    offspring.Add(child);
                }
            }

            return offspring;
        }

        /// <summary>
        /// Single-point crossover (논문 Figure 12)
        /// </summary>
        private Individual Crossover(Individual parent1, Individual parent2)
        {
            int crossoverPoint = random.Next(1, parent1.Genes.Count);

            var childGenes = new List<string>();
            childGenes.AddRange(parent1.Genes.Take(crossoverPoint));

            // 중복 제거하면서 parent2 유전자 추가
            var remaining = parent2.Genes.Where(g => !childGenes.Contains(g)).ToList();
            childGenes.AddRange(remaining);

            // 누락된 유전자 추가 (혹시 모를 경우)
            var allGenes = parent1.Genes.Union(parent2.Genes).ToList();
            var missing = allGenes.Where(g => !childGenes.Contains(g)).ToList();
            childGenes.AddRange(missing);

            return new Individual(childGenes);
        }

        /// <summary>
        /// Swap mutation (논문 Figure 12)
        /// </summary>
        private Individual Mutate(Individual individual)
        {
            if (individual.Genes.Count < 2)
                return individual;

            int index1 = random.Next(individual.Genes.Count);
            int index2 = random.Next(individual.Genes.Count);

            // Swap
            var temp = individual.Genes[index1];
            individual.Genes[index1] = individual.Genes[index2];
            individual.Genes[index2] = temp;

            return individual;
        }
        #endregion

        #region Solution Application
        private void ApplySolution(Individual solution, List<Item> residuals, out List<Pallet> usedPallets)
        {
            usedPallets = new List<Pallet>();
            // 실제 배치는 메타데이터를 사용하여 복원
            // 간단화를 위해 재배치
            var testPallets = availablePallets.Select(p => new Pallet(p.PalletId, p.Length, p.Width, p.MaxHeight)).ToList();

            int palletIndex = 0;
            var strategy = new PlacementStrategy(testPallets[palletIndex]);

            foreach (var productId in solution.Genes)
            {
                if (!residualsByType.ContainsKey(productId))
                    continue;

                foreach (var item in residualsByType[productId])
                {
                    bool placed = false;

                    while (palletIndex < testPallets.Count && !placed)
                    {
                        if (strategy.TryPlaceItem(item, allowRotation: true))
                        {
                            testPallets[palletIndex].AddItem(item);
                            placed = true;
                        }
                        else
                        {
                            palletIndex++;
                            if (palletIndex < testPallets.Count)
                            {
                                strategy = new PlacementStrategy(testPallets[palletIndex]);
                            }
                        }
                    }
                }
            }

            usedPallets = testPallets.Take(palletIndex + 1).ToList();
        }
        #endregion
    }
}
