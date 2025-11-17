# Changelog

All notable changes to the MHA Palletizing project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2024-11-17

### Added
- **CSV 데이터 입출력 기능**
  - `CsvReader`: Dataset10.csv 형식의 주문 데이터 읽기
  - `ResultWriter`: 3가지 CSV 결과 파일 생성 (요약, 상세, 배치)
  - Summary Results: 주문별 팔레트 수, 활용률, 실행시간
  - Detailed Results: 팔레트별 상세 통계 (부피/높이 활용률, 이질성, 밀집도)
  - Item Placements: 각 아이템의 3D 좌표 및 회전 정보

- **병렬 처리 기능**
  - `ParallelProcessor`: 멀티스레드 주문 처리
  - CPU 코어 수 기반 자동 스레드 할당 (2-8 스레드)
  - 실시간 진행률 모니터링
  - 배치 처리 모드 (메모리 효율성)
  - 스레드 안전한 결과 수집

- **디버깅 도구**
  - `DebugTests`: 배치 실패 원인 진단 테스트
  - 단일 아이템, 실제 데이터셋, 복수 아이템 테스트

### Changed
- **Stability 제약 조건 개선**
  - 아이템 수에 따른 동적 tolerance 조정
  - 1-2개: 0.99 (거의 제약 없음)
  - 3-4개: 0.7 (완화된 제약)
  - 5-9개: 0.5 (중간 제약)
  - 10개 이상: 0.4 (엄격한 제약)
  - **결과**: 빈 팔레트에서 첫 아이템 배치 실패 문제 해결

- **GA 파라미터 최적화**
  - MAX_GENERATIONS: 20 → 30 (품질 개선)
  - MAX_STAGNATION: 5 → 8 (조기 종료 방지)
  - 주석 명확화 및 코드 정리

- **프로그램 구조 개선**
  - Program.cs: 테스트 옵션 간소화 및 명확화
  - 중복된 옵션 제거
  - 더 직관적인 주석 구조

### Fixed
- **Phase 2 GA 배치 실패 문제**
  - 원인: 첫 아이템 배치 시 과도한 Stability 제약
  - 해결: 동적 tolerance 적용으로 100% 배치 성공률 달성
  - 테스트 결과: Order 16129 (27 items) - 100% 배치, 47.09% 활용률

- **컴파일 경고 제거**
  - CS0219: 미사용 변수 `palletIdCounter` 제거
  - 미사용 메서드 `TryAddLayerToPallet` 제거
  - 디버깅 코드 정리

- **메모리 및 성능**
  - output.txt 임시 파일 제거
  - 불필요한 주석 코드 삭제

### Performance
- **병렬 처리 성능**
  - 4스레드: 약 4배 속도 향상
  - Dataset10 (10 orders): 0.34초 (평균 0.03초/주문)
  - Order 16129: 304ms (27 items, 1 pallet)

- **CSV 출력 최적화**
  - 대량 결과 파일 생성 (30+ 파일) 고속 처리
  - 스레드 안전한 파일 쓰기

### Documentation
- **코드 주석 개선**
  - PlacementStrategy: Stability tolerance 로직 상세 설명
  - MHAAlgorithm: Phase 1 비활성화 이유 명확화
  - GeneticAlgorithm: 파라미터 설명 추가

- **README.md 업데이트**
  - CSV 사용법 섹션 추가
  - 병렬 처리 가이드 추가
  - 예제 코드 업데이트

## [1.0.0] - 2024-11-15

### Added
- 초기 MHA 알고리즘 구현
- Phase 1: Layer & Block 기반 휴리스틱
- Phase 2: NSGA-II 유전 알고리즘
- 8가지 실제 제약조건 적용
- 기본 테스트 스위트
- Euro 팔레트 표준 지원

### Known Issues
- Phase 1 LayerBuilder: 메모리 최적화 필요 (OutOfMemoryException)
- 현재 모든 아이템이 Phase 2 GA로 처리됨

## Future Plans

### v1.2.0 (계획)
- [ ] Phase 1 LayerBuilder 메모리 최적화
- [ ] Item.Clone() 호출 최소화
- [ ] 더 효율적인 레이어 생성 알고리즘
- [ ] Phase 1 + Phase 2 하이브리드 처리 복원

### v1.3.0 (계획)
- [ ] GUI 인터페이스 추가
- [ ] 3D 시각화 도구
- [ ] 실시간 배치 시뮬레이션
- [ ] 다양한 팔레트 크기 지원

### v2.0.0 (계획)
- [ ] 기계 학습 기반 파라미터 자동 조정
- [ ] 클라우드 기반 대규모 처리
- [ ] REST API 서버
- [ ] Docker 컨테이너화
