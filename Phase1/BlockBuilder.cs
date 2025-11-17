using System;
using System.Collections.Generic;
using System.Linq;
using MHAPalletizing.Models;
using MHAPalletizing.Constraints;

namespace MHAPalletizing.Phase1
{
    /// <summary>
    /// Block 생성 알고리즘
    /// 논문 Section IV-B-2: "layers are stacked to produce blocks"
    /// </summary>
    public class BlockBuilder
    {
        /// <summary>
        /// Layer들을 Block으로 조립
        /// 논문: "Full → Half → Quarter 순서로 배치"
        /// </summary>
        public static List<Block> BuildBlocks(List<Layer> layers, out List<Layer> remainingLayers)
        {
            var blocks = new List<Block>();
            var availableLayers = new List<Layer>(layers);

            // Layer를 타입별로 분류
            var fullLayers = availableLayers.Where(l => l.Type == LayerType.Full)
                                           .OrderByDescending(l => l.OccupiedArea)
                                           .ThenByDescending(l => l.Items.Sum(i => i.Weight))
                                           .ThenBy(l => l.Items[0].ProductId)
                                           .ToList();

            var halfLayers = availableLayers.Where(l => l.Type == LayerType.Half)
                                           .OrderByDescending(l => l.OccupiedArea)
                                           .ThenByDescending(l => l.Items.Sum(i => i.Weight))
                                           .ThenBy(l => l.Items[0].ProductId)
                                           .ToList();

            var quarterLayers = availableLayers.Where(l => l.Type == LayerType.Quarter)
                                              .OrderByDescending(l => l.OccupiedArea)
                                              .ThenByDescending(l => l.Items.Sum(i => i.Weight))
                                              .ThenBy(l => l.Items[0].ProductId)
                                              .ToList();

            // 사용된 Layer 추적
            var usedLayers = new HashSet<Layer>();

            // 1. Full Layer들로 Block 생성
            BuildFullLayerBlocks(blocks, fullLayers, usedLayers);

            // 2. Half Layer들을 기존 Block에 추가하거나 새 Block 생성
            BuildHalfLayerBlocks(blocks, halfLayers, usedLayers);

            // 3. Quarter Layer들을 기존 Block에 추가하거나 새 Block 생성
            BuildQuarterLayerBlocks(blocks, quarterLayers, usedLayers);

            // 사용되지 않은 Layer 반환
            remainingLayers = availableLayers.Where(l => !usedLayers.Contains(l)).ToList();

            return blocks;
        }

        #region Full Layer Stacking
        private static void BuildFullLayerBlocks(List<Block> blocks, List<Layer> fullLayers, HashSet<Layer> usedLayers)
        {
            if (!fullLayers.Any())
                return;

            // 첫 번째 Layer로 Block 시작
            Block currentBlock = null;

            foreach (var layer in fullLayers)
            {
                if (currentBlock == null)
                {
                    // 새 Block 생성
                    int blockId = blocks.Count + 1;
                    var pallet = new Pallet(blockId);
                    currentBlock = new Block(blockId, pallet);
                    blocks.Add(currentBlock);
                }

                // Interlocking 최적화 적용
                var optimizedLayer = OptimizeInterlocking(layer, currentBlock);

                // Layer 추가 시도
                if (!currentBlock.AddLayer(optimizedLayer))
                {
                    // 높이 초과 -> 새 Block 생성
                    int blockId = blocks.Count + 1;
                    var pallet = new Pallet(blockId);
                    currentBlock = new Block(blockId, pallet);
                    blocks.Add(currentBlock);
                    currentBlock.AddLayer(optimizedLayer);
                }

                // Stability 검증
                if (!ConstraintValidator.ValidateStability(currentBlock.Pallet))
                {
                    // 불안정하면 새 Block으로
                    currentBlock.Layers.Remove(optimizedLayer);
                    foreach (var item in optimizedLayer.Items)
                    {
                        currentBlock.Pallet.RemoveItem(item);
                    }

                    int blockId = blocks.Count + 1;
                    var pallet = new Pallet(blockId);
                    currentBlock = new Block(blockId, pallet);
                    blocks.Add(currentBlock);
                    currentBlock.AddLayer(optimizedLayer);
                }

                usedLayers.Add(layer);
            }
        }
        #endregion

        #region Half Layer Stacking
        private static void BuildHalfLayerBlocks(List<Block> blocks, List<Layer> halfLayers, HashSet<Layer> usedLayers)
        {
            if (!halfLayers.Any())
                return;

            foreach (var layer in halfLayers)
            {
                bool placed = false;

                // 기존 Block 중 가장 낮은 Half에 배치 시도
                var sortedBlocks = blocks.OrderBy(b => b.HalfHeights.Min(h => h.Value)).ToList();

                foreach (var block in sortedBlocks)
                {
                    int lowestHalf = block.GetLowestHalf();
                    double lowestHeight = block.HalfHeights[lowestHalf];

                    // 높이 제한 확인
                    if (lowestHeight + layer.Height <= block.Pallet.MaxHeight)
                    {
                        // Half 위치 계산
                        double x = lowestHalf == 0 ? 0 : block.Pallet.Length / 2;
                        double y = 0;

                        var optimizedLayer = OptimizeInterlocking(layer, block);
                        optimizedLayer.PlaceAt(x, y, lowestHeight);

                        // Pallet에 아이템 추가
                        foreach (var item in optimizedLayer.Items)
                        {
                            item.X += x;
                            item.Y = y;
                            item.Z = lowestHeight;
                            block.Pallet.AddItem(item);
                        }

                        block.Layers.Add(optimizedLayer);
                        block.HalfHeights[lowestHalf] += layer.Height;

                        usedLayers.Add(layer);
                        placed = true;
                        break;
                    }
                }

                // 기존 Block에 배치 실패 시 새 Block 생성
                if (!placed)
                {
                    int blockId = blocks.Count + 1;
                    var pallet = new Pallet(blockId);
                    var newBlock = new Block(blockId, pallet);
                    blocks.Add(newBlock);

                    var optimizedLayer = OptimizeInterlocking(layer, newBlock);
                    newBlock.AddLayerAtPosition(optimizedLayer, 0, 0);
                    newBlock.HalfHeights[0] = layer.Height;

                    usedLayers.Add(layer);
                }
            }
        }
        #endregion

        #region Quarter Layer Stacking
        private static void BuildQuarterLayerBlocks(List<Block> blocks, List<Layer> quarterLayers, HashSet<Layer> usedLayers)
        {
            if (!quarterLayers.Any())
                return;

            foreach (var layer in quarterLayers)
            {
                bool placed = false;

                // 기존 Block 중 가장 낮은 Quadrant에 배치 시도
                var sortedBlocks = blocks.OrderBy(b => b.QuadrantHeights.Min(q => q.Value)).ToList();

                foreach (var block in sortedBlocks)
                {
                    var lowestQuadrant = block.GetLowestQuadrant();
                    double lowestHeight = block.QuadrantHeights[lowestQuadrant];

                    if (lowestHeight + layer.Height <= block.Pallet.MaxHeight)
                    {
                        var (x, y) = block.GetQuadrantPosition(lowestQuadrant);

                        var optimizedLayer = OptimizeInterlocking(layer, block);
                        optimizedLayer.PlaceAt(x, y, lowestHeight);

                        foreach (var item in optimizedLayer.Items)
                        {
                            item.X += x;
                            item.Y += y;
                            item.Z = lowestHeight;
                            block.Pallet.AddItem(item);
                        }

                        block.Layers.Add(optimizedLayer);
                        block.QuadrantHeights[lowestQuadrant] += layer.Height;

                        usedLayers.Add(layer);
                        placed = true;
                        break;
                    }
                }

                // 기존 Block에 배치 실패 시 새 Block 생성
                if (!placed)
                {
                    int blockId = blocks.Count + 1;
                    var pallet = new Pallet(blockId);
                    var newBlock = new Block(blockId, pallet);
                    blocks.Add(newBlock);

                    var optimizedLayer = OptimizeInterlocking(layer, newBlock);
                    newBlock.AddLayerAtPosition(optimizedLayer, 0, 0);
                    newBlock.QuadrantHeights[Block.Quadrant.First] = layer.Height;

                    usedLayers.Add(layer);
                }
            }
        }
        #endregion

        #region Interlocking Optimization
        /// <summary>
        /// Hausdorff Distance를 사용하여 Layer Interlocking 최적화
        /// 논문 Section IV-B-2: "maximize Hausdorff distance for better interlocking"
        /// </summary>
        private static Layer OptimizeInterlocking(Layer layer, Block block)
        {
            if (!block.Layers.Any())
                return layer;

            // 마지막 Layer와의 Interlocking 최적화
            var bottomLayer = block.Layers.Last();

            // 4가지 대칭 패턴 테스트 (논문 Section IV-B-2)
            var patterns = new[]
            {
                layer.Clone(),                      // Original
                CreateHorizontalSymmetry(layer),    // Horizontal symmetry
                CreateVerticalSymmetry(layer),      // Vertical symmetry
                CreateBothSymmetry(layer)           // Both symmetries
            };

            // Hausdorff distance가 가장 큰 패턴 선택
            double maxDistance = 0;
            Layer bestPattern = layer;

            foreach (var pattern in patterns)
            {
                double distance = ConstraintValidator.CalculateHausdorffDistance(
                    bottomLayer.Items, pattern.Items);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    bestPattern = pattern;
                }
            }

            return bestPattern;
        }

        private static Layer CreateHorizontalSymmetry(Layer layer)
        {
            var symmetricLayer = layer.Clone();
            double maxY = symmetricLayer.Items.Max(i => i.Y + i.CurrentWidth);

            foreach (var item in symmetricLayer.Items)
            {
                item.Y = maxY - (item.Y + item.CurrentWidth);
            }

            return symmetricLayer;
        }

        private static Layer CreateVerticalSymmetry(Layer layer)
        {
            var symmetricLayer = layer.Clone();
            double maxX = symmetricLayer.Items.Max(i => i.X + i.CurrentLength);

            foreach (var item in symmetricLayer.Items)
            {
                item.X = maxX - (item.X + item.CurrentLength);
            }

            return symmetricLayer;
        }

        private static Layer CreateBothSymmetry(Layer layer)
        {
            var symmetricLayer = CreateHorizontalSymmetry(layer);
            return CreateVerticalSymmetry(symmetricLayer);
        }
        #endregion
    }
}
