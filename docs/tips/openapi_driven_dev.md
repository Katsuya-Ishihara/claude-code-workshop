# Tips：OpenAPI駆動開発と省力化のしくみ

> **対象フェーズ：** Phase 2（設計資料の生成）→ Phase 4（実装）
> **難易度：** ★★☆（APIの基礎知識があると読みやすい）

---

## このドキュメントについて

ワークショップの Phase 2 で Claude が生成した `api_design.md` には、
末尾に OpenAPI（Swagger）形式の YAML が含まれています。

「なぜわざわざ YAML で書くのか？」
「設計書を丁寧に書くと何が嬉しいのか？」

その答えがこのドキュメントです。

---

## そもそも REST API・OpenAPI・Swagger とは

### REST API

フロントエンドとバックエンドが**データをやり取りするための窓口**です。

```
Blazor（画面）  ←──── API ────→  ASP.NET Core（DB操作）

「Todoの一覧をください」  →  「はい、どうぞ 📋」
```

REST APIでは「操作の種類」をHTTPメソッドで、「対象」をURLで表現します。

```
GET    /todos      → Todo一覧を取得する
POST   /todos      → Todoを新規作成する
PUT    /todos/1    → ID=1のTodoを更新する
DELETE /todos/1    → ID=1のTodoを削除する
```

### OpenAPI

REST APIの**仕様書を書くためのフォーマット規格**です。
「このエンドポイントはどんなJSONを受け取って、何を返すか」を
誰が見ても分かるよう標準化したものです。

### Swagger

OpenAPI仕様書を**ブラウザで見やすく表示し、その場で実行できるツール**です。
ASP.NET Core に組み込むと、コードから自動でこんな画面が生成されます。

```
┌─────────────────────────────────────────┐
│  TodoApp API                            │
│                                         │
│  ▶ POST   /auth/login    ログイン       │
│  ▶ GET    /todos         一覧取得       │ ← クリックで展開・実行できる
│  ▶ POST   /todos         Todo作成       │
│  ▶ DELETE /todos/{id}    Todo削除       │
│                                         │
│  [Try it out] でブラウザから直接叩ける  │
└─────────────────────────────────────────┘
```

### 3つの関係まとめ

```
REST API  ＝ 今回作るAPIの「設計スタイル」
OpenAPI   ＝ 仕様書の「フォーマット規格」
Swagger   ＝ OpenAPI仕様書を「ブラウザで見るツール」

REST API を → OpenAPI形式で文書化 → Swagger で表示する
```

---

## OpenAPI 設計書があると何が省力化できるか

### ① クライアントコードの自動生成（最大のメリット）

OpenAPI YAML から **Blazor 側の API クライアントコードを自動生成**できます。
`Services/TodoApiClient.cs` などを**手書きゼロ**で作れます。

```bash
# NSwag CLI を使った自動生成の例
dotnet tool install -g NSwag.ConsoleCore
nswag openapi2csclient /input:api_design.yaml /output:TodoApiClient.cs
```

手書きとの違いを見てみましょう。

```csharp
// ❌ 手書きの場合（ミスが起きやすい・時間がかかる）
public async Task<TodoResponseDto?> GetTodoByIdAsync(int id)
{
    var response = await _httpClient.GetAsync($"/api/v1/todos/{id}");
    if (!response.IsSuccessStatusCode) return null;
    return await response.Content.ReadFromJsonAsync<TodoResponseDto>();
    // エラーハンドリング、デシリアライズ設定... 全部自分で書く
}
```

```csharp
// ✅ 自動生成の場合（叩くだけ）
var todo = await _todoClient.GetTodoByIdAsync(id);
```

DTOクラス（`TodoResponseDto` など）も同時に生成されるため、
**フロントエンドとバックエンドの型定義が自動で一致**します。

---

### ② バリデーションの自動化

設計書に書いたルールが、そのままASP.NET Coreのバリデーションに反映されます。

```yaml
# api_design.md に書いたこの定義が…
title:
  type: string
  maxLength: 255
  minLength: 1
progressRate:
  type: integer
  minimum: 0
  maximum: 100
```

```csharp
// ✅ [ApiController] 属性を付けるだけで自動バリデーション
// 255文字超・0未満などは自動で 400 Bad Request を返してくれる
// バリデーションコードを一切書かなくていい
[ApiController]
[Route("api/v1/todos")]
public class TodosController : ControllerBase { ... }
```

---

### ③ テストコードの期待値が明確になる

APIの仕様が設計書に明記されているので、テストの「正解」がブレません。

```csharp
// ✅ api_design.md に「POST /todos は 201 を返す」と明記されているので
// テストの期待値を迷わず書ける

[Fact]
public async Task CreateTodo_Returns201Created()
{
    var response = await _client.PostAsJsonAsync("/api/v1/todos", new
    {
        title = "テストTodo",
        priority = "high"
    });

    // 設計書通りかを検証するだけ
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

さらに **Spectral** などの Linter を CI に組み込むと、
「実装が OpenAPI 仕様から外れていないか」を自動チェックできます。

---

### ④ フロントエンドとバックエンドの並行開発が可能になる

設計書が先に確定しているため、実装を待たずに開発を進められます。

```
通常の開発（設計書なし）
  バックエンド実装 ──────────────────→ 完成
                                        ↓
                         フロントエンド開発スタート（待ち時間が発生）


OpenAPI 駆動開発
  Phase 2 で API 仕様を確定
       ↓
  バックエンド実装  ──────────────────→ 完成
  フロントエンド開発（モック使用）────→ 完成   ← 並行できる！
```

フロントエンドは **MSW（Mock Service Worker）** などを使い、
OpenAPI 仕様書を元にモックサーバーを立てて開発できます。

---

## ワークショップとの対応関係

| ワークショップ Phase | OpenAPI 駆動開発との対応 |
|---|---|
| Phase 2：API設計書生成 | OpenAPI YAML を確定させる（投資フェーズ） |
| Phase 3：Issue 登録 | 自動生成できる Issue を省略・統合できる |
| Phase 4：実装 | クライアントコード・バリデーションを自動生成で省力化（回収フェーズ） |
| Phase 5：PR レビュー | CI で OpenAPI 準拠チェックを自動実行 |

**Phase 2 で丁寧に設計した分が、Phase 4 で一気に回収できる**というのが
OpenAPI 駆動開発の本質です。

---

## まとめ

```
Phase 2 で OpenAPI 設計書をしっかり作る
        ↓
┌──────────────────────────────────────────────┐
│ ① クライアントコードを自動生成               │
│   → Services/*.cs を手書きしなくていい       │
│                                              │
│ ② バリデーションを自動化                     │
│   → バリデーションコードを書かなくていい     │
│                                              │
│ ③ テストの期待値が明確になる                 │
│   → テストがブレない・CI で自動検証できる    │
│                                              │
│ ④ フロントとバックの並行開発が可能           │
│   → 待ち時間ゼロで両チームが動ける           │
└──────────────────────────────────────────────┘
        ↓
  これが「モダンな開発スタイル」の正体
```

「速くコードを書く」のではなく、
**「書かなくていいコードを増やす」** ことで生産性を上げる。

Claude Code による AI 駆動開発と、OpenAPI 駆動開発は、
この思想を共有しています。

---

## 参考リンク

- [OpenAPI Specification 公式](https://spec.openapis.org/oas/latest.html)
- [NSwag：OpenAPI → C# クライアント自動生成](https://github.com/RicoSuter/NSwag)
- [Kiota：Microsoft 製クライアント生成ツール](https://github.com/microsoft/kiota)
- [Spectral：OpenAPI Linter](https://stoplight.io/open-source/spectral)
- [MSW：フロントエンド向けモックサーバー](https://mswjs.io/)
