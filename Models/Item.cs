using System;

namespace MHAPalletizing.Models
{
    /// <summary>
    /// 팔레트에 배치될 박스 아이템
    /// 논문 Section IV-A: Item representation [li, wi, hi, mi, vi, qi]
    /// </summary>
    public class Item
    {
        // 아이템 식별자
        public string ProductId { get; set; }
        public int ItemId { get; set; }

        // 기본 속성 (논문 Section IV-A)
        public double Length { get; set; }   // li
        public double Width { get; set; }    // wi
        public double Height { get; set; }   // hi
        public double Weight { get; set; }   // mi
        public double Volume { get; set; }   // vi

        // 배치 정보 (논문 Figure 4)
        public double X { get; set; }        // xi
        public double Y { get; set; }        // yi
        public double Z { get; set; }        // zi
        public double RotationZ { get; set; } // Z축 회전각 (0° or 90°)

        // 현재 방향 (Constraint 1: Orthogonal rotation along Z-axis)
        public bool IsRotated { get; set; }

        // 현재 차원 (회전 고려)
        public double CurrentLength => IsRotated ? Width : Length;
        public double CurrentWidth => IsRotated ? Length : Width;
        public double CurrentHeight => Height; // 높이는 변하지 않음

        // 생성자
        public Item(string productId, int itemId, double length, double width, double height, double weight)
        {
            ProductId = productId;
            ItemId = itemId;
            Length = length;
            Width = width;
            Height = height;
            Weight = weight;
            Volume = length * width * height;

            // 초기 위치 및 회전
            X = 0;
            Y = 0;
            Z = 0;
            RotationZ = 0;
            IsRotated = false;
        }

        // 복사 생성자
        public Item Clone()
        {
            return new Item(ProductId, ItemId, Length, Width, Height, Weight)
            {
                X = this.X,
                Y = this.Y,
                Z = this.Z,
                RotationZ = this.RotationZ,
                IsRotated = this.IsRotated
            };
        }

        // 아이템을 특정 위치에 배치 (회전 포함)
        public void Place(double x, double y, double z, bool rotated = false)
        {
            X = x;
            Y = y;
            Z = z;
            IsRotated = rotated;
            RotationZ = rotated ? 90 : 0;
        }

        // 바운딩 박스 좌표 (충돌 검사용)
        public double MinX => X;
        public double MaxX => X + CurrentLength;
        public double MinY => Y;
        public double MaxY => Y + CurrentWidth;
        public double MinZ => Z;
        public double MaxZ => Z + CurrentHeight;

        // 무게 중심 좌표
        public (double x, double y, double z) GetCenterOfMass()
        {
            return (X + CurrentLength / 2, Y + CurrentWidth / 2, Z + CurrentHeight / 2);
        }

        // 베이스 4개 꼭지점 (Support constraint 용)
        public (double x, double y)[] GetBaseVertices()
        {
            return new[]
            {
                (X, Y),                           // 좌하단
                (X + CurrentLength, Y),           // 우하단
                (X + CurrentLength, Y + CurrentWidth), // 우상단
                (X, Y + CurrentWidth)             // 좌상단
            };
        }

        // 상단 4개 꼭지점 (Interlocking용)
        public (double x, double y, double z)[] GetTopVertices()
        {
            double topZ = Z + CurrentHeight;
            return new[]
            {
                (X, Y, topZ),
                (X + CurrentLength, Y, topZ),
                (X + CurrentLength, Y + CurrentWidth, topZ),
                (X, Y + CurrentWidth, topZ)
            };
        }

        public override string ToString()
        {
            return $"Item {ItemId} ({ProductId}): [{CurrentLength:F0}x{CurrentWidth:F0}x{CurrentHeight:F0}] " +
                   $"at ({X:F0}, {Y:F0}, {Z:F0}), Weight: {Weight:F1}kg, Rotated: {IsRotated}";
        }
    }
}
