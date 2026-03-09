# Tips：EF Core Code First とエンティティ設計

> **対象フェーズ：** Phase 2（DB設計書の生成）→ Phase 4（実装）
> **難易度：** ★☆☆（C# の基礎知識があれば読めます）

---

## このドキュメントについて

ワークショップの Phase 2 では、DB設計書を生成すると同時に
**EF Core のエンティティクラス**も出来上がります。

「なぜ設計フェーズでコードが書けるの？」
「これは AI だから特別にできること？」

その答えがこのドキュメントです。

---

## エンティティクラスとは何か

EF Core のエンティティクラスは、**DBのテーブル構造をそのまま C# で表現したもの**です。
1対1の対応関係があるため、テーブル定義が決まれば機械的に変換できます。

```
テーブル定義                      エンティティクラス
────────────────────────          ────────────────────────────────
テーブル名: todos          →      public class Todo
カラム: id INT PK          →          public int Id { get; set; }
カラム: title VARCHAR(255) →          public string Title { get; set; }
カラム: assignee_id FK     →          public int? AssigneeId { get; set; }
リレーション（FK）         →          public User? Assignee { get; set; }
```

この「機械的な変換」ゆえに、DB設計と同時にエンティティも完成します。

---

## EF Core の2つのアプローチ

EF Core には元々2つのアプローチがあり、どちらも「DB構造とエンティティはセット」という前提です。

### Code First（コードファースト）

**C# のクラスを先に書いて、DB を自動生成する**方法です。
今回のワークショップはこのアプローチを採用しています。

```
エンティティクラスを書く
        ↓
dotnet ef migrations add InitialCreate
        ↓
SQL（CREATE TABLE文）が自動生成される
        ↓
dotnet ef database update
        ↓
実際の MySQL テーブルが作られる
```

### Database First（DBファースト）

**DB を先に作って、エンティティクラスを自動生成する**方法です。
既存DBにあとから EF Core を導入するケースで使います。

```
SQL でテーブルを先に作る
        ↓
dotnet ef dbcontext scaffold
        ↓
エンティティクラスが自動生成される
```

### どちらのアプローチでも「DB設計とエンティティは常にセット」

```
Code First    → クラスを書く → DB が生成される（クラスが先）
Database First → DB を作る   → クラスが生成される（DB が先）

どちらも「DB構造とエンティティクラスは常にセット」という前提
```

---

## これは AI 以前からある伝統的なプラクティス

**「DB設計と同時にエンティティを書く」は AI 登場以前からの定番手法です。**

EF Core（および前身の Entity Framework）が2008年頃から採用している設計思想であり、
バイブコーディングが新しく発明したものではありません。

---

## AI 駆動開発で何が変わったのか

変わったのは**やる人間のスキル要件と速度**です。

```
AI 以前
  DB設計書を書く          → 人間（設計者）
  エンティティクラスを書く  → 人間（開発者）
  ※ 設計者と開発者が別の場合、認識の齟齬が起きやすかった

AI 以後（今回のワークショップ）
  DB設計書を書く          → Claude
  エンティティクラスを書く  → Claude（設計書から機械的に変換）
  ※ 同じ文脈で一気通貫で生成されるため、認識の齟齬が起きない
```

| | AI 以前 | AI 駆動開発 |
|---|---|---|
| プラクティス自体 | ✅ 既に存在した | 同じ |
| 誰がやるか | 開発者が手で書く | Claude が生成 |
| スピード | 設計 → 実装に時間差が発生 | 設計と同時に完成 |
| 認識齟齬リスク | 設計者 ↔ 開発者間で発生しうる | 同一文脈で生成されるため低い |

---

## エンティティ・DTO・ビジネスロジックの役割分担

エンティティはあくまで「DB構造の写し」であり、すべてのクラスがこの早さで書けるわけではありません。

```
Phase 2 で完成するもの（判断不要・機械的変換）
  └─ エンティティクラス（DB テーブルの写し）

Phase 4 で書くもの（判断が必要）
  ├─ DTO（何をクライアントに返すか）
  ├─ Service クラス（どんなビジネスロジックを持つか）
  └─ Controller（どんな認可ルールを設けるか）
```

**なぜ DTO や Service は Phase 4 なのか？**

```csharp
// DTO はクライアントに返す情報を「選ぶ」判断が必要
// → PasswordHash は返さない、削除済みフラグは返さない、など
public class TodoResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    // DeletedAt は外部に返さない ← これは設計判断
}

// Service はビジネスルールを「決める」判断が必要
// → 削除できるのは作成者だけか？担当者も削除可能か？など
public class TodoService
{
    public async Task DeleteAsync(int id, int requestUserId)
    {
        // ← この認可ルールは人間が決める
    }
}
```

エンティティにはこうした「判断」が不要なため、設計フェーズで書いてしまえます。

---

## EF Core マイグレーションの流れ（Phase 4 での実装イメージ）

Phase 2 でエンティティが完成しているため、Phase 4 での DB 構築は一発で完了します。

```bash
# 1. マイグレーションファイルを生成（エンティティを元にSQLが自動生成される）
dotnet ef migrations add InitialCreate --project src/TodoApp.API

# 2. 生成されたSQLを確認（自動生成された CREATE TABLE 文）
dotnet ef migrations script

# 3. MySQL に適用
dotnet ef database update --project src/TodoApp.API
```

これが Phase 3 で「EF Core マイグレーション作成 = 1 Issue」と定義される理由です。
エンティティは既にあるので、`migrations add` コマンドを叩くだけで済みます。

---

## まとめ

```
なぜ DB 設計フェーズでエンティティクラスが書けるのか
        ↓
エンティティ ＝ テーブル定義の C# による機械的な写し
判断が不要なため、設計と同時に完成できる

これは AI 以前からある EF Core の設計思想
        ↓
AI 駆動開発が変えたのは「誰がやるか」と「速度」だけ

「いいプラクティスを、人間のスキルに依存せず再現できる」
これが Claude Code の本質的な価値
```

---

## 参考リンク

- [EF Core 公式：Code First アプローチ](https://learn.microsoft.com/ja-jp/ef/core/managing-schemas/migrations/)
- [EF Core 公式：Database First アプローチ](https://learn.microsoft.com/ja-jp/ef/core/managing-schemas/scaffolding/)
- [Pomelo.EntityFrameworkCore.MySql](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
