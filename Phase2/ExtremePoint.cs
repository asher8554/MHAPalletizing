using System;
using MHAPalletizing.Models;

namespace MHAPalletizing.Phase2
{
    /// <summary>
    /// Extreme Point - 아이템 배치 가능 위치
    /// 논문 Section IV-B-3: "modified definition of Extreme Points"
    /// 논문 Figure 11 참조
    /// </summary>
    public class ExtremePoint : IComparable<ExtremePoint>
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        // EP가 사용되었는지 여부
        public bool IsUsed { get; set; }

        // EP의 우선순위 (낮을수록 우선)
        public double Priority { get; private set; }

        public ExtremePoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
            IsUsed = false;
            UpdatePriority();
        }

        /// <summary>
        /// EP 우선순위 계산
        /// 논문: "ranked based on height, lower EPs score higher"
        /// 같은 높이면 원점에 가까운 것 우선
        /// </summary>
        public void UpdatePriority()
        {
            // Z 좌표가 낮을수록 우선 (바닥에 가까울수록)
            // 같은 높이면 원점에 가까운 것 우선
            Priority = Z * 1000 + Math.Sqrt(X * X + Y * Y);
        }

        public int CompareTo(ExtremePoint other)
        {
            if (other == null) return 1;
            return Priority.CompareTo(other.Priority);
        }

        /// <summary>
        /// 두 EP가 같은 위치인지 확인 (중복 제거용)
        /// </summary>
        public bool IsSamePosition(ExtremePoint other, double epsilon = 0.1)
        {
            return Math.Abs(X - other.X) < epsilon &&
                   Math.Abs(Y - other.Y) < epsilon &&
                   Math.Abs(Z - other.Z) < epsilon;
        }

        /// <summary>
        /// 아이템을 이 EP에 배치했을 때 생성되는 3개의 새 EP
        /// 논문: "three new EPs are created: [li+xi, yi, zi], [xi, wi+yi, zi], [xi, yi, hi+zi]"
        /// 논문 Figure 11 참조
        /// </summary>
        public ExtremePoint[] GenerateNewPoints(Item item)
        {
            return new[]
            {
                new ExtremePoint(X + item.CurrentLength, Y, Z),  // X 방향
                new ExtremePoint(X, Y + item.CurrentWidth, Z),   // Y 방향
                new ExtremePoint(X, Y, Z + item.CurrentHeight)   // Z 방향
            };
        }

        public override string ToString()
        {
            return $"EP({X:F0}, {Y:F0}, {Z:F0}) Priority: {Priority:F2} {(IsUsed ? "[Used]" : "")}";
        }

        public ExtremePoint Clone()
        {
            return new ExtremePoint(X, Y, Z)
            {
                IsUsed = this.IsUsed,
                Priority = this.Priority
            };
        }
    }
}
