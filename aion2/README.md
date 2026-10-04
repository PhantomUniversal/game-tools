# AION2 Tools

아이온2 파티 · 본부(본캐/부캐 로테이션) 조합 대시보드. C# / .NET 10 / Avalonia 12.

## 실행

```
dotnet run --project src/Aion2Tools
```

1. **캐릭터 명단**: 직접 추가하거나 표/CSV를 붙여넣는다. 번호(1~100)가 같은 캐릭터는 한 사람의 본캐·부캐로 본다.
   형식: `번호, 캐릭터, 클래스, 전투력, 아이템 레벨, 본캐(Y/N)`
2. **파티 조합**: 프리셋(성역 10인 / 원정 5인 등)을 고르고 `자동 조합`. 대안 3개, 텍스트 복사.
3. **본부 조합**: 회차 수를 정하고 `로테이션 생성`. 한 회차에 번호당 캐릭터 1개, 캐릭터는 전체 회차 중 1번.

## 설치와 업데이트

| 방법 | 대상 | 앱 업데이트 | 게임 데이터 업데이트 |
|---|---|---|---|
| `setup\windows\install.bat` | 체크아웃이 있는 PC | 시작 시 새 커밋 확인 → 설정에서 pull · 빌드 · 교체 | ✅ |
| `setup\windows\build-installer.bat` → `Aion2ToolsSetup.exe` | 체크아웃 없는 사람 | 새 설치 파일로 덮어쓰기 | ✅ |

**게임 데이터**(`data/gamedata.json`): 클래스 역할, 프리셋(인원 · 아이템 레벨 컷), 조합 가중치.
`Version`을 올려 push하면 앱이 시작할 때(또는 설정 > 지금 확인) 받아서 바로 적용한다. 앱 재빌드는 필요 없다.

## 조합 규칙 (`Weights`)

| 키 | 의미 |
|---|---|
| `MissingTank` / `MissingHealer` | 파티에 탱 / 힐이 없을 때 감점 |
| `SupportInParty` | 파티에 서포터(호법성)가 있을 때 가점 |
| `BuffPriority` | 호법 파티에 들어간 딜러의 `BuffPriority` × 이 값 (살성 3, 권성 · 마도성 2, 궁성 · 정령성 1) |
| `ExtraSameRole` | 한 파티에 같은 역할(탱 · 힐 · 서폿) 2명 이상일 때 1명당 감점 |
| `RaidDebuffDuplicate` | 공대 디버프 클래스(수호 · 검성)가 포스에 겹칠 때 감점 |
| `PreferredParty` | 클래스의 권장 파티(수호성 → 1파티)에 들어갔을 때 가점 |
| `DuplicatePlayer` | 같은 번호의 캐릭터가 한 번에 2개 이상일 때 감점 |
| `BalancePercent` | 파티 간 전투력 차이 1%당 감점 |
| `CombatPowerPer1000` | 출전자 전투력 합 1,000당 가점 (정원 초과 시 강한 캐릭터 우선) |

## 테스트

```
dotnet test Aion2Tools.slnx
```
