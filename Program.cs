using System;
using MHAPalletizing.Tests;

namespace MHAPalletizing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║   MHA Palletizing Algorithm Implementation    ║");
            Console.WriteLine("║   Multi-Heuristic 3D Bin Packing Problem      ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                // === 테스트 옵션 선택 ===
                // 아래 주석을 해제하여 원하는 테스트를 실행하세요

                // Dataset10 병렬 처리 (권장) ⚡
                DatasetTests.RunDatasetTestsParallel(maxThreads: 4);

                // 다른 테스트 옵션들:
                // BasicTests.RunAllTests();                                          // 기본 테스트
                // Phase2Tests.RunAllTests();                                         // Phase 2 테스트
                // IntegrationTests.RunIntegrationTests();                            // 통합 테스트
                // DatasetTests.RunDatasetTests();                                    // Dataset10 순차 처리
                // DatasetTests.RunDatasetTestsInBatches(batchSize: 10, maxThreads: 4); // 배치 처리
                // DatasetTests.RunSingleOrderTest("16129");                          // 특정 주문 테스트
                // DatasetTests.PrintDatasetStatistics();                             // 데이터셋 통계
                // DebugTests.TestSingleItemPlacement();                              // 디버그 테스트

                Console.WriteLine("\n프로그램이 정상적으로 완료되었습니다.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n오류 발생: {ex.Message}");
                Console.WriteLine($"스택 트레이스:\n{ex.StackTrace}");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
