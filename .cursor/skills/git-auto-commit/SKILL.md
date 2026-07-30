---
name: git-auto-commit
description: Unity 프로젝트에서 하나의 작업을 완료할 때마다 변경 사항을 Conventional Commits 컨벤션으로 커밋하고 Pull Request 생성 → 머지 → 브랜치 삭제까지 진행한다. 사용자가 `PR` 이라는 단어를 프롬프트에서 사용하였거나, 자율 개발에서 하나의 작업 단위가 끝났을 때 사용한다. Use when a task is completed and changes should be committed, a PR opened, merged, and the branch cleaned up.
---

# Git Auto Commit & PR (작업 완료 시 자동 커밋 + PR + 머지)

**하나의 작업(task)을 완료할 때마다** 변경 사항을 논리적 단위로 커밋하고 Pull Request를 생성한 뒤,
**머지하고 작업 브랜치를 정리**한다. 커밋/PR 본문은 "무엇을(what)"보다 "왜(why)"에 초점을 두되,
**요약을 필수로 넣고 항목을 나눠 간결하게** 작성한다.

> 발동 조건: 사용자가 커밋/PR/머지를 명시적으로 요청했거나, 자율 개발 모드에서 한 작업 단위가 끝났을 때만.

## 안전 규칙 (반드시 준수)

- `git config`를 절대 수정하지 않는다.
- `push --force`, `hard reset` 등 파괴적/되돌릴 수 없는 명령은 사용자가 명시적으로 요청할 때만 실행한다.
- `--no-verify` 등으로 훅을 건너뛰지 않는다.
- **`main`/`master`에 직접 커밋·푸시하지 않는다.** 반드시 작업용 브랜치를 만들어 그 위에서 작업한다.
- **머지는 반드시 PR을 통해서** 한다(`gh pr merge`). 로컬에서 `main`으로 직접 병합해 푸시하지 않는다.
- **브랜치 삭제는 PR이 실제로 머지된 뒤에만** 한다. 머지되지 않은 브랜치는 삭제하지 않는다.
- `.env`, credentials, 키 파일(예: GSSL 서비스 계정 JSON) 등 비밀정보 파일은 커밋하지 않는다. 발견 시 사용자에게 경고한다.
- **`.meta` 파일은 짝이 되는 에셋과 항상 함께 커밋**한다. (Unity 필수) `.cs`만 커밋하고 `.cs.meta`를 빼면 안 된다.
- GSSL 생성 산출물(`Generated/**`, `Resources/TableLinker.asset`, `Resources/Localize_*.json`)은 손편집하지 않지만, **재생성된 결과는 정상적으로 커밋**한다(짝 `.meta` 포함).

## 워크플로우

작업 단위가 끝나면 먼저 다음을 **병렬로** 실행해 상태를 파악한다.

- `git status` — 추적/미추적 변경 전체
- `git diff` + `git diff --staged` — 실제 변경 내용
- `git log --oneline -20` — 기존 커밋 메시지 스타일 확인
- `git branch --show-current` — 현재 브랜치 확인

그다음:

1. **브랜치 준비**: 현재 브랜치가 `main`/`master`면 작업용 브랜치를 새로 만든다.
   - 이름 규칙: `<type>/<간단한-요약>` (예: `feat/monster-skill`, `fix/gauge-overflow`)
2. **변경 분석 및 스테이징**: 이번 작업과 관련된 파일만 `git add`. Unity 에셋은 짝 `.meta`도 함께 추가한다.
3. **커밋**: 메시지는 HEREDOC으로 전달한다 (아래 형식 참고).
4. **푸시**: `git push -u origin HEAD`.
5. **PR 생성**: `gh pr create`. 본문은 HEREDOC + 아래 템플릿.
6. **머지**: `gh pr merge <PR> --squash --delete-branch`.
7. **로컬 정리**: `main` 체크아웃 → `git pull` → 로컬 작업 브랜치 삭제.
8. **보고**: 머지된 PR URL과 정리 결과를 사용자에게 알린다.

## 커밋 메시지 형식 (Conventional Commits)

```
<type>(<scope>): <subject>

<body>
```

- **type** (필수): 아래 표에서 선택
- **scope** (선택): 변경 범위. 이 프로젝트 모듈명 사용 예: `ui`, `pool`, `audio`, `table`, `addressable`, `boot`, `scene`, `system`, `controller`, `input`, `resource`
- **subject** (필수): 명령문, 소문자 시작, 50자 이내, 마침표 없음
- **body** (선택): "왜" 변경했는지. 한 줄 비우고 `-` 불릿

| type | 용도 |
|------|------|
| `feat` | 새 기능 추가 |
| `fix` | 버그 수정 |
| `refactor` | 기능 변화 없는 구조 개선 |
| `perf` | 성능 개선 |
| `style` | 포맷 등 동작에 영향 없는 변경 |
| `docs` | 문서/주석 |
| `test` | 테스트 추가/수정 |
| `chore` | 빌드/설정/에셋 정리 등 기타 |
| `build` | 빌드/패키지 의존성 변경 |
| `ci` | CI 설정 변경 |

### 커밋 명령 예시

```bash
git commit -m "$(cat <<'EOF'
feat(system): add monster skill turn resolution

- 몬스터 스킬 사용 조건을 TurnSystem 흐름에 통합
- 스킬 발동/연출을 System-Controller 콜백으로 분리
EOF
)"
```

## Pull Request 본문 형식 (요약 필수 · 항목 분리)

- **요약(Summary)은 필수**다. 1~2문장으로 이 작업이 무엇을 왜 했는지 한눈에 파악되게 쓴다.
- 그 아래는 항목을 나눠 **불릿으로 간결하게**. 장황한 문단 금지.
- 변경이 없는 섹션은 생략 가능하되 요약은 절대 생략하지 않는다.
- 특정 고유명사를 제외하고 한글로 작성한다.

```bash
gh pr create --title "feat(system): add monster skill turn resolution" --body "$(cat <<'EOF'
## 요약
몬스터가 턴 중 스킬을 사용할 수 있도록 TurnSystem 흐름에 스킬 판정을 통합함.

## 변경 사항
- `MonsterAttackSystem`에 스킬 발동 조건 판정 추가
- 발동/연출을 `MonsterAttackVfxSystem` 콜백으로 분리
- `TableManager.Skill` 조회 메서드 연동

## 영향 범위
- system / controller / table

EOF
)"
```

### PR 제목
- 대표 커밋과 동일하게 `<type>(<scope>): <subject>` 형식으로 작성한다.

## 머지 및 브랜치 정리

1. **PR 머지 + 원격 브랜치 삭제**: `--squash` 로 히스토리를 깔끔하게, `--delete-branch` 로 원격 브랜치 제거.

```bash
gh pr merge <PR번호 또는 브랜치명> --squash --delete-branch
```

2. **로컬 정리**: 기본 브랜치로 돌아가 최신 상태를 받고, 로컬 작업 브랜치를 삭제한다.

```bash
git checkout main
git pull origin main
git branch -d <작업-브랜치명>
```

> `git branch -d` 는 머지되지 않은 브랜치면 실패한다(안전장치). 강제 삭제(`-D`)로 우회하지 말고, 머지가 정상 완료됐는지 먼저 확인한다.

### 머지가 막힐 때

- **머지 충돌**: 강제로 밀어붙이지 말고, 충돌 파일을 보고한 뒤 사용자에게 해결 방향을 확인한다.
- **필수 리뷰/CI 정책**: 브랜치 보호 규칙으로 머지가 거부되면, PR은 생성된 상태로 두고 URL과 사유를 보고한다. 정책을 우회하지 않는다.
- **CI 대기가 필요할 때**: `gh pr merge --squash --delete-branch --auto` 로 자동 머지를 예약할 수 있다(정책이 허용할 때만).

## 여러 단위로 나눠 커밋할 때

한 작업에 성격이 다른 변경이 섞였으면 커밋을 분리한다(PR은 작업 단위로 하나). 파일이 많으면 `TodoWrite`로 커밋 계획을 만들고 순서대로 진행한다.

```
- [ ] chore(assets): add UI border textures and fonts
- [ ] feat(ui): add confirm popup with MVP presenter
```

## 자율성 원칙

- 브랜치명·type·scope·메시지·PR 요약 문구는 diff를 근거로 합리적으로 직접 정한다.
- 다음의 경우에만 `AskQuestion`으로 확인한다:
  - 비밀정보 의심 파일을 커밋해야 하는지 불확실할 때
  - `--force`/`reset` 등 파괴적 동작, 또는 `main`/`master` 대상 작업 여부
  - PR의 base 브랜치가 애매할 때
  - 머지 충돌이 발생했거나, 브랜치 보호 정책으로 머지가 거부될 때
- 그 외에는 멈추지 말고 커밋 → 푸시 → PR 생성 → 머지 → 브랜치 정리까지 완료한 뒤, 머지된 PR URL과 정리 결과를 보고한다.
