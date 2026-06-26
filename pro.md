# 김밥 게임 프로젝트 구조 (초기 설계)

## 프로젝트 목표

Good Pizza Great Pizza처럼 손님의 주문을 받고,
드래그 앤 드롭으로 김밥을 제작한 뒤 주문과 일치하는지 판정하는 게임을 제작한다.

현재 MVP(Minimum Viable Product) 구현 범위는 다음과 같다.

- 주문 생성
- 엑셀 데이터 읽기
- 재료 드래그 앤 드롭
- 김밥 제작
- 정답 판정

---

# 개발 순서

## 1. 공통 데이터 클래스 생성

주문 씬과 조리 씬이 모두 사용할 데이터 구조를 먼저 만든다.

### IngredientType.cs

모든 재료의 종류를 관리하는 Enum

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

앞으로는 문자열 대신 Enum을 사용한다.

예시

```csharp
IngredientType.Ham
IngredientType.Rice
```

---

## 2. OrderData.cs

주문 하나를 저장하는 클래스

```csharp
using System.Collections.Generic;

[System.Serializable]
public class OrderData
{
    public int orderId;

    public string orderName;

    public string customerDialogue;

    public List<IngredientType> ingredients;
}
```

예시

```text
ID : 1

이름 : 참치김밥

손님 :
"참치김밥 하나 주세요."

재료 :
- Seaweed
- Rice
- Tuna
```

---

## 3. PlayerKimbap.cs

플레이어가 현재 만든 김밥 정보를 저장

```csharp
using System.Collections.Generic;

public class PlayerKimbap
{
    public List<IngredientType> ingredients = new();
}
```

재료를 놓을 때마다

```csharp
playerKimbap.ingredients.Add(IngredientType.Ham);
```

처럼 추가된다.

---

## 4. (선택) CurrentOrder.cs

현재 주문을 저장

```csharp
public class CurrentOrder
{
    public OrderData order;

    public bool isCompleted;
}
```

나중에 주문 상태 관리가 쉬워진다.

---

# 데이터 흐름

```text
Orders.xlsx

↓

DataManager

↓

OrderData

↓

OrderManager

↓

현재 주문(CurrentOrder)

↓

조리 씬

↓

PlayerKimbap

↓

RecipeChecker

↓

성공 / 실패
```

---

# 추천 폴더 구조

```text
Assets
└── Scripts
    ├── Data
    │   ├── IngredientType.cs
    │   ├── OrderData.cs
    │   ├── PlayerKimbap.cs
    │   └── CurrentOrder.cs
    │
    ├── Managers
    │   ├── DataManager.cs
    │   ├── OrderManager.cs
    │   ├── GameManager.cs
    │   └── UIManager.cs
    │
    ├── Gameplay
    │   ├── DragItem.cs
    │   ├── Ingredient.cs
    │   ├── RecipeChecker.cs
    │   └── Kimbap.cs
    │
    └── UI
        ├── OrderUI.cs
        └── InventoryUI.cs
```

---

# 각 클래스 역할

## DataManager

- 엑셀 파일 읽기
- OrderData 생성
- 주문 목록 관리

---

## OrderManager

- 현재 주문 관리
- 다음 주문 생성

---

## GameManager

게임의 전체 흐름 제어

```text
게임 시작

↓

주문 생성

↓

플레이어 제작

↓

완성 버튼

↓

채점

↓

다음 주문
```

---

## Ingredient

재료 하나를 의미

```csharp
public class Ingredient
{
    public IngredientType type;
}
```

---

## DragItem

드래그 앤 드롭 담당

- OnBeginDrag()
- OnDrag()
- OnEndDrag()

---

## PlayerKimbap

플레이어가 만든 김밥의 재료를 저장

```text
Seaweed
↓

Rice
↓

Ham
↓

Egg
```

↓

```text
List<IngredientType>
```

---

## RecipeChecker

주문과 플레이어가 만든 김밥을 비교

예시

주문

```text
Seaweed
Rice
Ham
Egg
```

플레이어

```text
Seaweed
Rice
Ham
Egg
```

→ 성공

플레이어

```text
Seaweed
Ham
Rice
Egg
```

→ 실패

---

# MVP 구현 우선순위

1. IngredientType 정의
2. OrderData 작성
3. PlayerKimbap 작성
4. 엑셀(Order.xlsx) 읽기
5. 주문 생성
6. 드래그 앤 드롭
7. 현재 김밥 저장
8. 완성 버튼
9. RecipeChecker로 비교
10. 성공/실패 UI 출력

---

# 팀원 역할 분담 예시

### 주문 시스템

- OrderData
- DataManager
- OrderManager

---

### 조리 시스템

- DragItem
- Ingredient
- PlayerKimbap

---

### 판정 시스템

- RecipeChecker
- GameManager

---

### UI

- 주문 UI
- 재료 UI
- 결과 UI

---

## 핵심 원칙

먼저 **공통 데이터 구조(Data)** 를 확정한 뒤 각 기능을 개발한다.

모든 시스템은 동일한 `IngredientType`, `OrderData`, `PlayerKimbap`을 사용하여 데이터를 주고받는다.

이 구조를 기준으로 개발하면 Git 충돌이 적고, 팀원 간 협업이 수월해진다.
