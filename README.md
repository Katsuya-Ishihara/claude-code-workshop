# Claude Code 活用ワークショップ
**〜 チーム向け Todo タスク管理アプリで Claude 駆動開発を体感する 〜**

---

## このリポジトリについて

本リポジトリは **Claude Code を使った AI 駆動開発**を体感するワークショップの教材です。

「要件定義書を渡すだけで、設計 → Issue登録 → 実装 → レビュー → PR作成までを  
Claude が自律的に実行する」開発スタイルをハンズオンで学びます。

---

## ワークショップの進め方

### ステップ1：デモを見る（講師が実演）

まず講師のデモンストレーションを通じて、全体の流れを把握します。

```
Phase 0  要件定義書の作成          👤 人間のみ
Phase 1  CLAUDE.md・ガイドライン生成 🤖 + 👤
Phase 2  設計書生成（DB・API・画面）  🤖 + 👤
Phase 3  タスク分解・Issue 自動登録  🤖 + 👤
Phase 4  テストファースト実装        🤖
Phase 5  PR作成・レビュー・マージ承認 🤖 + 👤
Phase 6  CI/CD パイプライン         🤖
```

### ステップ2：自分でなぞる（ハンズオン）

デモ終了後、このリポジトリを fork して自分の環境で同じ手順を再現してください。  
各 Phase のプロンプトはシナリオ台本にコピペ用でまとめてあります。

---

## リポジトリ構成

```
.
├── README.md                              # このファイル
├── claude_code_workshop_scenario_v6.md    # シナリオ台本（全 Phase のプロンプト集）
└── docs/
    └── tips/
        ├── README.md                      # Tips 索引
        ├── openapi_driven_dev.md          # Tips：OpenAPI 駆動開発と省力化のしくみ
        └── efcore_codefirst.md            # Tips：EF Core Code First とエンティティ設計
```

### 各ファイルの役割

| ファイル | 説明 |
|---|---|
| `claude_code_workshop_scenario_v6.md` | 全 Phase の手順・プロンプトをまとめたメインドキュメント |
| `docs/tips/openapi_driven_dev.md` | Phase 2 の API 設計書に OpenAPI YAML を含める理由と省力化の解説 |
| `docs/tips/efcore_codefirst.md` | DB 設計と同時にエンティティクラスが書ける理由の解説 |

> **💡 Tips ドキュメントについて**  
> シナリオ台本の流れを追うだけでもハンズオンは完結します。  
> 「なぜそう設計するのか」を深く理解したい方は Tips を参照してください。

---

## 事前準備

ハンズオンを始める前に以下を準備してください。

```
□ Claude Code インストール
    npm install -g @anthropic-ai/claude-code

□ GitHub CLI（gh）インストール・認証
    brew install gh
    gh auth login

□ .NET SDK 8.0 インストール
    https://dotnet.microsoft.com/download

□ Node.js インストール（GitHub Actions 設定用）

□ VS Code または任意のターミナル
```

---

## ハンズオンの開始手順

### 1. このリポジトリを fork する

GitHub の「Fork」ボタンから自分のアカウントに fork してください。

### 2. ローカルに clone する

```bash
git clone https://github.com/{your-account}/claude-code-workshop.git
cd claude-code-workshop
```

### 3. 要件定義書を配置する

```bash
mkdir -p docs
# docs/要件定義書.md を作成する（シナリオ台本 Phase 0 を参照）
```

### 4. Claude Code を起動する

```bash
claude
```

### 5. シナリオ台本の Phase 1 から開始する

`claude_code_workshop_scenario_v6.md` を開き、  
Phase 1 のプロンプトをコピペして Claude Code に渡してください。

---

## 人間と Claude の役割分担

このワークショップで最も重要な概念です。

```
Claude が担う領域：「どう書くか」
  → ルールに照らして ○ か × かが明確なもの
  → 設計書生成・コード実装・チェック・PR 作成

人間が担う領域：「何を作るか・なぜそう作るか」
  → 判断と文脈が必要なもの
  → 要件定義・設計レビュー・最終承認
```

> **AI は速度と網羅性を上げる手段であり、最終的な品質の責任は人間が持ちます。**

---

## Tips：モダン開発プラクティス

本ワークショップでは AI 駆動開発に加え、以下のモダンな開発手法も体感できます。

- **OpenAPI 駆動開発** → クライアントコードの自動生成・バリデーション自動化  
  → [詳細](./docs/tips/openapi_driven_dev.md)

- **EF Core Code First** → DB 設計と同時にエンティティクラスが完成する理由  
  → [詳細](./docs/tips/efcore_codefirst.md)

これらは AI 以前から存在するベストプラクティスです。  
Claude Code はこれらを「人間のスキルに依存せず再現できる」ようにしました。

---

## ライセンス

本教材はワークショップ用途に限り自由に利用できます。
