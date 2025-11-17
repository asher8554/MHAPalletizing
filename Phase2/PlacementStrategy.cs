using System;
using System.Collections.Generic;
using System.Linq;
using MHAPalletizing.Models;
using MHAPalletizing.Constraints;

namespace MHAPalletizing.Phase2
{
    /// <summary>
    /// Extreme Points 기반 배치 전략
    /// 논문 Section IV-B-3: "placement strategy"
    /// </summary>
    /// <remarks>
    /// Extreme Points (EP)는 아이템을 배치할 수 있는 3D 공간의 후보 위치입니다.
    ///
    /// EP 생성 규칙:
    /// - 초기 EP: 팔레트 원점 (0, 0, 0)
    /// - 아이템 배치 시 3개의 새 EP 생성:
    ///   1. (x + length, y, z) - X 방향
    ///   2. (x, y + width, z) - Y 방향
    ///   3. (x, y, z + height) - Z 방향
    ///
    /// EP 우선순위:
    /// - Z 좌표가 낮을수록 우선 (바닥부터 채우기)
    /// - 같은 높이면 원점에 가까운 순서
    ///
    /// 제약조건 검증:
    /// - Non-collision: AABB 충돌 검사
    /// - Support: 지지 면적 및 꼭지점 검증
    /// - Stability: 무게 중심 검증
    /// - 팔레트 범위: 길이/너비/높이 초과 방지
    /// </remarks>
    public class PlacementStrategy
    {
        private List<ExtremePoint> extremePoints;
        private Pallet pallet;
        private const double EPSILON = 0.1;

        /// <summary>
        /// PlacementStrategy의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="pallet">아이템을 배치할 팔레트</param>
        /// <remarks>
        /// 생성 시 팔레트의 기존 아이템 상단 꼭지점을 EP로 초기화합니다.
        /// 팔레트가 비어있으면 원점 (0, 0, 0)을 EP로 사용합니다.
        /// </remarks>
        public PlacementStrategy(Pallet pallet)
        {
            this.pallet = pallet;
            this.extremePoints = new List<ExtremePoint>();
            InitializeExtremePoints();
        }

        /// <summary>
        /// 초기 EP 설정 (기존 Block의 상단 꼭지점)
        /// 논문: "EPs corresponding to vertices of top surfaces of items in all blocks"
        /// </summary>
        private void InitializeExtremePoints()
        {
            extremePoints.Clear();

            if (!pallet.Items.Any())
            {
                // 빈 팔레트: 원점에서 시작
                extremePoints.Add(new ExtremePoint(0, 0, 0));
            }
            else
            {
                // 기존 아이템들의 상단 꼭지점을 EP로 추가
                foreach (var item in pallet.Items)
                {
                    var topVertices = item.GetTopVertices();
                    foreach (var (x, y, z) in topVertices)
                    {
                        var ep = new ExtremePoint(x, y, z);
                        AddExtremePoint(ep);
                    }
                }
            }

            SortExtremePoints();
        }

        /// <summary>
        /// EP 추가 (중복 제거)
        /// </summary>
        private void AddExtremePoint(ExtremePoint newEP)
        {
            // 중복 체크
            if (extremePoints.Any(ep => ep.IsSamePosition(newEP)))
                return;

            // 팔레트 범위 체크
            if (newEP.X < 0 || newEP.X > pallet.Length ||
                newEP.Y < 0 || newEP.Y > pallet.Width ||
                newEP.Z < 0 || newEP.Z > pallet.MaxHeight)
                return;

            extremePoints.Add(newEP);
        }

        /// <summary>
        /// EP를 우선순위로 정렬
        /// </summary>
        private void SortExtremePoints()
        {
            extremePoints.Sort();
        }

        /// <summary>
        /// 아이템을 배치 가능한 Extreme Point에 배치합니다.
        /// 논문: "attempt to place item in highest scoring EP"
        /// </summary>
        /// <param name="item">배치할 아이템. 배치 성공 시 위치와 회전 정보가 업데이트됩니다.</param>
        /// <param name="allowRotation">Z축 회전 허용 여부. true인 경우 0°와 90° 회전을 모두 시도합니다.</param>
        /// <returns>아이템이 성공적으로 배치되면 true, 그렇지 않으면 false</returns>
        /// <remarks>
        /// 배치 프로세스:
        /// 1. 사용되지 않은 EP를 우선순위 순으로 탐색
        /// 2. 각 EP에서 회전 방향(0°, 90°)을 시도
        /// 3. 제약조건 검증 (충돌, 지지, 안정성, 팔레트 범위)
        /// 4. 배치 성공 시 해당 EP를 사용됨으로 표시하고 3개의 새 EP 생성
        /// 5. 실패 시 다음 EP로 이동
        ///
        /// 검증되는 제약조건:
        /// - 팔레트 범위 초과 방지
        /// - Non-collision (충돌 방지)
        /// - Support (지지 면적 및 꼭지점)
        /// - Stability (무게 중심)
        /// </remarks>
        /// <example>
        /// <code>
        /// var pallet = new Pallet(1);
        /// var strategy = new PlacementStrategy(pallet);
        /// var item = new Item("MILK", 1, 100, 80, 200, 1.0);
        ///
        /// if (strategy.TryPlaceItem(item, allowRotation: true))
        /// {
        ///     pallet.AddItem(item);
        ///     Console.WriteLine($"배치 성공: ({item.X}, {item.Y}, {item.Z}), 회전: {item.IsRotated}");
        /// }
        /// else
        ///     Console.WriteLine("배치 실패");
        /// </code>
        /// </example>
        public bool TryPlaceItem(Item item, bool allowRotation = true)
        {
            // 두 방향 시도
            var orientations = allowRotation ? new[] { false, true } : new[] { false };

            foreach (var ep in extremePoints.Where(e => !e.IsUsed).OrderBy(e => e.Priority))
            {
                foreach (bool rotated in orientations)
                {
                    item.IsRotated = rotated;

                    // EP에 아이템 임시 배치
                    item.Place(ep.X, ep.Y, ep.Z, rotated);

                    // 제약조건 검증
                    if (ValidatePlacement(item))
                    {
                        // 배치 성공
                        ep.IsUsed = true;

                        // 새로운 EP 생성 및 추가
                        var newEPs = ep.GenerateNewPoints(item);
                        foreach (var newEP in newEPs)
                        {
                            AddExtremePoint(newEP);
                        }

                        SortExtremePoints();
                        return true;
                    }
                }
            }

            return false; // 배치 실패
        }

        /// <summary>
        /// 배치 검증 (Hard Constraints)
        /// </summary>
        private bool ValidatePlacement(Item item)
        {
            // 1. 팔레트 범위 체크
            if (item.MaxX > pallet.Length + EPSILON ||
                item.MaxY > pallet.Width + EPSILON ||
                item.MaxZ > pallet.MaxHeight + EPSILON)
                return false;

            // 2. Non-collision
            if (!ConstraintValidator.ValidateNonCollision(item, pallet.Items))
                return false;

            // 3. Support (바닥이 아닌 경우)
            if (item.Z > EPSILON)
            {
                if (!ConstraintValidator.ValidateSupport(item, pallet))
                    return false;
            }

            // 4. Stability (전체 팔레트)
            // 아이템 수에 따라 stability tolerance를 동적으로 조정
            // 적은 아이템: 배치 유연성 우선 / 많은 아이템: 안정성 우선
            double stabilityTolerance = 0.99;       // 1-2개: 거의 제약 없음
            if (pallet.Items.Count >= 10)
                stabilityTolerance = 0.4;           // 10개 이상: 엄격한 제약
            else if (pallet.Items.Count >= 5)
                stabilityTolerance = 0.5;           // 5-9개: 중간 제약
            else if (pallet.Items.Count >= 3)
                stabilityTolerance = 0.7;           // 3-4개: 완화된 제약

            if (!ConstraintValidator.ValidateStabilityAfterPlacement(pallet, item, tolerance: stabilityTolerance))
                return false;

            return true;
        }

        /// <summary>
        /// 배치 가능한 EP 개수
        /// </summary>
        public int GetAvailableEPCount()
        {
            return extremePoints.Count(ep => !ep.IsUsed);
        }

        /// <summary>
        /// 모든 EP 정보
        /// </summary>
        public List<ExtremePoint> GetAllExtremePoints()
        {
            return new List<ExtremePoint>(extremePoints);
        }

        /// <summary>
        /// EP 초기화 (재시작용)
        /// </summary>
        public void Reset()
        {
            InitializeExtremePoints();
        }
    }
}
