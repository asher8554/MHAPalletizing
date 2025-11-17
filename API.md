# MHA Palletizing - API Reference

이 문서는 MHA Palletizing 프로젝트의 Public API에 대한 상세한 레퍼런스를 제공합니다.

---

## 목차

- [MHAAlgorithm](#mhaalgorithm)
- [Models](#models)
  - [Order](#order)
  - [Item](#item)
  - [Pallet](#pallet)
- [Phase 1 - Constructive Heuristics](#phase-1---constructive-heuristics)
  - [LayerBuilder](#layerbuilder)
  - [Layer](#layer)
- [Phase 2 - Genetic Algorithm](#phase-2---genetic-algorithm)
  - [GeneticAlgorithm](#geneticalgorithm)
  - [PlacementStrategy](#placementstrategy)
  - [ExtremePoint](#extremepoint)
- [Constraints](#constraints)
  - [ConstraintValidator](#constraintvalidator)
- [사용 예제](#사용-예제)

---

## MHAAlgorithm

**Namespace:** `MHAPalletizing`

**File:** `E:\Github\MHAPalletizing\MHAAlgorithm.cs`

Multi-Heuristic Algorithm의 메인 컨트롤러 클래스입니다. 2단계 하이브리드 알고리즘을 실행합니다.

### 생성자

```csharp
public MHAAlgorithm(int? seed = null)
```

**파라미터:**
- `seed` (int?, optional): 난수 생성기 시드. 재현 가능한 결과를 위해 사용. `null`인 경우 랜덤 시드 사용.

**예제:**
```csharp
// 재현 가능한 결과
var mha = new MHAAlgorithm(seed: 42);

// 랜덤 시드
var mhaRandom = new MHAAlgorithm();
```

### 메서드

#### Solve

```csharp
public List<Pallet> Solve(Order order, int maxPallets = 10)
```

주문을 처리하여 팔레트에 아이템을 최적으로 배치합니다.

**파라미터:**
- `order` (Order): 처리할 주문 객체
- `maxPallets` (int, default=10): 사용 가능한 최대 팔레트 수

**반환값:**
- `List<Pallet>`: 아이템이 배치된 팔레트 목록

**동작 과정:**
1. **Phase 1**: LayerBuilder로 동일 제품 레이어 생성 및 배치
2. **Phase 2**: Residual 아이템을 Genetic Algorithm으로 배치
3. 최종 통계 출력

**예제:**
```csharp
var order = new Order("ORDER_001");
order.AddItem("MILK_1L", 100, 80, 200, 1.0, 20);
order.AddItem("JUICE_500ML", 90, 70, 180, 0.8, 15);

var mha = new MHAAlgorithm(seed: 42);
var pallets = mha.Solve(order, maxPallets: 5);

Console.WriteLine($"총 팔레트 수: {pallets.Count}");
Console.WriteLine($"평균 공간 활용률: {pallets.Average(p => p.VolumeUtilization):P2}");
```

#### ValidateSolution

```csharp
public void ValidateSolution(List<Pallet> pallets, Order order)
```

생성된 솔루션의 유효성을 검증하고 상세 통계를 출력합니다.

**파라미터:**
- `pallets` (List<Pallet>): 검증할 팔레트 목록
- `order` (Order): 원본 주문 객체

**출력 정보:**
- 배치된 아이템 수 vs 주문 아이템 수
- 팔레트별 공간 활용률, 높이, 무게
- 팔레트별 제품 타입 분포

**예제:**
```csharp
var pallets = mha.Solve(order, maxPallets: 5);
mha.ValidateSolution(pallets, order);
```

---

## Models

### Order

**Namespace:** `MHAPalletizing.Models`

**File:** `E:\Github\MHAPalletizing\Models\Order.cs`

고객 주문을 표현하는 클래스입니다.

#### 생성자

```csharp
public Order(string orderId)
```

**파라미터:**
- `orderId` (string): 주문 ID

#### 속성

| 속성 | 타입 | 설명 |
|------|------|------|
| `OrderId` | string | 주문 ID |
| `Items` | List&lt;Item&gt; | 주문 아이템 목록 |
| `Pallets` | List&lt;Pallet&gt; | 배치된 팔레트 목록 |
| `TotalItemCount` | int | 전체 아이템 수 |
| `ProductTypeCount` | int | 제품 타입 수 |
| `TotalVolume` | double | 전체 부피 (mm³) |
| `TotalWeight` | double | 전체 무게 (kg) |
| `Entropy` | double | 주문 복잡도 (0~1) |

#### 메서드

##### AddItem

```csharp
public void AddItem(string productId, double length, double width, double height, double weight, int quantity = 1)
```

주문에 아이템을 추가합니다.

**파라미터:**
- `productId` (string): 제품 ID
- `length` (double): 길이 (mm)
- `width` (double): 너비 (mm)
- `height` (double): 높이 (mm)
- `weight` (double): 무게 (kg)
- `quantity` (int, default=1): 수량

**예제:**
```csharp
var order = new Order("ORDER_001");
order.AddItem("MILK_1L", 100, 80, 200, 1.0, 20);
order.AddItem("JUICE_500ML", 90, 70, 180, 0.8, 15);
```

##### GetComplexityClass

```csharp
public string GetComplexityClass()
```

Entropy 기반 복잡도 클래스를 반환합니다.

**반환값:**
- Entropy Interval 1 [0, 0.2]
- Entropy Interval 2 (0.2, 0.4]
- Entropy Interval 3 (0.4, 0.6]
- Entropy Interval 4 (0.6, 0.8]
- Entropy Interval 5 (0.8, 1.0]

##### GetSizeClass

```csharp
public string GetSizeClass()
```

아이템 수 기반 크기 클래스를 반환합니다.

**반환값:**
- "Small": 아이템 수 < 600
- "Medium": 600 ≤ 아이템 수 < 1300
- "Large": 아이템 수 ≥ 1300

##### IsCompletelyPacked

```csharp
public bool IsCompletelyPacked()
```

모든 아이템이 배치되었는지 확인합니다 (Constraint 6).

**반환값:**
- `true`: 모든 아이템 배치됨
- `false`: 일부 아이템 미배치

---

### Item

**Namespace:** `MHAPalletizing.Models`

**File:** `E:\Github\MHAPalletizing\Models\Item.cs`

팔레트에 배치될 박스 아이템을 표현하는 클래스입니다.

#### 생성자

```csharp
public Item(string productId, int itemId, double length, double width, double height, double weight)
```

**파라미터:**
- `productId` (string): 제품 ID
- `itemId` (int): 아이템 ID (고유 식별자)
- `length` (double): 길이 (mm)
- `width` (double): 너비 (mm)
- `height` (double): 높이 (mm)
- `weight` (double): 무게 (kg)

#### 속성

| 속성 | 타입 | 설명 |
|------|------|------|
| `ProductId` | string | 제품 ID |
| `ItemId` | int | 아이템 ID |
| `Length` | double | 원본 길이 (mm) |
| `Width` | double | 원본 너비 (mm) |
| `Height` | double | 원본 높이 (mm) |
| `Weight` | double | 무게 (kg) |
| `Volume` | double | 부피 (mm³) |
| `X` | double | X 좌표 (mm) |
| `Y` | double | Y 좌표 (mm) |
| `Z` | double | Z 좌표 (mm) |
| `RotationZ` | double | Z축 회전각 (0° or 90°) |
| `IsRotated` | bool | 회전 여부 |
| `CurrentLength` | double | 현재 길이 (회전 고려) |
| `CurrentWidth` | double | 현재 너비 (회전 고려) |
| `CurrentHeight` | double | 현재 높이 (항상 동일) |

#### 메서드

##### Place

```csharp
public void Place(double x, double y, double z, bool rotated = false)
```

아이템을 특정 위치에 배치합니다.

**파라미터:**
- `x` (double): X 좌표
- `y` (double): Y 좌표
- `z` (double): Z 좌표
- `rotated` (bool, default=false): 90° 회전 여부

**예제:**
```csharp
var item = new Item("MILK", 1, 100, 80, 200, 1.0);
item.Place(0, 0, 0, rotated: false);  // 원점에 기본 방향으로 배치
item.Place(100, 0, 0, rotated: true); // (100, 0, 0)에 90° 회전하여 배치
```

##### Clone

```csharp
public Item Clone()
```

아이템의 복사본을 생성합니다.

**반환값:**
- `Item`: 모든 속성이 복사된 새 아이템 객체

##### GetCenterOfMass

```csharp
public (double x, double y, double z) GetCenterOfMass()
```

아이템의 무게 중심 좌표를 반환합니다.

**반환값:**
- `(double, double, double)`: (x, y, z) 무게 중심 좌표

---

### Pallet

**Namespace:** `MHAPalletizing.Models`

**File:** `E:\Github\MHAPalletizing\Models\Pallet.cs`

유로 팔레트를 표현하는 클래스입니다.

#### 상수

```csharp
public const double DEFAULT_LENGTH = 1200;  // mm
public const double DEFAULT_WIDTH = 800;    // mm
public const double DEFAULT_HEIGHT = 1400;  // mm
```

#### 생성자

```csharp
public Pallet(int palletId, double length = 1200, double width = 800, double maxHeight = 1400)
```

**파라미터:**
- `palletId` (int): 팔레트 ID
- `length` (double, default=1200): 길이 (mm)
- `width` (double, default=800): 너비 (mm)
- `maxHeight` (double, default=1400): 최대 적재 높이 (mm)

#### 속성

| 속성 | 타입 | 설명 |
|------|------|------|
| `PalletId` | int | 팔레트 ID |
| `Length` | double | 길이 (mm) |
| `Width` | double | 너비 (mm) |
| `MaxHeight` | double | 최대 적재 높이 (mm) |
| `Items` | List&lt;Item&gt; | 배치된 아이템 목록 |
| `UsedVolume` | double | 사용된 부피 (mm³) |
| `TotalVolume` | double | 전체 부피 (mm³) |
| `VolumeUtilization` | double | 공간 활용률 (0~1) |
| `TotalWeight` | double | 총 무게 (kg) |
| `CurrentHeight` | double | 현재 최고 높이 (mm) |

#### 메서드

##### AddItem / RemoveItem

```csharp
public void AddItem(Item item)
public void RemoveItem(Item item)
```

팔레트에 아이템을 추가하거나 제거합니다.

##### IsStable

```csharp
public bool IsStable(double tolerance = 0.2)
```

팔레트의 안정성을 검증합니다 (Constraint 3).

**파라미터:**
- `tolerance` (double, default=0.2): 무게 중심 허용 오차 (0.2 = 20%)

**반환값:**
- `true`: 무게 중심이 안정 영역 내
- `false`: 무게 중심이 안정 영역 벗어남

**예제:**
```csharp
var pallet = new Pallet(1);
// ... 아이템 배치 ...

if (pallet.IsStable(tolerance: 0.2))
    Console.WriteLine("팔레트가 안정적입니다.");
else
    Console.WriteLine("경고: 팔레트가 불안정합니다!");
```

##### GetCenterOfMass

```csharp
public (double x, double y, double z) GetCenterOfMass()
```

팔레트의 무게 중심을 계산합니다.

**반환값:**
- `(double, double, double)`: (x, y, z) 무게 중심 좌표

##### GetAverageCompactness

```csharp
public double GetAverageCompactness()
```

팔레트 내 아이템들의 평균 Compactness를 계산합니다.

**반환값:**
- `double`: 평균 Compactness (0~1). 높을수록 아이템 간 접촉이 많음.

---

## Phase 1 - Constructive Heuristics

### LayerBuilder

**Namespace:** `MHAPalletizing.Phase1`

**File:** `E:\Github\MHAPalletizing\Phase1\LayerBuilder.cs`

동일 제품으로 구성된 레이어를 생성하는 정적 클래스입니다.

#### 메서드

##### BuildLayers

```csharp
public static List<Layer> BuildLayers(Order order)
```

주문의 모든 아이템으로부터 가능한 모든 레이어를 생성합니다.

**파라미터:**
- `order` (Order): 레이어를 생성할 주문 객체

**반환값:**
- `List<Layer>`: 생성된 레이어 목록 (Fill Rate가 높은 순)

**레이어 타입:**
- **Full Layer**: 1200mm × 800mm, Fill Rate ≥ 90%
- **Half Layer**: 600mm × 800mm, Fill Rate ≥ 90%
- **Quarter Layer**: 600mm × 400mm, Fill Rate ≥ 85%

**예제:**
```csharp
var order = new Order("TEST");
order.AddItem("MILK", 100, 80, 200, 1.0, 50);

var layers = LayerBuilder.BuildLayers(order);
Console.WriteLine($"총 {layers.Count}개 레이어 생성됨");

foreach (var layer in layers)
{
    Console.WriteLine($"{layer.Type} Layer: {layer.Items.Count} items, Fill Rate: {layer.FillRate:P2}");
}
```

---

### Layer

**Namespace:** `MHAPalletizing.Phase1`

**File:** `E:\Github\MHAPalletizing\Phase1\Layer.cs`

동일 또는 유사한 높이의 아이템들로 구성된 레이어를 표현합니다.

#### 열거형: LayerType

```csharp
public enum LayerType
{
    Full,    // 전체 팔레트 면적 (1200×800)
    Half,    // 1/2 면적 (600×800)
    Quarter  // 1/4 면적 (600×400)
}
```

#### 생성자

```csharp
public Layer(int layerId, LayerType type, double baseArea, double height = 0)
```

#### 속성

| 속성 | 타입 | 설명 |
|------|------|------|
| `LayerId` | int | 레이어 ID |
| `Type` | LayerType | 레이어 타입 |
| `Items` | List&lt;Item&gt; | 레이어 내 아이템 목록 |
| `Height` | double | 레이어 높이 (mm) |
| `BaseArea` | double | 레이어 면적 (mm²) |
| `OccupiedArea` | double | 사용된 면적 (mm²) |
| `FillRate` | double | 면적 활용률 (0~1) |
| `IsHomogeneous` | bool | 동종 레이어 여부 |

---

## Phase 2 - Genetic Algorithm

### GeneticAlgorithm

**Namespace:** `MHAPalletizing.Phase2`

**File:** `E:\Github\MHAPalletizing\Phase2\GeneticAlgorithm.cs`

Phase 1에서 배치되지 않은 Residual 아이템을 NSGA-II 기반 유전 알고리즘으로 배치합니다.

#### 생성자

```csharp
public GeneticAlgorithm(Random random = null)
```

**파라미터:**
- `random` (Random, optional): 난수 생성기. `null`인 경우 새 인스턴스 생성.

#### 메서드

##### PackResiduals

```csharp
public bool PackResiduals(List<Item> residuals, List<Pallet> pallets, out List<Pallet> usedPallets)
```

Residual 아이템들을 유전 알고리즘으로 배치합니다.

**파라미터:**
- `residuals` (List&lt;Item&gt;): 배치할 Residual 아이템 목록
- `pallets` (List&lt;Pallet&gt;): 사용 가능한 빈 팔레트 목록
- `usedPallets` (out List&lt;Pallet&gt;): 아이템이 배치된 팔레트 목록 (출력)

**반환값:**
- `bool`: 모든 Residual이 성공적으로 배치되면 `true`, 그렇지 않으면 `false`

**GA 파라미터:**
- Population Size: 100
- Mu (선택): 15
- Lambda (자손): 30
- Crossover Probability: 0.5
- Mutation Probability: 0.2
- Max Generations: 30
- Early Stopping: 5세대 정체

**Fitness 함수:**
- **Fitness 1**: Heterogeneity (최소화) - 팔레트당 제품 타입 다양성
- **Fitness 2**: Compactness (최대화) - 아이템 간 접촉 면적 비율

**예제:**
```csharp
var residuals = new List<Item> { /* Phase 1에서 남은 아이템들 */ };
var pallets = new List<Pallet> { new Pallet(1), new Pallet(2) };

var ga = new GeneticAlgorithm(new Random(42));
bool success = ga.PackResiduals(residuals, pallets, out var usedPallets);

if (success)
{
    Console.WriteLine($"성공! {usedPallets.Count}개 팔레트 사용");
    foreach (var pallet in usedPallets)
        Console.WriteLine($"  Pallet {pallet.PalletId}: {pallet.Items.Count} items");
}
else
    Console.WriteLine("배치 실패: 일부 아이템을 배치할 수 없습니다.");
```

---

### PlacementStrategy

**Namespace:** `MHAPalletizing.Phase2`

**File:** `E:\Github\MHAPalletizing\Phase2\PlacementStrategy.cs`

Extreme Points 기반 배치 전략을 구현합니다.

#### 생성자

```csharp
public PlacementStrategy(Pallet pallet)
```

**파라미터:**
- `pallet` (Pallet): 아이템을 배치할 팔레트

#### 메서드

##### TryPlaceItem

```csharp
public bool TryPlaceItem(Item item, bool allowRotation = true)
```

아이템을 배치 가능한 Extreme Point에 배치합니다.

**파라미터:**
- `item` (Item): 배치할 아이템. 배치 성공 시 위치와 회전 정보가 업데이트됨.
- `allowRotation` (bool, default=true): Z축 회전 허용 여부

**반환값:**
- `bool`: 배치 성공 시 `true`, 실패 시 `false`

**배치 프로세스:**
1. 사용되지 않은 EP를 우선순위 순으로 탐색
2. 각 EP에서 회전 방향(0°, 90°)을 시도
3. 제약조건 검증 (충돌, 지지, 안정성, 팔레트 범위)
4. 배치 성공 시 해당 EP를 사용됨으로 표시하고 3개의 새 EP 생성

**예제:**
```csharp
var pallet = new Pallet(1);
var strategy = new PlacementStrategy(pallet);
var item = new Item("MILK", 1, 100, 80, 200, 1.0);

if (strategy.TryPlaceItem(item, allowRotation: true))
{
    pallet.AddItem(item);
    Console.WriteLine($"배치 성공: ({item.X}, {item.Y}, {item.Z}), 회전: {item.IsRotated}");
}
else
    Console.WriteLine("배치 실패");
```

##### GetAvailableEPCount

```csharp
public int GetAvailableEPCount()
```

사용 가능한 Extreme Point 개수를 반환합니다.

**반환값:**
- `int`: 사용되지 않은 EP 개수

##### Reset

```csharp
public void Reset()
```

Extreme Point를 초기화합니다 (재시작용).

---

### ExtremePoint

**Namespace:** `MHAPalletizing.Phase2`

**File:** `E:\Github\MHAPalletizing\Phase2\ExtremePoint.cs`

아이템 배치 가능 위치를 표현하는 클래스입니다.

#### 생성자

```csharp
public ExtremePoint(double x, double y, double z)
```

**파라미터:**
- `x` (double): X 좌표
- `y` (double): Y 좌표
- `z` (double): Z 좌표

#### 속성

| 속성 | 타입 | 설명 |
|------|------|------|
| `X` | double | X 좌표 (mm) |
| `Y` | double | Y 좌표 (mm) |
| `Z` | double | Z 좌표 (mm) |
| `IsUsed` | bool | EP 사용 여부 |
| `Priority` | double | EP 우선순위 (낮을수록 우선) |

#### 메서드

##### GenerateNewPoints

```csharp
public ExtremePoint[] GenerateNewPoints(Item item)
```

아이템을 이 EP에 배치했을 때 생성되는 3개의 새 EP를 반환합니다.

**파라미터:**
- `item` (Item): 배치된 아이템

**반환값:**
- `ExtremePoint[]`: 3개의 새 EP
  - EP1: (x + length, y, z) - X 방향
  - EP2: (x, y + width, z) - Y 방향
  - EP3: (x, y, z + height) - Z 방향

**예제:**
```csharp
var ep = new ExtremePoint(0, 0, 0);
var item = new Item("MILK", 1, 100, 80, 200, 1.0);
item.Place(ep.X, ep.Y, ep.Z);

var newEPs = ep.GenerateNewPoints(item);
// newEPs[0]: (100, 0, 0)
// newEPs[1]: (0, 80, 0)
// newEPs[2]: (0, 0, 200)
```

---

## Constraints

### ConstraintValidator

**Namespace:** `MHAPalletizing.Constraints`

**File:** `E:\Github\MHAPalletizing\Constraints\ConstraintValidator.cs`

8가지 제약조건을 검증하는 정적 클래스입니다.

#### Hard Constraints (필수)

##### 1. ValidateOrientation

```csharp
public static bool ValidateOrientation(Item item)
```

Constraint 1: Z축 회전만 허용 (0° 또는 90°)

**파라미터:**
- `item` (Item): 검증할 아이템

**반환값:**
- `bool`: 회전각이 0° 또는 90°이면 `true`

##### 2. ValidateNonCollision

```csharp
public static bool ValidateNonCollision(Item newItem, List<Item> existingItems)
```

Constraint 2: 아이템 간 충돌 방지 (AABB 충돌 검사)

**파라미터:**
- `newItem` (Item): 배치할 새 아이템
- `existingItems` (List&lt;Item&gt;): 기존 아이템 목록

**반환값:**
- `bool`: 충돌이 없으면 `true`

##### 3. ValidateStability

```csharp
public static bool ValidateStability(Pallet pallet, double tolerance = 0.2)
```

Constraint 3: 무게 중심이 안정 영역 내에 위치

**파라미터:**
- `pallet` (Pallet): 검증할 팔레트
- `tolerance` (double, default=0.2): 허용 오차 (0.2 = 20%)

**반환값:**
- `bool`: 안정적이면 `true`

##### 4. ValidateSupport

```csharp
public static bool ValidateSupport(Item item, Pallet pallet, double vertexInset = 10)
```

Constraint 4: 아이템의 적절한 지지 검증

**파라미터:**
- `item` (Item): 검증할 아이템
- `pallet` (Pallet): 팔레트
- `vertexInset` (double, default=10): 꼭지점 안쪽 이동 거리 (mm)

**반환값:**
- `bool`: 다음 조건 중 하나를 만족하면 `true`
  - 40% 지지 + 4개 꼭지점
  - 50% 지지 + 3개 꼭지점
  - 75% 지지 + 2개 꼭지점

##### 5. ValidatePatternComplexity

```csharp
public static bool ValidatePatternComplexity(Item item, Pallet pallet)
```

Constraint 5: 수동 작업 가능한 배치 패턴

**파라미터:**
- `item` (Item): 검증할 아이템
- `pallet` (Pallet): 팔레트

**반환값:**
- `bool`: 최소 1개 꼭지점 + 2개 모서리 정렬되면 `true`

##### 6. ValidateCompleteShipment

```csharp
public static bool ValidateCompleteShipment(Order order)
```

Constraint 6: 주문의 모든 아이템이 배치됨

**파라미터:**
- `order` (Order): 검증할 주문

**반환값:**
- `bool`: 모든 아이템이 배치되면 `true`

#### Soft Constraints (최적화 목표)

##### 7. CalculateHeterogeneity

```csharp
public static double CalculateHeterogeneity(Pallet pallet)
public static double CalculateAverageHeterogeneity(List<Pallet> pallets)
```

Constraint 7: 팔레트 내 제품 타입 다양성 (낮을수록 좋음)

**반환값:**
- `double`: Heterogeneity 값 (0~1). 0에 가까울수록 동일 제품끼리 잘 그룹화됨.

##### 8. CalculateHausdorffDistance

```csharp
public static double CalculateHausdorffDistance(List<Item> bottomLayer, List<Item> topLayer)
```

Constraint 8: 층간 맞물림 측정 (Hausdorff Distance)

**파라미터:**
- `bottomLayer` (List&lt;Item&gt;): 하단 레이어 아이템 목록
- `topLayer` (List&lt;Item&gt;): 상단 레이어 아이템 목록

**반환값:**
- `double`: Hausdorff Distance (낮을수록 층간 맞물림이 좋음)

#### 종합 검증

##### ValidatePlacement

```csharp
public static bool ValidatePlacement(Item item, Pallet pallet, double vertexInset = 10, double stabilityTolerance = 0.2)
```

아이템 배치 전 모든 Hard Constraints를 한 번에 검증합니다.

**파라미터:**
- `item` (Item): 배치할 아이템
- `pallet` (Pallet): 팔레트
- `vertexInset` (double, default=10): 꼭지점 안쪽 이동 거리
- `stabilityTolerance` (double, default=0.2): 안정성 허용 오차

**반환값:**
- `bool`: 모든 제약조건을 만족하면 `true`

**검증 순서:**
1. Orientation (Z축 회전)
2. Non-collision (충돌 방지)
3. Stability (안정성)
4. Support (지지)

**예제:**
```csharp
var pallet = new Pallet(1);
var item = new Item("MILK", 1, 100, 80, 200, 1.0);
item.Place(0, 0, 0);

if (ConstraintValidator.ValidatePlacement(item, pallet))
{
    pallet.AddItem(item);
    Console.WriteLine("배치 성공 - 모든 제약조건 만족");
}
else
    Console.WriteLine("배치 실패 - 제약조건 위반");
```

---

## 사용 예제

### 예제 1: 기본 사용법

```csharp
using MHAPalletizing;
using MHAPalletizing.Models;

// 1. 주문 생성
var order = new Order("ORDER_001");

// 2. 아이템 추가
order.AddItem("MILK_1L", 100, 80, 200, 1.0, 20);
order.AddItem("JUICE_500ML", 90, 70, 180, 0.8, 15);
order.AddItem("WATER_1.5L", 110, 90, 220, 1.5, 10);

// 3. MHA 알고리즘 실행
var mha = new MHAAlgorithm(seed: 42);
var pallets = mha.Solve(order, maxPallets: 5);

// 4. 결과 검증
mha.ValidateSolution(pallets, order);

// 5. 결과 출력
Console.WriteLine($"\n=== 배치 결과 ===");
Console.WriteLine($"사용된 팔레트 수: {pallets.Count}");
Console.WriteLine($"평균 공간 활용률: {pallets.Average(p => p.VolumeUtilization):P2}");

foreach (var pallet in pallets)
{
    Console.WriteLine($"\nPallet {pallet.PalletId}:");
    Console.WriteLine($"  아이템 수: {pallet.Items.Count}");
    Console.WriteLine($"  공간 활용률: {pallet.VolumeUtilization:P2}");
    Console.WriteLine($"  높이: {pallet.CurrentHeight:F0}/{pallet.MaxHeight:F0}mm");
    Console.WriteLine($"  무게: {pallet.TotalWeight:F2}kg");
    Console.WriteLine($"  안정성: {pallet.IsStable()}");
}
```

### 예제 2: Phase별 실행

```csharp
using MHAPalletizing;
using MHAPalletizing.Models;
using MHAPalletizing.Phase1;
using MHAPalletizing.Phase2;

var order = new Order("ORDER_002");
order.AddItem("BOX_A", 200, 150, 100, 3.0, 30);
order.AddItem("BOX_B", 180, 140, 120, 2.5, 25);

// Phase 1: Layer Building
Console.WriteLine("=== Phase 1: Layer Building ===");
var layers = LayerBuilder.BuildLayers(order);
Console.WriteLine($"생성된 레이어 수: {layers.Count}");

foreach (var layer in layers.Take(5))
{
    Console.WriteLine($"Layer {layer.LayerId} ({layer.Type}): " +
                      $"{layer.Items.Count} items, Fill Rate: {layer.FillRate:P2}");
}

// Phase 2: Genetic Algorithm (Residuals가 있는 경우)
// ... MHA.Solve()가 자동으로 처리
```

### 예제 3: 제약조건 검증

```csharp
using MHAPalletizing.Models;
using MHAPalletizing.Constraints;

var pallet = new Pallet(1);
var item1 = new Item("MILK", 1, 100, 80, 200, 1.0);
var item2 = new Item("MILK", 2, 100, 80, 200, 1.0);

// 첫 번째 아이템 배치 (바닥)
item1.Place(0, 0, 0);
if (ConstraintValidator.ValidatePlacement(item1, pallet))
{
    pallet.AddItem(item1);
    Console.WriteLine("Item 1 배치 성공");
}

// 두 번째 아이템 배치 (위에)
item2.Place(0, 0, 200);
if (ConstraintValidator.ValidatePlacement(item2, pallet))
{
    pallet.AddItem(item2);
    Console.WriteLine("Item 2 배치 성공");
}
else
    Console.WriteLine("Item 2 배치 실패 - 제약조건 위반");

// 안정성 검증
if (pallet.IsStable())
    Console.WriteLine("팔레트 안정성: OK");
else
    Console.WriteLine("팔레트 안정성: 경고!");

// Soft Constraints
double heterogeneity = ConstraintValidator.CalculateHeterogeneity(pallet);
Console.WriteLine($"Heterogeneity: {heterogeneity:F3}");

double compactness = pallet.GetAverageCompactness();
Console.WriteLine($"Compactness: {compactness:F3}");
```

### 예제 4: 사용자 정의 팔레트

```csharp
using MHAPalletizing.Models;

// 사용자 정의 크기 팔레트
var customPallet = new Pallet(
    palletId: 1,
    length: 1000,   // 1000mm
    width: 600,     // 600mm
    maxHeight: 1200 // 1200mm
);

Console.WriteLine($"팔레트 크기: {customPallet.Length} × {customPallet.Width} × {customPallet.MaxHeight}mm");
Console.WriteLine($"팔레트 부피: {customPallet.TotalVolume:N0}mm³");
```

### 예제 5: 주문 분석

```csharp
using MHAPalletizing.Models;

var order = new Order("ANALYSIS");
order.AddItem("MILK", 100, 80, 200, 1.0, 30);
order.AddItem("JUICE", 90, 70, 180, 0.8, 20);
order.AddItem("WATER", 110, 90, 220, 1.5, 10);

// 주문 통계
Console.WriteLine($"=== 주문 분석 ===");
Console.WriteLine($"주문 ID: {order.OrderId}");
Console.WriteLine($"총 아이템 수: {order.TotalItemCount}");
Console.WriteLine($"제품 타입 수: {order.ProductTypeCount}");
Console.WriteLine($"총 부피: {order.TotalVolume:N0}mm³");
Console.WriteLine($"총 무게: {order.TotalWeight:F2}kg");
Console.WriteLine($"Entropy: {order.Entropy:F3}");
Console.WriteLine($"복잡도 클래스: {order.GetComplexityClass()}");
Console.WriteLine($"크기 클래스: {order.GetSizeClass()}");
```

---

## 설정 파라미터 요약

### MHAAlgorithm

| 파라미터 | 타입 | 기본값 | 설명 |
|----------|------|--------|------|
| `seed` | int? | null | 난수 생성기 시드 |
| `maxPallets` | int | 10 | 최대 팔레트 수 |

### Genetic Algorithm (GeneticAlgorithm.cs)

| 파라미터 | 값 | 설명 |
|----------|-----|------|
| `MU` | 15 | 다음 세대 선택 개체 수 |
| `LAMBDA` | 30 | 자손 개체 수 |
| `POPULATION_SIZE` | 100 | 초기 개체군 크기 |
| `CROSSOVER_PROB` | 0.5 | 교배 확률 (50%) |
| `MUTATION_PROB` | 0.2 | 돌연변이 확률 (20%) |
| `MAX_GENERATIONS` | 30 | 최대 세대 수 |
| `MAX_STAGNATION` | 5 | 조기 종료 기준 (세대) |

### Layer Builder (LayerBuilder.cs)

| 파라미터 | 값 | 설명 |
|----------|-----|------|
| `FULL_LAYER_MIN_FILL_RATE` | 0.90 | Full Layer 최소 Fill Rate (90%) |
| `HALF_LAYER_MIN_FILL_RATE` | 0.90 | Half Layer 최소 Fill Rate (90%) |
| `QUARTER_LAYER_MIN_FILL_RATE` | 0.85 | Quarter Layer 최소 Fill Rate (85%) |

### Constraint Validator

| 파라미터 | 기본값 | 설명 |
|----------|--------|------|
| `vertexInset` | 10 | 꼭지점 안쪽 이동 거리 (mm) |
| `stabilityTolerance` | 0.2 | 안정성 허용 오차 (20%) |

---

## 반환값 및 에러 처리

### 성공/실패 반환

대부분의 검증 및 배치 메서드는 `bool`을 반환합니다:
- `true`: 성공
- `false`: 실패

### Out 파라미터

일부 메서드는 `out` 파라미터로 결과를 반환합니다:
```csharp
// GeneticAlgorithm.PackResiduals
bool success = ga.PackResiduals(residuals, pallets, out var usedPallets);
```

### 예외 처리

현재 구현에서는 명시적인 예외를 던지지 않지만, 다음 경우 주의가 필요합니다:
- `null` 객체 전달
- 잘못된 파라미터 범위 (예: 음수 크기)
- 빈 목록 전달

---

## 성능 고려사항

### 시간 복잡도

- **Layer Building**: O(n × m), n = 아이템 수, m = 제품 타입 수
- **Genetic Algorithm**: O(g × p × n), g = 세대 수, p = 개체군 크기, n = 아이템 수
- **Extreme Points**: O(e × n), e = EP 수, n = 아이템 수

### 메모리 사용

- Population Size가 크면 메모리 사용량 증가
- 대량 주문(1000+ 아이템)은 4GB+ RAM 권장

### 최적화 팁

1. **적절한 maxPallets 설정**: 과도하게 크면 GA 성능 저하
2. **Seed 사용**: 재현 가능한 결과를 위해 seed 설정
3. **Early Stopping**: MAX_STAGNATION으로 불필요한 세대 실행 방지

---

## 추가 리소스

- **README.md**: 프로젝트 개요 및 설치 가이드
- **논문**: A Multi-Heuristic Algorithm for Multi-Container 3D Bin Packing Problem Optimization
- **테스트 코드**: `E:\Github\MHAPalletizing\Tests\` 디렉토리 참조

---

## 버전 정보

- **Version**: 1.0.0
- **.NET Framework**: 4.8
- **Last Updated**: 2025
