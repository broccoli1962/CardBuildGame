# CardBuildGame

플레이어가 입력한 콘셉트로 카드를 만들고, 그 덱으로 맵을 진행하는 Unity 2D 로그라이크 카드 빌더입니다.

전투·엘리트 노드를 클리어하면 로컬 LLM이 카드를 생성합니다. 모델이 없거나 추론이 실패해도 규칙 기반 Fallback으로 런이 이어집니다.

WebGL 플레이: [broccoli1962.github.io/CardBuildGame](https://broccoli1962.github.io/CardBuildGame/)

## 요구 사항

| 항목 | 버전 / 내용 |
|------|-------------|
| Unity | **6000.3.6f1** (Unity 6) |
| 렌더 파이프라인 | Universal RP 2D |
| 입력 | Unity Input System |
| 플랫폼 | Editor, Android, WebGL (GitHub Pages 빌드 포함) |

에디터에서 `Assets/Scenes/LobbyScene.unity`를 연 뒤 Play하면 로비 → 인게임 순으로 진입합니다. 빌드 씬 순서는 `LobbyScene` → `GameScene`입니다.

## 게임 루프

1. **로비** — 사운드 설정, 런 시작
2. **맵** — 층·슬롯 그래프에서 다음 노드 선택
3. **노드**
   - `Battle` / `Elite` / `Boss` — 턴제 전투
   - `Event` / `Rest` / `Treasure` — 선택형 콘텐츠
4. **카드 생성** — 전투가 있는 노드(보스 제외) 클리어 후, 플레이어 콘셉트로 후보 카드를 만들고 덱에 추가
5. **사망** — `DeathPanel`에서 런 종료

위협도(`ThreatScore`)가 몬스터 스케일링에 반영됩니다.

## 로컬 LLM

카드 생성은 프로세스 내부 LlamaSharp 추론을 사용합니다.

- 기본 가중치: `Qwen2.5-1.5B-Instruct-Q4_K_M.gguf`
- 배치 경로: `Assets/StreamingAssets/Models/`
- `.gguf`는 git에 포함하지 않습니다. 다운로드와 파일명은 [`Assets/StreamingAssets/Models/README.md`](Assets/StreamingAssets/Models/README.md)를 따릅니다.
- 모델이 없거나 응답이 비정상이면 `FallbackCardGenerator`가 규칙 기반 카드를 반환합니다.
- 에디터 스모크 테스트: `Tools/CardBuildGame/LLM/Run Smoke Test`

부트(`Boot`) 시 `LocalLlmManager`가 모델을 비동기로 로드하므로, 첫 생성 전에 로드가 끝날 수 있습니다.

## 아키텍처

책임을 계층으로 나눕니다.

| 계층 | 역할 | 예시 |
|------|------|------|
| **System** | 정적 게임 규칙·상태. 씬 오브젝트를 직접 만들지 않음 | `BattleSystem`, `MapSystem`, `DeckSystem`, `CardCreationSystem` |
| **Controller** | 화면에 존재하는 연출·스폰 | `CardController`, `MonsterController`, `BattleVfxController` |
| **UI (MVP)** | View는 입력·표시, Presenter는 로직. `TableManager`는 Presenter만 조회 | `GamePanel` / `GamePanelPresenter` |
| **SceneContext** | 부트 완료 후 해당 씬 UI·컨트롤러를 열고 System 이벤트를 구독 | `LobbySceneContext`, `GameSceneContext` |
| **Manager** | `SingletonGameObject<T>`, `DontDestroyOnLoad` | `GameManager`, `UIManager`, `ResourceManager`, `AudioManager` |

흐름 요약:

```
Boot → GameManager.InitializeCore (Audio / Table / LLM)
     → SceneContext.OnEnterAsync
     → GameManager.StartGameplay (System 초기화 + 1챕터 런)
```

앱 종료 중 Manager에 접근하기 전에 `GameStateUtil.IsQuitting`을 확인합니다. 종료 중 새 싱글톤을 만들지 않습니다.

UI는 `UIManager`로만 열고 닫습니다. Addressable 주소는 문자열 리터럴 대신 `AddressableKeys`를 사용합니다.

## 디렉터리

```
Assets/
  Scenes/                          LobbyScene, GameScene
  GameResource/
    Scripts/
      Object/
        Controller/                씬에 존재하는 연출 객체
        FX/                        이펙트 (자동 반환 등)
        GameSystems/
          Gameplay/                맵·전투·덱·이벤트·휴식·보물·카드 생성
          Llm/                     로컬 추론, 프롬프트, Fallback
        UI/                        Panel / Popup / Presenter
        Management/                GameManager, UIManager, Audio, Table, SceneContext
      Util/                        Singleton, AddressableKeys, Input, Extension
  GoogleSpreadSheetLoader/         GSSL 런타임 + Generated 테이블
  Resources/                       TableLinker, Localize_*.json (GSSL 생성물)
  StreamingAssets/Models/          GGUF 가중치 (로컬 전용)

.cursor/                           에이전트 규칙·스킬·GSSL 동기화 요청
```

## 테이블 데이터 (GSSL)

밸런스·몬스터·맵·로컬라이즈의 **단일 소스**는 Google Sheets입니다.

| 시트 | 용도 |
|------|------|
| `BaseCard`, `CardEffectType`, `CardPowerWeight` | 기본 카드·효과·CPS |
| `Monster`, `MonsterAction` | 적 스탯·행동 패턴 |
| `MapTemplate`, `MapNodeType`, `Stage` | 맵 그래프·노드 규칙 |
| `MapEvent`, `RestOption`, `TreasureOption` | 비전투 노드 |
| `ThreatScaling`, `EliteScaling`, `BalanceConstant` | 난이도·상수 |
| `GeneralGameStringLocalization` | UI 문자열 |

생성물(`Generated/**`, `Resources/Localize_*.json`, `Resources/TableLinker.asset`)은 손으로 고치지 않습니다. 시트 수정 → `Tools/GSSL/Sync Pending Sheets` (`mode: update`) 순서를 따릅니다.

GSSL 서비스 계정 JSON과 `SettingData.asset`은 저장소에 넣지 않습니다.

## 주요 패키지

| 패키지 | 용도 |
|--------|------|
| Cysharp UniTask | 비동기 |
| Cysharp R3 | 이벤트·반응형 상태 |
| Unity Addressables | 리소스 로드 |
| Unity Input System | 입력 |
| LitMotion | 짧은 연출 |
| LlamaSharp (NuGetForUnity) | 로컬 GGUF 추론 |

매니저·풀·리소스 사용 규칙과 C# 컨벤션은 `.cursor/rules/`를 참고합니다.

## 개발 메모

- 새 게임플레이는 System → SceneContext 구독 → UI/Controller 순으로 붙입니다.
- public/protected 메서드에는 XML `<summary>`를 둡니다. 로그에는 `[ClassName]` 접두어를 붙입니다.
- Unity `.meta`는 에셋과 항상 같이 커밋하고, 에이전트가 `.meta`를 새로 만들거나 고치지 않습니다.
- 커밋은 Conventional Commits (`feat`, `fix`, `docs` 등)를 사용합니다.
