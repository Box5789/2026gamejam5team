# Kitchen Scene Shared Design

작성 브랜치: `hosung`  
담당 씬: `kitchen`  
목적: 주문을 받는 씬 담당자와 주방 씬 담당자가 공통 데이터 구조와 역할 경계를 먼저 맞춘다.

이 문서는 `main` 브랜치의 `pro.md`를 기준으로 정리한 협업용 설계 문서다. 현재 단계에서는 직접 구현보다 **공통 클래스 정의, 데이터 흐름, 각자 담당 범위 합의**를 우선한다.

---

## 1. 전체 MVP 목표

게임은 손님의 주문을 받고, 플레이어가 주방 씬에서 김밥을 만든 뒤, 주문과 제작 결과가 일치하는지 판정하는 구조다.

MVP 범위는 다음과 같다.

1. 주문 데이터 생성 또는 로드
2. 주문 씬에서 현재 주문 선택
3. 주방 씬으로 주문 데이터 전달
4. 주방 씬에서 재료 선택 및 김밥 제작
5. 완성된 김밥 데이터 저장
6. 주문 데이터와 제작 데이터 비교
7. 성공/실패 결과 반환

---

## 2. 씬 역할 분리

## Order Scene 담당

주문 씬은 손님과 주문 데이터를 관리한다.

담당 범위:

- xlsx 데이터에서 주문 목록 읽기
- 손님 대사 표시
- 현재 주문 선택
- `CurrentOrder` 생성
- `kitchen` 씬으로 현재 주문 전달

주문 씬이 주방 씬에 넘겨야 하는 정보:

- 주문 ID
- 주문 이름
- 손님 대사
- 필요한 재료 목록
- 필요하다면 제한 시간, 난이도, 보상 점수

## Kitchen Scene 담당

주방 씬은 플레이어가 김밥을 실제로 만드는 화면이다.

담당 범위:

- 현재 주문 표시
- 재료 버튼 또는 드래그 UI 배치
- 플레이어가 선택한 재료를 `PlayerKimbap`에 기록
- 김밥 제작 단계 관리
- 완성 버튼 처리
- `RecipeChecker`에 주문 데이터와 제작 데이터 전달
- 판정 결과를 UI 또는 다음 흐름으로 전달

---

## 3. 공통 데이터 클래스

공통 클래스는 주문 씬과 주방 씬이 모두 같은 기준으로 데이터를 주고받기 위해 먼저 합의해야 한다.

권장 위치:

```text
Assets/Scripts/Data
```

---

## 3.1 IngredientType

모든 재료 종류를 관리하는 enum이다. 문자열 비교 대신 enum을 사용한다.

```csharp
public enum IngredientType
{
    Seaweed,
    Rice,
    Ham,
    Egg,
    Carrot,
    Spinach,
    Tuna,
    CrabMeat,
    PickledRadish
}
```

합의할 점:

- xlsx의 재료명과 enum 이름을 어떻게 매핑할지 정해야 한다.
- 한글 재료명이 들어올 경우 변환 규칙이 필요하다.
- 새 재료를 추가할 때는 이 enum을 먼저 수정한다.

예시 매핑:

```text
김 / 김밥김 / seaweed -> Seaweed
밥 / rice -> Rice
햄 / ham -> Ham
계란 / 달걀 / egg -> Egg
당근 / carrot -> Carrot
시금치 / spinach -> Spinach
참치 / tuna -> Tuna
맛살 / crab / crabmeat -> CrabMeat
단무지 / pickledradish -> PickledRadish
```

---

## 3.2 OrderData

주문 하나를 저장하는 클래스다.

```csharp
[System.Serializable]
public class OrderData
{
    public int orderId;
    public string orderName;
    public string customerDialogue;
    public List<IngredientType> ingredients;
}
```

역할:

- xlsx 한 행 또는 주문 데이터 하나를 표현한다.
- 주문 씬에서 생성하고 주방 씬에서 읽는다.
- 주방 씬은 이 데이터를 수정하지 않는다.

예시:

```text
orderId: 1
orderName: 참치김밥
customerDialogue: 참치김밥 하나 주세요.
ingredients:
- Seaweed
- Rice
- Tuna
- PickledRadish
```

---

## 3.3 PlayerKimbap

플레이어가 현재 만든 김밥 정보를 저장하는 클래스다.

```csharp
public class PlayerKimbap
{
    public List<IngredientType> ingredients = new();
}
```

역할:

- 주방 씬에서만 주로 갱신된다.
- 플레이어가 재료를 올릴 때마다 `ingredients`에 추가한다.
- 완성 버튼을 누르면 `RecipeChecker`에 전달한다.

합의할 점:

- 재료 순서를 판정에 포함할지 정해야 한다.
- 중복 재료 허용 여부를 정해야 한다.
- 김과 밥을 자동 포함할지, 플레이어가 직접 선택하게 할지 정해야 한다.

---

## 3.4 CurrentOrder

현재 진행 중인 주문 상태를 저장하는 클래스다.

```csharp
public class CurrentOrder
{
    public OrderData order;
    public bool isCompleted;
}
```

역할:

- 주문 씬과 주방 씬 사이에서 현재 주문 상태를 공유한다.
- 완료 여부, 실패 여부, 진행 상태 확장에 사용한다.

추가 확장 후보:

```csharp
public float timeLimit;
public float remainingTime;
public int rewardScore;
public bool isFailed;
```

---

## 4. 매니저 클래스 역할 합의

권장 위치:

```text
Assets/Scripts/Managers
```

## 4.1 DataManager

담당: 주문 데이터 로드

역할:

- xlsx 또는 임시 테스트 데이터에서 주문 목록 생성
- `List<OrderData>` 관리
- 주문 ID로 주문 검색

주문 씬 담당자가 우선 구현하기 좋다.

## 4.2 OrderManager

담당: 현재 주문 관리

역할:

- 다음 주문 선택
- 현재 주문을 `CurrentOrder`로 보관
- 주방 씬으로 넘길 주문 제공

주문 씬 담당자와 주방 씬 담당자가 함께 인터페이스를 맞춰야 한다.

## 4.3 GameManager

담당: 전체 게임 흐름

역할:

- 주문 시작
- 주방 씬 진입
- 제작 완료 처리
- 결과 화면 또는 다음 주문으로 이동

초기 MVP에서는 과하게 구현하지 않고, 씬 간 연결이 필요해지는 시점에 작성한다.

## 4.4 UIManager

담당: 공통 UI 흐름

역할:

- 주문 표시
- 결과 표시
- 씬 전환 버튼 처리

씬별 UI가 다르면 `OrderUI`, `KitchenUI`처럼 나눠도 된다.

---

## 5. Kitchen Scene 구현 범위

권장 씬 이름:

```text
Assets/Scenes/kitchen.unity
```

권장 위치:

```text
Assets/Scripts/Kitchen
```

주방 씬에서 필요한 클래스 틀:

```text
KitchenController
KitchenIngredientSlot
KitchenDropArea
KitchenOrderView
KitchenResultView
```

## 5.1 KitchenController

주방 씬의 중심 컨트롤러다.

담당:

- 현재 주문 받기
- `PlayerKimbap` 초기화
- 재료 추가 요청 받기
- 완성 버튼 처리
- 판정 요청

주문 씬과 맞춰야 하는 입력:

```text
CurrentOrder currentOrder
```

주방 씬이 넘겨야 하는 출력:

```text
PlayerKimbap playerKimbap
bool isSuccess
```

## 5.2 KitchenIngredientSlot

재료 버튼 또는 드래그 가능한 재료 UI다.

담당:

- 자신이 어떤 `IngredientType`인지 보관
- 클릭 또는 드래그 시 `KitchenController`에 재료 추가 요청

## 5.3 KitchenDropArea

재료를 놓는 김밥 제작 영역이다.

담당:

- 드래그 앤 드롭 도착 위치 처리
- 시각적으로 재료가 쌓이는 연출 담당
- 실제 데이터 저장은 `PlayerKimbap` 또는 `KitchenController`가 담당

## 5.4 KitchenOrderView

현재 주문 정보를 표시한다.

담당:

- 주문 이름 표시
- 손님 대사 표시
- 필요한 재료 목록 표시

표시는 주방 씬에서 필요하지만, 데이터 원본은 주문 씬 또는 `OrderManager`다.

## 5.5 KitchenResultView

완성 후 성공/실패 결과를 표시한다.

담당:

- 성공/실패 표시
- 틀린 재료 표시 여부는 추후 결정
- 다음 주문 또는 주문 씬 복귀 버튼 표시

---

## 6. 판정 클래스

권장 위치:

```text
Assets/Scripts/Gameplay
```

## RecipeChecker

주문 데이터와 플레이어 제작 데이터를 비교한다.

기본 MVP 판정:

```text
OrderData.ingredients == PlayerKimbap.ingredients
```

합의할 판정 방식:

1. 순서까지 정확히 같아야 성공
2. 순서는 상관없고 재료 구성만 같으면 성공
3. 필수 재료만 맞으면 성공, 추가 재료는 감점
4. 김과 밥은 자동 기본 재료로 처리

현재 `pro.md` 기준으로는 **순서까지 비교하는 방식**이 예시로 적혀 있다.

---

## 7. xlsx 데이터 형식 합의

제공 데이터셋은 Google Drive의 xlsx 파일 형식이라고 전달받았다. 아직 코드 구현 전이므로, 주문 씬 담당자와 아래 컬럼명을 먼저 합의하는 것이 좋다.

권장 컬럼:

```text
orderId
orderName
customerDialogue
ingredients
```

예시:

| orderId | orderName | customerDialogue | ingredients |
|---:|---|---|---|
| 1 | 참치김밥 | 참치김밥 하나 주세요. | Seaweed,Rice,Tuna,PickledRadish |
| 2 | 햄계란김밥 | 햄이랑 계란 넣어주세요. | Seaweed,Rice,Ham,Egg |

한글 컬럼을 사용한다면 권장 매핑:

```text
주문ID -> orderId
주문명 -> orderName
대사 / 손님대사 -> customerDialogue
재료 -> ingredients
```

재료 칸 표기 방식 권장:

```text
Seaweed,Rice,Ham,Egg
```

또는 한글 사용 시:

```text
김,밥,햄,계란
```

단, 한글을 사용할 경우 `IngredientType` 변환 테이블이 반드시 필요하다.

---

## 8. 씬 간 데이터 전달 방식 후보

초기 MVP에서는 단순한 방식부터 사용한다.

## 후보 A: Static GameSession

장점:

- 구현이 가장 쉽다.
- 씬 전환 시 빠르게 데이터 전달 가능하다.

단점:

- 데이터 초기화 타이밍을 조심해야 한다.

예시 역할:

```text
GameSession.CurrentOrder
GameSession.LastPlayerKimbap
GameSession.LastResult
```

## 후보 B: DontDestroyOnLoad GameManager

장점:

- 전체 게임 흐름 관리에 좋다.

단점:

- 초기 설계가 조금 더 필요하다.

## 현재 추천

MVP에서는 `GameSession` 또는 간단한 `GameManager`로 시작하고, 구조가 커지면 매니저 방식으로 확장한다.

---

## 9. 팀원 간 인터페이스 약속

주문 씬 담당자가 보장할 것:

- `CurrentOrder.order`는 null이 아니어야 한다.
- `OrderData.ingredients`는 비어 있지 않아야 한다.
- 재료명은 `IngredientType`으로 변환된 상태로 주방 씬에 전달한다.

주방 씬 담당자가 보장할 것:

- 주방 씬 시작 시 현재 주문을 읽어 화면에 표시한다.
- 플레이어가 선택한 재료는 `PlayerKimbap.ingredients`에 저장한다.
- 완성 버튼을 누르면 `RecipeChecker`로 판정을 요청한다.
- 주문 데이터 자체는 수정하지 않는다.

판정 담당 또는 공통 담당자가 보장할 것:

- 비교 규칙을 하나로 고정한다.
- 성공/실패 결과 형식을 정한다.

---

## 10. 현재 단계에서 해야 할 합의 체크리스트

- [ ] `IngredientType` 최종 재료 목록 확정
- [ ] xlsx 컬럼명 확정
- [ ] xlsx 재료 표기 방식 확정
- [ ] 순서 판정 여부 확정
- [ ] 김/밥 자동 포함 여부 확정
- [ ] 주문 씬에서 주방 씬으로 데이터 전달 방식 확정
- [ ] 주방 씬 이름 `kitchen`으로 통일
- [ ] 성공/실패 결과를 어느 씬에서 보여줄지 확정

---

## 11. Hosung 담당 작업 범위 요약

내 담당은 `kitchen` 씬이다.

우선 담당할 작업:

1. `kitchen` 씬 구성안 작성
2. 주문 표시 UI 자리 만들기
3. 재료 선택 UI 자리 만들기
4. 김밥 제작 영역 만들기
5. `PlayerKimbap`에 재료가 쌓이는 흐름 설계
6. 완성 버튼 흐름 설계
7. `RecipeChecker` 호출 위치 정리

직접 구현 전 주문 씬 담당자와 먼저 맞출 것:

- `CurrentOrder`를 어떤 방식으로 받을지
- `OrderData.ingredients`가 어떤 형태로 들어오는지
- 제작 결과를 어디로 넘길지
- 성공/실패 화면을 주방 씬에서 보여줄지, 별도 결과 씬에서 보여줄지
