# MHA Palletizing 사용 가이드

이 문서는 MHA Palletizing 알고리즘의 상세한 사용법을 제공합니다.

## 목차
1. [빠른 시작](#빠른-시작)
2. [CSV 데이터 처리](#csv-데이터-처리)
3. [병렬 처리](#병렬-처리)
4. [결과 분석](#결과-분석)
5. [고급 설정](#고급-설정)
6. [문제 해결](#문제-해결)

---

## 빠른 시작

### 1. 단일 주문 처리

```csharp
using MHAPalletizing;
using MHAPalletizing.Models;

// 주문 생성
var order = new Order("ORDER_001");

// 아이템 추가
order.AddItem("MILK_1L", 100, 80, 200, 1.0, 20);   // ProductId, L, W, H, Weight, Qty
order.AddItem("JUICE_500ML", 90, 70, 180, 0.8, 15);

// MHA 실행
var mha = new MHAAlgorithm(seed: 42);
var pallets = mha.Solve(order, maxPallets: 5);

// 결과 출력
Console.WriteLine($"사용된 팔레트: {pallets.Count}개");
Console.WriteLine($"배치된 아이템: {pallets.Sum(p => p.Items.Count)}/{order.TotalItemCount}");
Console.WriteLine($"평균 활용률: {pallets.Average(p => p.VolumeUtilization):P2}");
```

### 2. 검증 및 통계

```csharp
// 솔루션 검증
mha.ValidateSolution(pallets, order);

// 팔레트별 상세 정보
foreach (var pallet in pallets)
{
    Console.WriteLine($"\n{pallet}");
    Console.WriteLine($"  부피 활용률: {pallet.VolumeUtilization:P2}");
    Console.WriteLine($"  높이 활용률: {pallet.CurrentHeight / pallet.MaxHeight:P2}");
    Console.WriteLine($"  총 무게: {pallet.TotalWeight:F2}kg");
    Console.WriteLine($"  제품 종류: {pallet.GetProductTypeCount()}개");
}
```

---

## CSV 데이터 처리

### Dataset10 형식

```csv
Order,Product,Quantity,Length,Width,Height,Weight
29718,28862,20,300,160,216,8.953
29718,70696,18,387,227,151,10.382
16129,93215,3,290,240,170,1.36
```

### CSV 읽기

```csharp
using MHAPalletizing.Utils;

// 전체 주문 로드
var orders = CsvReader.ReadOrdersFromCsv(@"Dataset10.csv");
Console.WriteLine($"총 {orders.Count}개 주문 로드됨");

// 특정 주문만 로드
var order = CsvReader.ReadSingleOrder(@"Dataset10.csv", "16129");
Console.WriteLine($"{order}");

// 주문 ID 목록 가져오기
var orderIds = CsvReader.GetOrderIds(@"Dataset10.csv");
foreach (var orderId in orderIds)
{
    Console.WriteLine($"Order: {orderId}");
}
```

### 결과를 CSV로 저장

```csharp
using System.Diagnostics;

var order = CsvReader.ReadSingleOrder("Dataset10.csv", "16129");

// 시간 측정
var stopwatch = Stopwatch.StartNew();
var mha = new MHAAlgorithm(seed: 42);
var pallets = mha.Solve(order, maxPallets: 10);
stopwatch.Stop();

// 3가지 CSV 파일 생성
ResultWriter.AppendOrderResult(
    "Results/summary_results.csv",
    order,
    pallets,
    stopwatch.Elapsed.TotalMilliseconds
);

ResultWriter.WriteDetailedResults(
    $"Results/detailed_{order.OrderId}.csv",
    order,
    pallets
);

ResultWriter.WriteItemPlacements(
    $"Results/placements_{order.OrderId}.csv",
    order,
    pallets
);
```

### 결과 CSV 구조

**1. summary_results.csv**
```csv
OrderId,Algorithm,ItemCount,ProductTypes,Entropy,Complexity,PalletsUsed,ItemsPlaced,ItemsUnplaced,AvgVolumeUtilization,AvgHeightUtilization,TotalWeight,AvgHeterogeneity,AvgCompactness,ExecutionTimeMs
16129,MHA,27,4,0.8289,Entropy Interval 5 (0.8, 1.0],1,27,0,0.4709,0.6021,340.23,1.0000,0.6104,304.81
```

**2. detailed_results_16129.csv**
```csv
OrderId,PalletId,ItemCount,ProductTypes,VolumeUtilization,HeightUtilization,Weight,Heterogeneity,Compactness,Products
16129,1,27,4,0.4709,0.6021,340.23,1.0000,0.6104,"20647(3);74349(15);29322(6);56746(3)"
```

**3. item_placements_16129.csv**
```csv
OrderId,PalletId,ItemId,ProductId,X,Y,Z,Length,Width,Height,Weight,IsRotated
16129,1,1633,20647,0.00,0.00,0.00,331.00,252.00,281.00,12.64,False
16129,1,1634,20647,0.00,252.00,0.00,331.00,252.00,281.00,12.64,False
```

---

## 병렬 처리

### 기본 병렬 처리

```csharp
using MHAPalletizing.Tests;

// 자동 스레드 수 (CPU 코어 수 기반)
DatasetTests.RunDatasetTestsParallel(maxThreads: 0);

// 수동 스레드 수 지정
DatasetTests.RunDatasetTestsParallel(maxThreads: 4);
```

### ParallelProcessor 직접 사용

```csharp
using MHAPalletizing.Utils;

var orders = CsvReader.ReadOrdersFromCsv("Dataset10.csv");

// 병렬 처리기 생성 (4 스레드)
var processor = new ParallelProcessor(maxThreads: 4);

// 병렬 실행
processor.ProcessOrdersParallel(
    orders,
    resultsPath: "Results/",
    maxPalletsPerOrder: 10,
    seed: 42
);

// 결과:
// ✓ Parallel Processing Complete!
//   Total Orders: 10
//   Successful: 10
//   Failed: 0
//   Total Time: 0.34s
//   Avg Time per Order: 0.03s
//   Speedup: ~4x (with 4 threads)
```

### 배치 처리 (메모리 절약)

```csharp
// 대량 주문을 배치로 나누어 처리
processor.ProcessOrdersInBatches(
    orders,
    resultsPath: "Results/",
    batchSize: 10,        // 배치당 10개 주문
    maxThreads: 4,
    seed: 42
);

// 각 배치 완료 후 GC 실행으로 메모리 절약
```

### 성능 비교

| 모드 | 주문 수 | 실행 시간 | 스레드 수 | 배속 |
|------|---------|-----------|-----------|------|
| 순차 처리 | 10 | ~1.2초 | 1 | 1x |
| 병렬 처리 | 10 | ~0.34초 | 4 | ~4x |

---

## 결과 분석

### 핵심 지표

```csharp
// 주문 정보
Console.WriteLine($"주문 ID: {order.OrderId}");
Console.WriteLine($"총 아이템: {order.TotalItemCount}");
Console.WriteLine($"제품 종류: {order.ProductTypeCount}");
Console.WriteLine($"Entropy: {order.Entropy:F4} ({order.GetComplexityClass()})");

// 팔레트 통계
Console.WriteLine($"사용 팔레트: {pallets.Count}개");
Console.WriteLine($"배치 성공률: {pallets.Sum(p => p.Items.Count) * 100.0 / order.TotalItemCount:F2}%");
Console.WriteLine($"평균 부피 활용률: {pallets.Average(p => p.VolumeUtilization):P2}");
Console.WriteLine($"평균 높이 활용률: {pallets.Average(p => p.CurrentHeight / p.MaxHeight):P2}");
Console.WriteLine($"총 무게: {pallets.Sum(p => p.TotalWeight):F2}kg");
```

### 제약조건 검증

```csharp
// 전체 솔루션 검증
mha.ValidateSolution(pallets, order);

// 개별 제약조건 확인
foreach (var pallet in pallets)
{
    Console.WriteLine($"\nPallet {pallet.PalletId} 검증:");

    // Stability
    bool isStable = pallet.IsStable(tolerance: 0.3);
    Console.WriteLine($"  안정성: {(isStable ? "✓" : "✗")}");

    // 무게중심
    var (comX, comY, comZ) = pallet.GetCenterOfMass();
    Console.WriteLine($"  무게중심: ({comX:F1}, {comY:F1}, {comZ:F1})");
}
```

---

## 고급 설정

### GA 파라미터 튜닝

`Phase2/GeneticAlgorithm.cs` 파일에서 파라미터 조정:

```csharp
private const int MAX_GENERATIONS = 30;     // 세대 수 (↑ = 품질↑, 시간↑)
private const int MAX_STAGNATION = 8;       // 정체 허용 (↑ = 탐색↑)
private const int POPULATION_SIZE = 100;    // 개체군 크기
private const double CROSSOVER_PROB = 0.7;  // 교배 확률
private const double MUTATION_PROB = 0.3;   // 돌연변이 확률
```

**권장 설정:**
- 빠른 처리: `MAX_GENERATIONS = 20`, `MAX_STAGNATION = 5`
- 균형: `MAX_GENERATIONS = 30`, `MAX_STAGNATION = 8` (기본값)
- 고품질: `MAX_GENERATIONS = 50`, `MAX_STAGNATION = 10`

### Stability Tolerance 조정

`Phase2/PlacementStrategy.cs`에서 조정:

```csharp
// 현재 설정 (아이템 수 기반 동적 조정)
double stabilityTolerance = 0.99;       // 1-2개
if (pallet.Items.Count >= 10)
    stabilityTolerance = 0.4;           // 10개 이상
else if (pallet.Items.Count >= 5)
    stabilityTolerance = 0.5;           // 5-9개
else if (pallet.Items.Count >= 3)
    stabilityTolerance = 0.7;           // 3-4개
```

**Tolerance 의미:**
- `0.3`: 매우 엄격 (무게중심이 중앙 30% 이내)
- `0.5`: 중간
- `0.7`: 완화
- `0.99`: 거의 제약 없음

### 병렬 처리 스레드 수

```csharp
// CPU 코어 수 확인
int cores = Environment.ProcessorCount;
Console.WriteLine($"사용 가능한 코어: {cores}개");

// 권장 스레드 수
int recommendedThreads = Math.Max(2, Math.Min(cores, 8));

// ParallelProcessor 생성
var processor = new ParallelProcessor(maxThreads: recommendedThreads);
```

**성능 팁:**
- I/O 작업 많음: 코어 수의 1.5배
- CPU 작업 많음: 코어 수와 동일
- 메모리 제한: 2-4 스레드 권장

---

## 문제 해결

### Q: "Phase 2 Failed: Could not place all residuals"

**원인:** 팔레트 용량 부족 또는 제약조건 위반

**해결:**
1. `maxPallets` 증가
2. Stability tolerance 완화
3. 아이템 크기 확인 (팔레트 범위 초과 여부)

```csharp
// maxPallets 증가
var pallets = mha.Solve(order, maxPallets: 20);  // 기본값 10 → 20

// 또는 동적 계산
int maxPallets = Math.Max(10, order.TotalItemCount / 50);
```

### Q: "OutOfMemoryException" 발생

**원인:** Phase 1 LayerBuilder의 과도한 메모리 사용

**현재 상태:** Phase 1은 비활성화됨 (모든 아이템이 Phase 2로 처리)

**해결:**
- 배치 처리 모드 사용
- 스레드 수 감소
- 주문을 작은 단위로 분할

```csharp
// 배치 처리 사용
DatasetTests.RunDatasetTestsInBatches(batchSize: 5, maxThreads: 2);
```

### Q: 배치 성공률이 낮음 (50% 이하)

**원인:**
1. 아이템 크기가 팔레트 대비 너무 큼
2. 제약조건이 너무 엄격
3. GA 파라미터 부족

**해결:**
```csharp
// 1. 팔레트 수 증가
int maxPallets = order.TotalItemCount / 20;  // 더 여유있게

// 2. GA 세대 수 증가
// GeneticAlgorithm.cs에서 MAX_GENERATIONS = 50

// 3. 디버그 테스트 실행
DebugTests.TestSingleItemPlacement();  // 단일 아이템 배치 테스트
```

### Q: CSV 파일이 비어있음

**원인:** 아이템 배치 실패 (0개 팔레트)

**확인:**
```csharp
// 콘솔 출력 확인
Total pallets used: 0     // ← 문제!
Total items placed: 0/27

// 해결: 위의 "Phase 2 Failed" 해결법 참조
```

### Q: 실행 시간이 너무 김

**최적화:**
```csharp
// 1. GA 파라미터 축소
MAX_GENERATIONS = 20;
MAX_STAGNATION = 5;

// 2. 병렬 처리 활성화
DatasetTests.RunDatasetTestsParallel(maxThreads: 4);

// 3. 특정 주문만 테스트
DatasetTests.RunSingleOrderTest("16129");
```

---

## 예제 시나리오

### 시나리오 1: 소규모 주문 (10-50 items)

```csharp
var order = CsvReader.ReadSingleOrder("Dataset10.csv", "16129");
var mha = new MHAAlgorithm(seed: 42);
var pallets = mha.Solve(order, maxPallets: 5);

// 예상 결과:
// - 실행 시간: 0.3-1초
// - 팔레트 수: 1-2개
// - 활용률: 40-60%
```

### 시나리오 2: 중규모 주문 (50-500 items)

```csharp
var order = CsvReader.ReadSingleOrder("Dataset10.csv", "11059");
var mha = new MHAAlgorithm(seed: 42);
var pallets = mha.Solve(order, maxPallets: 15);

// 예상 결과:
// - 실행 시간: 5-30초
// - 팔레트 수: 5-10개
// - 활용률: 50-70%
```

### 시나리오 3: 대규모 데이터셋 (100+ orders)

```csharp
var orders = CsvReader.ReadOrdersFromCsv("Dataset10.csv");
var processor = new ParallelProcessor(maxThreads: 4);

processor.ProcessOrdersParallel(
    orders,
    resultsPath: "Results/",
    maxPalletsPerOrder: 15,
    seed: 42
);

// 예상 결과:
// - 총 시간: 1-5분
// - 처리 속도: ~4x (4 스레드)
// - 메모리 사용: 1-2GB
```

---

## 추가 자료

- [README.md](README.md): 프로젝트 개요
- [CHANGELOG.md](CHANGELOG.md): 변경 이력
- [CLAUDE.md](CLAUDE.md): 개발자 가이드
- [API.md](API.md): API 레퍼런스

**논문:** "A Multi-Heuristic Algorithm for Multi-Container 3D Bin Packing Problem Optimization Using Real World Constraints" (IEEE Access 2024)
