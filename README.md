# MHA Palletizing - Multi-Heuristic Algorithm for 3D Bin Packing

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Paper](https://img.shields.io/badge/Paper-IEEE%20Access%202024-orange.svg)](https://ieeexplore.ieee.org/)

## 개요

MHA Palletizing은 **Multi-Heuristic Algorithm (MHA)**을 활용한 3D 빈 패킹(Bin Packing) 문제 솔루션입니다.
이 프로젝트는 2024년 IEEE Access에 게재된 논문을 기반으로 실제 물류 환경의 제약조건을 반영하여 팔레트에 박스를 최적으로 배치하는 알고리즘을 구현합니다.

### 논문 정보
- **제목**: A Multi-Heuristic Algorithm for Multi-Container 3D Bin Packing Problem Optimization Using Real World Constraints
- **저자**: Anan Ashrabi Ananno, Luis Ribeiro
- **출처**: IEEE Access, 2024
- **DOI**: [논문 링크]

### 주요 특징
- 유로 팔레트(1200mm × 800mm × 1400mm) 표준 지원
- 실제 물류 환경의 8가지 제약조건 적용
- 2단계 하이브리드 알고리즘 (휴리스틱 + 유전 알고리즘)
- 높은 공간 활용률 및 안정성 보장
- .NET Framework 4.8 C# 구현

---

## 목차
- [설치 및 설정](#설치-및-설정)
- [빠른 시작](#빠른-시작)
- [사용 예제](#사용-예제)
- [아키텍처 개요](#아키텍처-개요)
- [알고리즘 설명](#알고리즘-설명)
- [8가지 제약조건](#8가지-제약조건)
- [설정 옵션](#설정-옵션)
- [API 레퍼런스](#api-레퍼런스)
- [프로젝트 구조](#프로젝트-구조)
- [참고 자료](#참고-자료)

---

## 설치 및 설정

### 시스템 요구사항
- **OS**: Windows 7 이상
- **.NET Framework**: 4.8 이상
- **IDE**: Visual Studio 2019 이상 (권장)
- **메모리**: 최소 2GB RAM (대량 주문 처리 시 4GB 권장)

### 설치 방법

#### 1. 레포지토리 클론
```bash
git clone https://github.com/yourusername/MHAPalletizing.git
cd MHAPalletizing
```

#### 2. Visual Studio에서 솔루션 열기
```
MHAPalletizing.sln 파일을 Visual Studio에서 엽니다.
```

#### 3. NuGet 패키지 복원
Visual Studio에서 자동으로 NuGet 패키지를 복원합니다. 수동 복원이 필요한 경우:
```
도구 > NuGet 패키지 관리자 > 패키지 관리자 콘솔
PM> Update-Package -reinstall
```

#### 4. 빌드 및 실행
```
빌드 > 솔루션 빌드 (Ctrl+Shift+B)
디버그 > 디버깅 시작 (F5)
```

---

## 빠른 시작

### 기본 사용법

```csharp
using MHAPalletizing;
using MHAPalletizing.Models;

// 1. 주문 생성
var order = new Order("ORDER_001");

// 2. 아이템 추가 (ProductId, Length, Width, Height, Weight, Quantity)
order.AddItem("MILK_1L", 100, 80, 200, 1.0, 20);
order.AddItem("JUICE_500ML", 90, 70, 180, 0.8, 15);

// 3. MHA 알고리즘 실행
var mha = new MHAAlgorithm(seed: 42);  // seed로 재현 가능한 결과
var pallets = mha.Solve(order, maxPallets: 5);

// 4. 결과 검증
mha.ValidateSolution(pallets, order);

// 5. 결과 출력
foreach (var pallet in pallets)
{
    Console.WriteLine(pallet.ToString());
    Console.WriteLine($"  Volume Utilization: {pallet.VolumeUtilization:P2}");
    Console.WriteLine($"  Items: {pallet.Items.Count}");
}
```

### 콘솔 출력 예제
```
=== MHA Algorithm Start ===
Order ORDER_001: 35 items, 2 types, Entropy: 0.687 (Entropy Interval 4 (0.6, 0.8]), Size: Small

--- Phase 1: Layer & Block Building ---
Building layers...
Created 4 layers
Placing layers on pallets...
Phase 1 Complete:
  Pallets used: 1
  Items placed: 35/35
  Residuals: 0

=== MHA Algorithm Complete ===
Total pallets used: 1
Total items placed: 35/35
Average volume utilization: 68.50%
```

---

## Dataset 사용법

### CSV 데이터셋 형식

Dataset10.csv 파일은 다음 형식을 따릅니다:

```csv
Order,Product,Quantity,Length,Width,Height,Weight
29718,28862,20,300,160,216,8.953
29718,70696,18,387,227,151,10.382
70138,99769,20,300,160,216,8.569
...
```

### CSV에서 주문 읽기

```csharp
using MHAPalletizing.Utils;

// 전체 주문 로드
var orders = CsvReader.ReadOrdersFromCsv(@"3DBPP-master\Dataset10.csv");
Console.WriteLine($"Loaded {orders.Count} orders");

// 특정 주문만 로드
var order = CsvReader.ReadSingleOrder(@"3DBPP-master\Dataset10.csv", "29718");

// 주문 ID 목록 가져오기
var orderIds = CsvReader.GetOrderIds(@"3DBPP-master\Dataset10.csv");
```

### 결과를 CSV로 저장

```csharp
using MHAPalletizing.Utils;
using System.Diagnostics;

var order = CsvReader.ReadSingleOrder("Dataset10.csv", "29718");

// MHA 실행
var stopwatch = Stopwatch.StartNew();
var mha = new MHAAlgorithm(seed: 42);
var pallets = mha.Solve(order, maxPallets: 10);
stopwatch.Stop();

// 결과 요약 CSV에 추가
ResultWriter.AppendOrderResult(
    "Results/summary_results.csv",
    order,
    pallets,
    stopwatch.Elapsed.TotalMilliseconds
);

// 상세 결과 저장
ResultWriter.WriteDetailedResults(
    $"Results/detailed_{order.OrderId}.csv",
    order,
    pallets
);

// 아이템 배치 결과 저장
ResultWriter.WriteItemPlacements(
    $"Results/placements_{order.OrderId}.csv",
    order,
    pallets
);
```

### 결과 CSV 파일 형식

**summary_results.csv**:
```csv
OrderId,Algorithm,ItemCount,ProductTypes,Entropy,Complexity,PalletsUsed,ItemsPlaced,ItemsUnplaced,AvgVolumeUtilization,AvgHeightUtilization,TotalWeight,AvgHeterogeneity,AvgCompactness,ExecutionTimeMs
29718,MHA,106,6,0.8762,Entropy Interval 5 (0.8, 1.0],2,98,8,0.6543,0.7234,856.42,0.4321,0.7654,1234.56
```

**detailed_results_{OrderId}.csv**:
```csv
OrderId,PalletId,ItemCount,ProductTypes,VolumeUtilization,HeightUtilization,Weight,Heterogeneity,Compactness,Products
29718,1,50,3,0.6823,0.7456,428.21,0.5000,0.7890,"28862(20);70696(18);49668(1)"
29718,2,48,4,0.6263,0.6912,428.21,0.6667,0.7418,"53900(18);25895(10);57007(12)"
```

**item_placements_{OrderId}.csv**:
```csv
OrderId,PalletId,ItemId,ProductId,X,Y,Z,Length,Width,Height,Weight,IsRotated
29718,1,1,28862,0.00,0.00,0.00,300.00,160.00,216.00,8.95,False
29718,1,2,28862,300.00,0.00,0.00,300.00,160.00,216.00,8.95,False
...
```

### 병렬 처리 (권장) ⚡

**빠른 대량 주문 처리:**

```csharp
// 자동 스레드 수 설정 (CPU 코어 기반)
DatasetTests.RunDatasetTestsParallel(maxThreads: 0);

// 수동 스레드 수 지정
DatasetTests.RunDatasetTestsParallel(maxThreads: 4);

// 배치 처리 (메모리 절약)
DatasetTests.RunDatasetTestsInBatches(batchSize: 10, maxThreads: 4);
```

**성능 비교:**
- 순차 처리: ~1.2초 (10 orders)
- 병렬 처리 (4 threads): ~0.34초 (약 4배 빠름)

### Dataset 테스트 실행

Program.cs에서 원하는 테스트 옵션의 주석을 해제:

```csharp
// 권장: 병렬 처리
DatasetTests.RunDatasetTestsParallel(maxThreads: 4);

// 순차 처리
DatasetTests.RunDatasetTests();

// 특정 주문만 테스트
DatasetTests.RunSingleOrderTest("16129");

// 데이터셋 통계 출력
DatasetTests.PrintDatasetStatistics();
```

## 사용 예제

### 예제 1: 단일 제품 타입 주문

```csharp
// 동일한 크기의 우유 팩 50개
var order = new Order("HOMOGENEOUS_ORDER");
order.AddItem("MILK_1L", 100, 80, 200, 1.0, 50);

var mha = new MHAAlgorithm();
var pallets = mha.Solve(order, maxPallets: 3);
```

### 예제 2: 혼합 제품 타입 주문

```csharp
var order = new Order("MIXED_ORDER");

// 여러 제품 타입
order.AddItem("MILK_1L", 100, 80, 200, 1.0, 20);
order.AddItem("JUICE_500ML", 90, 70, 180, 0.8, 15);
order.AddItem("WATER_1.5L", 110, 90, 220, 1.5, 25);
order.AddItem("YOGURT_200G", 60, 60, 80, 0.2, 30);

var mha = new MHAAlgorithm(seed: 123);
var pallets = mha.Solve(order, maxPallets: 5);
```

### 예제 3: 수동으로 아이템 생성

```csharp
var order = new Order("MANUAL_ORDER");

// 개별 아이템 추가
for (int i = 1; i <= 10; i++)
{
    var item = new Item("PRODUCT_A", i, 150, 100, 120, 2.5);
    order.Items.Add(item);
}

var mha = new MHAAlgorithm();
var pallets = mha.Solve(order, maxPallets: 2);
```

### 예제 4: 결과 분석

```csharp
var order = new Order("ANALYSIS_ORDER");
order.AddItem("BOX_A", 200, 150, 100, 3.0, 30);

var mha = new MHAAlgorithm();
var pallets = mha.Solve(order, maxPallets: 5);

// 상세 분석
foreach (var pallet in pallets)
{
    Console.WriteLine($"\nPallet {pallet.PalletId}:");
    Console.WriteLine($"  Volume Utilization: {pallet.VolumeUtilization:P2}");
    Console.WriteLine($"  Current Height: {pallet.CurrentHeight:F0}mm / {pallet.MaxHeight:F0}mm");
    Console.WriteLine($"  Total Weight: {pallet.TotalWeight:F2}kg");
    Console.WriteLine($"  Product Types: {pallet.GetProductTypeCount()}");

    var (comX, comY, comZ) = pallet.GetCenterOfMass();
    Console.WriteLine($"  Center of Mass: ({comX:F0}, {comY:F0}, {comZ:F0})");
    Console.WriteLine($"  Is Stable: {pallet.IsStable()}");
}

// 주문 통계
Console.WriteLine($"\nOrder Statistics:");
Console.WriteLine($"  Entropy: {order.Entropy:F3}");
Console.WriteLine($"  Complexity Class: {order.GetComplexityClass()}");
Console.WriteLine($"  Size Class: {order.GetSizeClass()}");
Console.WriteLine($"  Average Volume Utilization: {order.GetAverageVolumeUtilization():P2}");
```

---

## 아키텍처 개요

### 시스템 구조

```
┌─────────────────────────────────────────────────────┐
│                    MHAAlgorithm                     │
│                  (Main Controller)                  │
└──────────────┬────────────────────┬─────────────────┘
               │                    │
       ┌───────▼────────┐   ┌──────▼──────────┐
       │    Phase 1     │   │     Phase 2     │
       │  Layer/Block   │   │  Genetic Algo   │
       │   Heuristics   │   │   (Residuals)   │
       └───────┬────────┘   └──────┬──────────┘
               │                    │
    ┌──────────▼──────────┐ ┌──────▼──────────┐
    │   LayerBuilder      │ │ GeneticAlgorithm│
    │   BlockBuilder      │ │ PlacementStrategy│
    └──────────┬──────────┘ └──────┬──────────┘
               │                    │
               └────────┬───────────┘
                        │
            ┌───────────▼────────────┐
            │  ConstraintValidator   │
            │  (8 Constraints)       │
            └───────────┬────────────┘
                        │
            ┌───────────▼────────────┐
            │   Data Models          │
            │  (Item, Pallet, Order) │
            └────────────────────────┘
```

### 핵심 컴포넌트

#### 1. **MHAAlgorithm** (E:\Github\MHAPalletizing\MHAAlgorithm.cs)
   - 2단계 알고리즘의 메인 컨트롤러
   - Phase 1과 Phase 2를 순차적으로 실행
   - 최종 솔루션 검증 및 통계 생성

#### 2. **Phase 1: Constructive Heuristics** (E:\Github\MHAPalletizing\Phase1\)
   - **LayerBuilder**: 동일 제품으로 레이어 생성 (Full/Half/Quarter)
   - **BlockBuilder**: 레이어를 조합하여 블록 생성
   - **Layer**: 동일 높이의 아이템 그룹

#### 3. **Phase 2: Genetic Algorithm** (E:\Github\MHAPalletizing\Phase2\)
   - **GeneticAlgorithm**: NSGA-II 기반 다목적 최적화
   - **Individual**: 아이템 타입의 배치 순서를 인코딩
   - **PlacementStrategy**: Extreme Points 기반 배치
   - **ExtremePoint**: 아이템 배치 가능 위치

#### 4. **Constraints** (E:\Github\MHAPalletizing\Constraints\)
   - **ConstraintValidator**: 8가지 제약조건 검증
   - Hard Constraints: 1-6 (필수)
   - Soft Constraints: 7-8 (최적화 목표)

#### 5. **Models** (E:\Github\MHAPalletizing\Models\)
   - **Item**: 박스 아이템 (크기, 무게, 위치, 회전)
   - **Pallet**: 유로 팔레트 (1200×800×1400mm)
   - **Order**: 주문 (아이템 목록, 통계)

---

## 알고리즘 설명

### 2-Phase Hybrid Algorithm

#### **Phase 1: Constructive Heuristics**

1. **Layer Generation** (LayerBuilder)
   - 동일 제품으로 레이어 생성
   - 3가지 타입: Full (1200×800), Half (600×800), Quarter (600×400)
   - 최소 Fill Rate 기준: Full≥90%, Half≥90%, Quarter≥85%
   - 두 방향(기본/회전)으로 배치 시도

2. **Layer Placement**
   - Fill Rate가 높은 레이어부터 팔레트에 배치
   - 기존 팔레트에 추가 시도 후 새 팔레트 생성
   - Dynamic Shifting으로 안정성 향상

3. **Residuals Collection**
   - Phase 1에서 배치되지 않은 아이템 수집
   - Phase 2로 전달

#### **Phase 2: Genetic Algorithm**

1. **Population Initialization**
   - 10개의 Custom Individuals (논문 Table 5)
     - Weight (증가/감소)
     - Quantity (증가/감소)
     - Base Surface Area (증가/감소)
     - Volume (증가/감소)
     - Volume × Quantity (증가/감소)
   - 90개의 Random Individuals
   - Total Population: 100

2. **Evaluation**
   - Extreme Points 기반 배치 전략
   - Fitness 1: Heterogeneity (최소화)
   - Fitness 2: Compactness (최대화)

3. **Selection (NSGA-II)**
   - Fast Non-Dominated Sorting
   - Crowding Distance 계산
   - Mu (15) 개체 선택

4. **Genetic Operators**
   - Crossover (50%): Single-Point Crossover
   - Mutation (20%): Swap Mutation
   - Lambda (30) 자손 생성

5. **Evolution**
   - Mu + Lambda Strategy
   - Max Generations: 30
   - Early Stopping: 5세대 정체 시

### Extreme Points Placement

```
1. 초기 EP: 팔레트 원점 (0, 0, 0)
2. 아이템 배치 시 3개의 새 EP 생성:
   - EP1: (x + length, y, z)
   - EP2: (x, y + width, z)
   - EP3: (x, y, z + height)
3. EP 우선순위: Z 좌표 → 원점 거리
4. 제약조건 검증 후 배치
```

---

## 8가지 제약조건

### Hard Constraints (필수)

#### 1. **Item Orientation (아이템 방향)**
- Z축 회전만 허용 (0° 또는 90°)
- X, Y축 회전 금지 (상하 뒤집기 방지)
- 구현: `ConstraintValidator.ValidateOrientation()`

#### 2. **Non-Collision (충돌 방지)**
- 아이템 간 겹침 금지
- AABB (Axis-Aligned Bounding Box) 충돌 검사
- 구현: `ConstraintValidator.ValidateNonCollision()`

#### 3. **Stability (안정성)**
- 무게 중심이 팔레트 중심 근처에 위치
- 허용 오차: 중심에서 ±20% (기본값)
- 구현: `Pallet.IsStable()`, `ConstraintValidator.ValidateStability()`

#### 4. **Support (지지)**
- 아이템의 최소 지지 면적 보장
- 3가지 조건 중 하나 만족:
  - 40% 지지 + 4개 꼭지점
  - 50% 지지 + 3개 꼭지점
  - 75% 지지 + 2개 꼭지점
- 구현: `ConstraintValidator.ValidateSupport()`

#### 5. **Pattern Complexity (패턴 복잡도)**
- 수동 작업 가능한 배치 패턴
- 최소 1개 꼭지점 + 2개 모서리 정렬
- 구현: `ConstraintValidator.ValidatePatternComplexity()`

#### 6. **Complete Shipment (완전 포장)**
- 주문의 모든 아이템 배치 필수
- 구현: `Order.IsCompletelyPacked()`, `ConstraintValidator.ValidateCompleteShipment()`

### Soft Constraints (최적화 목표)

#### 7. **Customer Positioning (고객 위치 지정)**
- 같은 제품끼리 그룹화 (Heterogeneity 최소화)
- Fitness Function 1: 팔레트당 제품 타입 다양성
- 구현: `ConstraintValidator.CalculateHeterogeneity()`

#### 8. **Layer Interlocking (층간 맞물림)**
- 층간 안정성 향상을 위한 맞물림
- Hausdorff Distance로 측정
- 구현: `ConstraintValidator.CalculateHausdorffDistance()`

---

## 설정 옵션

### MHAAlgorithm 파라미터

```csharp
// 생성자
public MHAAlgorithm(int? seed = null)
```

**파라미터:**
- `seed` (int?, optional): 난수 생성기 시드. 재현 가능한 결과를 위해 사용. 기본값: null (랜덤)

```csharp
// Solve 메서드
public List<Pallet> Solve(Order order, int maxPallets = 10)
```

**파라미터:**
- `order` (Order): 처리할 주문 객체
- `maxPallets` (int): 최대 팔레트 수. 기본값: 10

**반환값:**
- `List<Pallet>`: 아이템이 배치된 팔레트 목록

### 제약조건 파라미터

```csharp
// Support 검증
ValidateSupport(Item item, Pallet pallet, double vertexInset = 10)
```
- `vertexInset`: 꼭지점 안쪽 이동 거리 (mm). 기본값: 10mm

```csharp
// Stability 검증
ValidateStability(Pallet pallet, double tolerance = 0.2)
```
- `tolerance`: 무게 중심 허용 오차. 기본값: 0.2 (20%)

### Genetic Algorithm 파라미터

GeneticAlgorithm.cs에서 수정 가능한 상수:

```csharp
private const int MU = 15;              // 다음 세대 선택 개체 수
private const int LAMBDA = 30;          // 자손 개체 수
private const int POPULATION_SIZE = 100; // 초기 개체군 크기
private const double CROSSOVER_PROB = 0.5;  // 교배 확률
private const double MUTATION_PROB = 0.2;   // 돌연변이 확률
private const int MAX_GENERATIONS = 30;     // 최대 세대 수
private const int MAX_STAGNATION = 5;       // 조기 종료 기준
```

### Pallet 설정

```csharp
// 기본 유로 팔레트 크기 (mm)
public const double DEFAULT_LENGTH = 1200;
public const double DEFAULT_WIDTH = 800;
public const double DEFAULT_HEIGHT = 1400;

// 사용자 정의 팔레트
var customPallet = new Pallet(
    palletId: 1,
    length: 1000,
    width: 600,
    maxHeight: 1200
);
```

---

## API 레퍼런스

자세한 API 문서는 [API.md](API.md)를 참조하세요.

### 주요 클래스

- **MHAAlgorithm**: 메인 알고리즘 실행
  - `Solve(Order, int)`: 주문 처리
  - `ValidateSolution(List<Pallet>, Order)`: 솔루션 검증

- **Order**: 주문 관리
  - `AddItem(string, double, double, double, double, int)`: 아이템 추가
  - `Entropy`: 주문 복잡도
  - `IsCompletelyPacked()`: 완전 포장 확인

- **Pallet**: 팔레트 관리
  - `VolumeUtilization`: 공간 활용률
  - `IsStable(double)`: 안정성 검증
  - `GetCenterOfMass()`: 무게 중심 계산

- **Item**: 아이템 정보
  - `Place(double, double, double, bool)`: 아이템 배치
  - `Clone()`: 아이템 복사

---

## 프로젝트 구조

```
MHAPalletizing/
├── MHAPalletizing.sln          # Visual Studio 솔루션
├── README.md                   # 이 문서
├── API.md                      # API 레퍼런스
├── LICENSE                     # 라이선스
│
├── MHAPalletizing/
│   ├── MHAAlgorithm.cs        # 메인 알고리즘
│   ├── Program.cs             # 진입점
│   │
│   ├── Models/                # 데이터 모델
│   │   ├── Item.cs           # 박스 아이템
│   │   ├── Pallet.cs         # 팔레트
│   │   └── Order.cs          # 주문
│   │
│   ├── Constraints/           # 제약조건 검증
│   │   └── ConstraintValidator.cs
│   │
│   ├── Phase1/                # Phase 1: Layer/Block
│   │   ├── Layer.cs          # 레이어
│   │   ├── LayerBuilder.cs   # 레이어 생성
│   │   ├── Block.cs          # 블록
│   │   └── BlockBuilder.cs   # 블록 생성
│   │
│   ├── Phase2/                # Phase 2: Genetic Algorithm
│   │   ├── GeneticAlgorithm.cs      # GA 메인
│   │   ├── Individual.cs            # GA 개체
│   │   ├── PlacementStrategy.cs     # EP 배치 전략
│   │   └── ExtremePoint.cs          # Extreme Point
│   │
│   └── Tests/                 # 테스트
│       ├── BasicTests.cs     # 기본 테스트
│       ├── Phase1Tests.cs    # Phase 1 테스트
│       ├── Phase2Tests.cs    # Phase 2 테스트
│       └── IntegrationTests.cs # 통합 테스트
```

---

## 구현 현황

- [x] 1단계: 기본 데이터 구조 및 제약조건
- [x] 2단계: Phase 1 - Layer Building
- [x] 3단계: Phase 2 - Genetic Algorithm
- [x] 4단계: 통합 및 최적화
- [x] 5단계: 테스트 및 검증

---

## 참고 자료

### 논문 및 데이터셋
- **Dataset 1000**: https://github.com/luferi/3DBPP
- **논문 PDF**: A Multi-Heuristic Algorithm for Multi-Container.pdf
- **IEEE Access**: https://ieeexplore.ieee.org/

### 관련 알고리즘
- **NSGA-II**: Non-dominated Sorting Genetic Algorithm II
- **Extreme Points**: 3D 빈 패킹을 위한 배치 포인트
- **Layer Building**: 동질 제품 레이어 생성 휴리스틱

### .NET Framework
- **.NET Framework 4.8 문서**: https://docs.microsoft.com/dotnet/framework/
- **C# 프로그래밍 가이드**: https://docs.microsoft.com/dotnet/csharp/

---

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다. 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.

---

## 기여

기여를 환영합니다! Pull Request를 제출하기 전에 다음을 확인해주세요:
1. 모든 테스트가 통과하는지 확인
2. 코드 스타일 가이드 준수
3. XML 문서 주석 추가
4. 적절한 커밋 메시지 작성

---

## 문의

- **이슈 트래커**: GitHub Issues
- **이메일**: [your-email@example.com]

---

## 감사의 말

이 프로젝트는 Anan Ashrabi Ananno와 Luis Ribeiro의 연구를 기반으로 합니다.
훌륭한 연구와 공개 데이터셋 제공에 감사드립니다.
