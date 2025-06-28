using StarcUp.Business.Game;
using StarcUp.Business.GameManager.Extensions;
using StarcUp.Business.Units.Types;
using static StarcUp.Business.GameManager.Extensions.ProductionConstants;

namespace StarcUp.Docs.Examples
{
    /// <summary>
    /// UnitCount 시스템 사용 예시
    /// </summary>
    public class UnitCountUsageExample
    {
        public void ExampleUsage()
        {
            // 플레이어 생성
            var player = new Player { PlayerIndex = 0 };

            // 1. 기본 사용법 - 완성된 넥서스 개수 조회
            int nexusCount = player.GetUnitCount(UnitType.ProtossNexus);
            Console.WriteLine($"완성된 넥서스: {nexusCount}개");

            // 2. 생산중 포함 - 생산중인 넥서스 포함
            int totalNexusCount = player.GetUnitCount(UnitType.ProtossNexus, true);
            Console.WriteLine($"넥서스 총 개수 (생산중 포함): {totalNexusCount}개");

            // 3. 다양한 유닛 조회
            int zealotCount = player.GetUnitCount(UnitType.ProtossZealot);
            int dragoonCount = player.GetUnitCount(UnitType.ProtossDragoon);
            int pylonCount = player.GetUnitCount(UnitType.ProtossPylon);

            Console.WriteLine($"질럿: {zealotCount}, 드라군: {dragoonCount}, 파일런: {pylonCount}");

            // 4. 모든 유닛 카운트 조회
            var allUnitCounts = player.GetAllUnitCounts();
            foreach (var unitCount in allUnitCounts)
            {
                Console.WriteLine($"{unitCount.UnitType}: {unitCount.CompletedCount}개");
            }

            // 5. 건물만 조회
            var buildings = player.GetUnitCountsByCategory(UnitCategory.Building);
            Console.WriteLine($"보유 건물 종류: {buildings.Count}개");

            // 6. 생산중인 유닛 정보까지 포함
            var allCountsWithProduction = player.GetAllUnitCounts(true);
            foreach (var unitCount in allCountsWithProduction)
            {
                if (unitCount.ProductionCount > 0)
                {
                    Console.WriteLine($"{unitCount.UnitType}: {unitCount.CompletedCount}개 완성, {unitCount.ProductionCount}개 생산중");
                }
            }
        }

        /// <summary>
        /// 프로토스 빌드 오더 체크 예시
        /// </summary>
        public void ProtossBuildOrderCheck(Player player)
        {
            // 기본 빌드 체크
            int nexusCount = player.GetUnitCount(UnitType.ProtossNexus);
            int pylonCount = player.GetUnitCount(UnitType.ProtossPylon);
            int gatewayCount = player.GetUnitCount(UnitType.ProtossGateway);

            if (nexusCount >= 1 && pylonCount >= 2 && gatewayCount >= 1)
            {
                Console.WriteLine("기본 빌드 완료!");
            }

            // 테크 트리 체크
            int cyberCoreCount = player.GetUnitCount(UnitType.ProtossCyberneticsCore);
            int citadelCount = player.GetUnitCount(UnitType.ProtossCitadelOfAdun);

            if (cyberCoreCount >= 1 && citadelCount >= 1)
            {
                Console.WriteLine("질럿 업그레이드 가능!");
            }

            // 생산중인 유닛까지 포함한 체크
            int totalZealots = player.GetUnitCount(UnitType.ProtossZealot, true);
            int totalDragoons = player.GetUnitCount(UnitType.ProtossDragoon, true);

            Console.WriteLine($"현재 지상군 규모: 질럿 {totalZealots}기 + 드라군 {totalDragoons}기");
        }

        /// <summary>
        /// 경제력 분석 예시
        /// </summary>
        public void EconomyAnalysis(Player player)
        {
            int nexusCount = player.GetUnitCount(UnitType.ProtossNexus);
            int probeCount = player.GetUnitCount(UnitType.ProtossProbe);
            int assimilatorCount = player.GetUnitCount(UnitType.ProtossAssimilator);

            // 이상적인 프로브 수 계산 (넥서스당 16개 + 가스당 3개)
            int idealProbeCount = (nexusCount * 16) + (assimilatorCount * 3);
            
            Console.WriteLine($"현재 프로브: {probeCount}기");
            Console.WriteLine($"이상적 프로브: {idealProbeCount}기");
            
            if (probeCount < idealProbeCount)
            {
                Console.WriteLine($"프로브 {idealProbeCount - probeCount}기 더 필요!");
            }
        }
    }
}