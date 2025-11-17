# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a C# .NET Framework 4.8 implementation of a Multi-Heuristic Algorithm (MHA) for the 3D Bin Packing Problem, based on the IEEE Access 2024 paper "A Multi-Heuristic Algorithm for Multi-Container 3D Bin Packing Problem Optimization Using Real World Constraints" by Anan Ashrabi Ananno and Luis Ribeiro.

The algorithm optimizes palletization for heterogeneous items using real-world constraints, targeting Euro pallets (1200mm × 800mm × 1400mm).

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run all tests
./bin/Debug/MHAPalletizing.exe

# Run with timeout (recommended for non-interactive environments)
timeout 30 ./bin/Debug/MHAPalletizing.exe
```

Note: The executable includes Console.ReadKey() which will throw an exception in non-interactive terminals. Use timeout or redirect output to avoid hanging.

## Architecture Overview

### Two-Phase Algorithm Structure

The implementation follows a strict two-phase approach as described in the paper (Section IV):

**Phase 1: Constructive Heuristics** (`Phase1/`)
- **Layer-based packing**: Items of the same product type are arranged into homogeneous layers
- **Layer types**: Full (1200×800), Half (600×800), Quarter (600×400)
- **Fill rate requirements**: 90% for Full/Half layers, 85% for Quarter layers
- **Block building**: Layers are stacked with interlocking optimization using Hausdorff distance
- **Output**: Packed pallets + residual items that don't fit into efficient layers

**Phase 2: Genetic Algorithm** (`Phase2/`)
- **Purpose**: Pack residual items that couldn't form efficient layers in Phase 1
- **Algorithm**: NSGA-II (Non-dominated Sorting Genetic Algorithm II) with Mu+Lambda strategy
- **Encoding**: Genes represent the *order of item types* (not individual items) to reduce search space
- **Placement**: Uses Extreme Points (EP) strategy for positioning items
- **Fitness**: Multi-objective optimization (minimize heterogeneity, maximize compactness)
- **Parameters**: μ=15, λ=30, population=100, generations=30

### Data Flow

```
Order (items)
    → Phase1: LayerBuilder.BuildLayers()
    → Layer stacking on Pallets
    → Residual items
    → Phase 2: GeneticAlgorithm.PackResiduals()
    → Final pallet solution
```

### Key Architectural Patterns

**1. Extreme Points (EP) Placement**
- After placing an item at position (x, y, z), three new EPs are generated:
  - (x+length, y, z) - along X axis
  - (x, y+width, z) - along Y axis
  - (x, y, z+height) - along Z axis
- EPs are prioritized: lower Z first, then closer to origin
- See `Phase2/ExtremePoint.cs` and `Phase2/PlacementStrategy.cs`

**2. Constraint Validation Architecture**
- All 8 constraints are centralized in `Constraints/ConstraintValidator.cs`
- Constraints are evaluated during placement in PlacementStrategy.ValidatePlacement()
- Hard constraints (1-6): Must be satisfied
- Soft constraints (7-8): Used for fitness optimization

**3. NSGA-II Implementation**
- `Individual.cs`: Represents chromosomes with Pareto ranking and crowding distance
- `GeneticAlgorithm.cs`: Implements fast non-dominated sorting, crowding distance calculation
- Custom individuals from Table 5: 10 heuristic orderings based on weight, quantity, area, volume
- Single-point crossover and swap mutation operators

**4. Layer Building Strategy**
- `LayerBuilder.cs` uses static methods: `BuildLayers()`, `CreateFullLayers()`, etc.
- Tries both orientations (0° and 90° rotation) for each item type
- Dynamic shifting algorithm pushes items to extremities for better stability
- Hierarchical sorting: area → weight → product type for optimal layer placement

## Important Implementation Details

### Coordinate System
- Origin (0,0,0) is at the bottom-left-front corner of the pallet
- X: length (1200mm), Y: width (800mm), Z: height (1400mm)
- Items have MinX, MaxX, MinY, MaxY, MinZ, MaxZ properties for collision detection

### Item Rotation
- Only 90° rotation around Z-axis is allowed (Constraint 1)
- `IsRotated` flag toggles between (Length, Width) and (Width, Length)
- CurrentLength, CurrentWidth, CurrentHeight properties account for rotation

### Support Validation Rules
- Items at Z=0 are always supported (on pallet floor)
- Items above Z=0 require support from items directly below (Z difference < EPSILON)
- Three tiers based on support ratio and vertex count:
  - ≥40% area + 4 vertices, OR
  - ≥50% area + 3 vertices, OR
  - ≥75% area + 2 vertices

### Stability Calculation
- Center of Mass (COM) must be within tolerance distance from pallet center
- COM formula: weighted average of all item centers
- Tolerance: 30% of pallet dimensions (configurable in ValidateStabilityAfterPlacement)

### Hausdorff Distance for Interlocking
- Used in BlockBuilder to optimize layer stacking patterns
- Tests 4 symmetry patterns: original, horizontal flip, vertical flip, both flips
- Maximizes distance between layer contact points for better interlocking

## Testing Structure

Tests are organized by phase and run sequentially in `Program.cs`:

1. **BasicTests.cs**: Data models, constraints, basic operations
2. **Phase1Tests.cs**: Layer creation, block generation (currently disabled due to timeout)
3. **Phase2Tests.cs**: Extreme points, placement strategy, GA execution
4. **IntegrationTests.cs**: Full MHA algorithm end-to-end (reduced dataset for performance)

To enable/disable test suites, comment/uncomment in `Program.cs` Main method.

## Known Limitations

1. **Layer Fill Rate**: The 90%/90%/85% fill rate requirements are strict and may produce 0 layers for small item quantities or sizes. Consider adjusting `FULL_LAYER_MIN_FILL_RATE` constants in `LayerBuilder.cs` for testing.

2. **Performance**: The full layer building process can be slow for large datasets (50+ items). Phase1Tests may timeout - this is expected behavior for comprehensive constraint validation.

3. **Console.ReadKey()**: Program.cs includes `Console.ReadKey()` which fails in non-interactive environments. Always use timeout wrapper or redirect output.

4. **GA Packing Success**: Phase 2 GA may fail to pack all residuals if pallet capacity is insufficient. This is correct behavior - check residual counts in output.

## Modifying the Algorithm

### To adjust constraint strictness:
Edit tolerance values in `Constraints/ConstraintValidator.cs`:
- `STABILITY_TOLERANCE`: COM distance from center (default: 0.3)
- `EPSILON`: Floating-point comparison tolerance (default: 0.1)
- Support percentages in `ValidateSupport()` method

### To change GA parameters:
Edit constants in `Phase2/GeneticAlgorithm.cs`:
- `MU`: Parent selection count (default: 15)
- `LAMBDA`: Offspring count (default: 30)
- `POPULATION_SIZE`: Initial population (default: 100)
- `MAX_GENERATIONS`: Evolution iterations (default: 30)
- `CROSSOVER_PROB`, `MUTATION_PROB`: Genetic operator probabilities

### To modify layer types or fill rates:
Edit `Phase1/LayerBuilder.cs`:
- Layer dimension constants: `PALLET_LENGTH`, `PALLET_WIDTH`
- Fill rate thresholds: `FULL_LAYER_MIN_FILL_RATE`, etc.
- Add new layer types by extending `LayerType` enum in `Phase1/Layer.cs`

## Reference Paper Structure Mapping

The codebase directly implements sections from the IEEE Access 2024 paper:
- **Section IV-A**: Data models (Models/Item.cs, Pallet.cs, Order.cs)
- **Section IV-B-1,2**: Phase 1 layer/block building (Phase1/)
- **Section IV-B-3**: Extreme points and GA (Phase2/)
- **Section IV-C**: NSGA-II parameters and custom individuals (GeneticAlgorithm.cs)
- **Table 5**: 10 custom individual orderings (CreateCustomIndividuals method)
- **Figure 11**: EP generation logic (ExtremePoint.GenerateNewPoints)
- **Figure 12**: Crossover and mutation operators

The paper PDF ("A Multi-Heuristic Algorithm for Multi-Container.pdf") should be consulted for theoretical background and algorithmic justification.

## Performance Optimizations

This implementation includes significant performance optimizations (documented in [OPTIMIZATIONS.md](OPTIMIZATIONS.md)):

**Overall Improvement: 3-6x faster end-to-end**

### Key Optimizations

1. **LayerBuilder HashSet Removal** (Lines 102-104, 163-165, 205-207)
   - Changed O(n²) `List.Remove()` to O(n) `HashSet` + `RemoveAll()`
   - **500x faster** for large datasets (5000+ items)

2. **Pallet.GetCenterOfMass() Single Loop** ([Models/Pallet.cs](Models/Pallet.cs#L66-L88))
   - Reduced 3 separate iterations to 1 loop
   - **3x faster**, called thousands of times during GA

3. **Support Validation Early Exit** ([Constraints/ConstraintValidator.cs](Constraints/ConstraintValidator.cs#L84-L133))
   - Lazy allocation + condition reordering
   - **2.7x faster** for typical cases

4. **AABB Collision Short-Circuit** ([Constraints/ConstraintValidator.cs](Constraints/ConstraintValidator.cs#L44-L55))
   - Early exit on axis mismatch
   - **1.4x faster** on average

### Performance Benchmarks

| Dataset | Before | After | Speedup |
|---------|--------|-------|---------|
| Small (100 items) | ~5s | ~1.5s | **3.3x** |
| Large (1000 items) | ~60s | ~12s | **5x** |
| Dataset10 (1200 orders) | ~180s | ~50s | **3.6x** |

See [OPTIMIZATIONS.md](OPTIMIZATIONS.md) for detailed analysis and future opportunities.
