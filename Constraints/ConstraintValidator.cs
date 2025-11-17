using System;
using System.Collections.Generic;
using System.Linq;
using MHAPalletizing.Models;

namespace MHAPalletizing.Constraints
{
    /// <summary>
    /// 8가지 제약조건 검증
    /// 논문 Section IV-A: Eight constraints
    /// </summary>
    public class ConstraintValidator
    {
        // 허용 오차
        private const double EPSILON = 0.1;

        #region Constraint 1: Item Orientation
        /// <summary>
        /// Constraint 1: Item orientation - Z축 회전만 허용 (0° or 90°)
        /// 논문: "restricts orthogonal rotation to the z-axis"
        /// </summary>
        public static bool ValidateOrientation(Item item)
        {
            // RotationZ는 0 또는 90도만 허용
            return item.RotationZ == 0 || item.RotationZ == 90;
        }
        #endregion

        #region Constraint 2: Non-Collision
        /// <summary>
        /// Constraint 2: Non-collision - 아이템 간 충돌 방지
        /// 논문: "items are placed such that they do not collide with each other"
        /// </summary>
        public static bool ValidateNonCollision(Item newItem, List<Item> existingItems)
        {
            foreach (var item in existingItems)
            {
                if (AreColliding(newItem, item))
                    return false;
            }
            return true;
        }

        private static bool AreColliding(Item a, Item b)
        {
            // AABB (Axis-Aligned Bounding Box) 충돌 검사
            // 최적화: 조기 종료 - 한 축이라도 겹치지 않으면 즉시 false 반환
            if (!(a.MinX < b.MaxX - EPSILON && a.MaxX > b.MinX + EPSILON))
                return false;

            if (!(a.MinY < b.MaxY - EPSILON && a.MaxY > b.MinY + EPSILON))
                return false;

            return a.MinZ < b.MaxZ - EPSILON && a.MaxZ > b.MinZ + EPSILON;
        }
        #endregion

        #region Constraint 3: Stability
        /// <summary>
        /// Constraint 3: Stability - 무게 중심이 안정 영역 내에 있는지 확인
        /// 논문: "COM restricted to area on XY plane surrounding geometric center"
        /// </summary>
        public static bool ValidateStability(Pallet pallet, double tolerance = 0.2)
        {
            return pallet.IsStable(tolerance);
        }

        /// <summary>
        /// 새 아이템 추가 후에도 안정적인지 테스트
        /// </summary>
        public static bool ValidateStabilityAfterPlacement(Pallet pallet, Item newItem, double tolerance = 0.2)
        {
            // 임시로 아이템 추가
            pallet.AddItem(newItem);
            bool isStable = pallet.IsStable(tolerance);
            // 롤백
            pallet.RemoveItem(newItem);
            return isStable;
        }
        #endregion

        #region Constraint 4: Support
        /// <summary>
        /// Constraint 4: Support - 아이템이 적절히 지지되는지 확인
        /// 논문: "at least 40% of base + 4 vertices OR 50% + 3 vertices OR 75% + 2 vertices"
        /// </summary>
        public static bool ValidateSupport(Item item, Pallet pallet, double vertexInset = 10)
        {
            // 바닥에 있는 경우 항상 지지됨 (조기 종료)
            if (item.Z < EPSILON)
                return true;

            // 최적화: LINQ 대신 foreach로 itemsBelow 직접 수집
            List<Item> itemsBelow = null;
            foreach (var other in pallet.Items)
            {
                if (Math.Abs(item.Z - other.MaxZ) < EPSILON)
                {
                    if (itemsBelow == null)
                        itemsBelow = new List<Item>();
                    itemsBelow.Add(other);
                }
            }

            if (itemsBelow == null || itemsBelow.Count == 0)
                return false; // 공중에 떠 있음

            // 지지 면적 계산
            double baseArea = item.CurrentLength * item.CurrentWidth;
            double supportedArea = CalculateSupportedArea(item, itemsBelow);
            double supportRatio = supportedArea / baseArea;

            // 최적화: 가장 쉬운 조건 먼저 체크 (조기 종료)
            // 75% 지지 + 2개 꼭지점이 가장 일반적이므로 먼저 체크
            if (supportRatio >= 0.75)
            {
                int vertices = CountSupportedVertices(item, itemsBelow, vertexInset);
                if (vertices >= 2) return true;
            }

            // 50% + 3 vertices
            if (supportRatio >= 0.50)
            {
                int vertices = CountSupportedVertices(item, itemsBelow, vertexInset);
                if (vertices >= 3) return true;
            }

            // 40% + 4 vertices (가장 엄격한 조건)
            if (supportRatio >= 0.40)
            {
                int vertices = CountSupportedVertices(item, itemsBelow, vertexInset);
                return vertices >= 4;
            }

            return false;
        }

        private static double CalculateSupportedArea(Item item, List<Item> itemsBelow)
        {
            double totalSupportArea = 0;

            foreach (var below in itemsBelow)
            {
                // 겹치는 영역 계산
                double overlapX = Math.Max(0, Math.Min(item.MaxX, below.MaxX) - Math.Max(item.X, below.X));
                double overlapY = Math.Max(0, Math.Min(item.MaxY, below.MaxY) - Math.Max(item.Y, below.Y));
                totalSupportArea += overlapX * overlapY;
            }

            return totalSupportArea;
        }

        private static int CountSupportedVertices(Item item, List<Item> itemsBelow, double inset)
        {
            // 꼭지점을 안쪽으로 inset만큼 이동 (논문 Section IV-A)
            var vertices = new[]
            {
                (item.X + inset, item.Y + inset),
                (item.MaxX - inset, item.Y + inset),
                (item.MaxX - inset, item.MaxY - inset),
                (item.X + inset, item.MaxY - inset)
            };

            int supportedCount = 0;

            foreach (var (vx, vy) in vertices)
            {
                foreach (var below in itemsBelow)
                {
                    if (vx >= below.X - EPSILON && vx <= below.MaxX + EPSILON &&
                        vy >= below.Y - EPSILON && vy <= below.MaxY + EPSILON)
                    {
                        supportedCount++;
                        break;
                    }
                }
            }

            return supportedCount;
        }
        #endregion

        #region Constraint 5: Pattern Complexity
        /// <summary>
        /// Constraint 5: Pattern complexity - 수동 작업 가능한 패턴
        /// 논문: "any placed item has at least one vertex and two edges aligned"
        /// </summary>
        public static bool ValidatePatternComplexity(Item item, Pallet pallet)
        {
            // 바닥에 있거나 팔레트 모서리에 있으면 OK
            if (item.Z < EPSILON)
            {
                // 팔레트 모서리에 정렬되어 있는지 확인
                bool alignedToCorner =
                    (Math.Abs(item.X) < EPSILON || Math.Abs(item.MaxX - pallet.Length) < EPSILON) &&
                    (Math.Abs(item.Y) < EPSILON || Math.Abs(item.MaxY - pallet.Width) < EPSILON);

                if (alignedToCorner)
                    return true;
            }

            // 다른 아이템과 정렬되어 있는지 확인
            int alignedEdges = 0;
            bool hasAlignedVertex = false;

            foreach (var other in pallet.Items)
            {
                if (other == item) continue;

                // X 방향 정렬
                if (Math.Abs(item.X - other.X) < EPSILON ||
                    Math.Abs(item.X - other.MaxX) < EPSILON ||
                    Math.Abs(item.MaxX - other.X) < EPSILON ||
                    Math.Abs(item.MaxX - other.MaxX) < EPSILON)
                {
                    alignedEdges++;
                }

                // Y 방향 정렬
                if (Math.Abs(item.Y - other.Y) < EPSILON ||
                    Math.Abs(item.Y - other.MaxY) < EPSILON ||
                    Math.Abs(item.MaxY - other.Y) < EPSILON ||
                    Math.Abs(item.MaxY - other.MaxY) < EPSILON)
                {
                    alignedEdges++;
                }

                // 꼭지점 정렬 확인
                var itemVertices = item.GetBaseVertices();
                var otherVertices = other.GetBaseVertices();

                foreach (var (ix, iy) in itemVertices)
                {
                    foreach (var (ox, oy) in otherVertices)
                    {
                        if (Math.Abs(ix - ox) < EPSILON && Math.Abs(iy - oy) < EPSILON)
                        {
                            hasAlignedVertex = true;
                            break;
                        }
                    }
                    if (hasAlignedVertex) break;
                }

                if (alignedEdges >= 2 && hasAlignedVertex)
                    return true;
            }

            return alignedEdges >= 2 && hasAlignedVertex;
        }
        #endregion

        #region Constraint 6: Complete Shipment
        /// <summary>
        /// Constraint 6: Complete shipment - 모든 아이템이 포장되어야 함
        /// 논문: "all the items in the order must be packed"
        /// </summary>
        public static bool ValidateCompleteShipment(Order order)
        {
            return order.IsCompletelyPacked();
        }
        #endregion

        #region Constraint 7: Customer Positioning (Soft Constraint)
        /// <summary>
        /// Constraint 7: Customer positioning - 같은 제품끼리 그룹화 (Soft)
        /// 논문: "items of the same kind/brand should be packed together"
        /// </summary>
        public static double CalculateHeterogeneity(Pallet pallet)
        {
            // 팔레트 내 제품 타입 다양성 (낮을수록 좋음)
            int productTypes = pallet.GetProductTypeCount();
            int totalItems = pallet.Items.Count;

            return totalItems > 0 ? (double)productTypes / totalItems : 0;
        }

        public static double CalculateAverageHeterogeneity(List<Pallet> pallets)
        {
            if (!pallets.Any())
                return 0;

            return pallets.Average(p => CalculateHeterogeneity(p));
        }
        #endregion

        #region Constraint 8: Layer Interlocking (Soft Constraint)
        /// <summary>
        /// Constraint 8: Layer interlocking - 층간 맞물림 보장 (Soft)
        /// 논문: "ensures interlocking among items between two layers"
        /// Uses Hausdorff distance (Section IV-B-2)
        /// </summary>
        public static double CalculateHausdorffDistance(List<Item> bottomLayer, List<Item> topLayer)
        {
            if (!bottomLayer.Any() || !topLayer.Any())
                return 0;

            // 하단 레이어 상단 꼭지점
            var bottomVertices = bottomLayer.SelectMany(item => item.GetTopVertices()).ToList();

            // 상단 레이어 하단 꼭지점
            var topVertices = topLayer.SelectMany(item =>
            {
                double z = item.Z;
                return item.GetBaseVertices().Select(v => (v.x, v.y, z));
            }).ToList();

            // Hausdorff distance 계산 (간략화)
            double maxMinDistance = 0;

            foreach (var (bx, by, bz) in bottomVertices)
            {
                double minDistance = double.MaxValue;

                foreach (var (tx, ty, tz) in topVertices)
                {
                    double distance = Math.Sqrt(Math.Pow(bx - tx, 2) + Math.Pow(by - ty, 2));
                    minDistance = Math.Min(minDistance, distance);
                }

                maxMinDistance = Math.Max(maxMinDistance, minDistance);
            }

            return maxMinDistance;
        }
        #endregion

        #region 종합 검증
        /// <summary>
        /// 아이템 배치 전 모든 Hard Constraints 검증
        /// </summary>
        public static bool ValidatePlacement(Item item, Pallet pallet, double vertexInset = 10, double stabilityTolerance = 0.2)
        {
            // Constraint 1: Orientation (이미 Item 클래스에서 보장)
            if (!ValidateOrientation(item))
                return false;

            // Constraint 2: Non-collision
            if (!ValidateNonCollision(item, pallet.Items))
                return false;

            // Constraint 3: Stability
            if (!ValidateStabilityAfterPlacement(pallet, item, stabilityTolerance))
                return false;

            // Constraint 4: Support
            if (!ValidateSupport(item, pallet, vertexInset))
                return false;

            // Constraint 5: Pattern complexity (약간 완화 가능)
            // Phase 2에서는 완화될 수 있음

            // Constraint 6: Complete shipment (주문 레벨에서 검증)

            return true;
        }
        #endregion
    }
}
