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
Phase 0  要件定義書の作成           👤 人間のみ
Phase 1  CLAUDE.md・ガイドライン生成  🤖 + 👤
Phase 2  設計書生成（DB・API・画面）   🤖 + 👤
Phase 3  タスク分解・Issue 自動登録   🤖 + 👤
Phase 4  テストファースト実装         🤖
Phase 5  PR作成・レビュー・マージ承認  🤖 + 👤
Phase 6  CI/CD パイプライン          🤖
```

### ステップ2：自分でなぞる（ハンズオン）

デモ終了後、このリポジトリを **fork** して自分の環境で同じ手順を再現してください。  
各 Phase のプロンプトはシナリオ台本にコピペ用でまとめてあります。

---

## fork とは？（Git 未経験者向け）

### fork の意味

**fork とは、他人のリポジトリを自分のアカウントに丸ごとコピーすること**です。

```
講師のリポジトリ（オリジナル）
  Katsuya-Ishihara/claude-code-workshop
          ↓  GitHub の「Fork」ボタンを押す
自分のリポジトリ（コピー）
  your-account/claude-code-workshop   ← ここに自由にコミットできる
```

### なぜ fork が必要か

このワークショップでは Claude Code が自動でコミット・PR を作成します。  
**fork せずに直接作業すると、生成されたコードが講師のリポジトリに入ってしまいます。**

fork することで、自分の作業はすべて自分のリポジトリに入り、  
講師のリポジトリには影響を与えません。

```
❌ fork しない場合
  Claude がコミット → 講師のリポジトリが汚れる（最悪、他の参加者の邪魔になる）

✅ fork する場合
  Claude がコミット → 自分のリポジトリだけに入る（他の人に影響なし）
```

### fork の手順

1. このページ右上の **「Fork」ボタン**をクリック
2. 自分の GitHub アカウントを選択
3. 自分のアカウントに同じリポジトリが複製される
4. 複製されたリポジトリを clone して作業開始

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
    # Mac の場合
    brew install gh
    gh auth login

    # Windows の場合
    winget install --id GitHub.cli
    gh auth login

□ .NET SDK 8.0 インストール
    https://dotnet.microsoft.com/download

□ Node.js インストール（GitHub Actions 設定用）

□ VS Code または任意のターミナル

□ Context7 MCP サーバー インストール
    claude mcp add context7 -- npx -y @upstash/context7-mcp
```

### Context7 MCP サーバーとは

**MCP（Model Context Protocol）** は、Claude に外部ツールやデータソースを接続するための仕組みです。
Claude Code はデフォルトでいくつかのツールを持っていますが、MCP サーバーを追加することで機能を拡張できます。

**Context7** は .NET・Blazor などのライブラリの**最新ドキュメントをリアルタイムで取得**する MCP サーバーです。
Claude の学習データには含まれていない最新の API 仕様や破壊的変更も正確に参照できます。

```
MCP サーバーなし：Claude の知識（学習データ）のみ → 古い API を使うリスクあり
MCP サーバーあり：最新ドキュメントを都度参照  → 常に正確な実装が可能
```

インストール後は Claude Code 起動時に自動的に有効になります。
Claude が必要に応じて `use context7` と指示するだけでドキュメントを参照します。

---

## ハンズオンの開始手順

### 1. このリポジトリを fork する

このページ右上の「Fork」ボタンを押して、自分のアカウントに複製します。  
→ [fork とは何か](#fork-とはgit-未経験者向け) を先に読んでおくことをおすすめします。

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

## 【講師向け】予行演習と本番のリセット手順

### 初期状態をタグで保存する（初回のみ）

ワークショップ教材の状態（コードが何も生成されていない状態）をタグとして保存します。  
これが「リセットの基準点」になります。

```bash
git tag v0-start
git push origin v0-start
```

### 予行演習をする

タグを打った後は自由に予行演習できます。  
Claude Code がコミットを積んでも問題ありません。

### 本番前にリセットする

予行演習で積んだコミットをすべて消して、初期状態に戻します。

```bash
git reset --hard v0-start
git push origin main --force
```

これで `v0-start` タグ時点の状態に完全に戻ります。  
何度でも繰り返し使えます。

> ⚠️ **注意：`reset --hard` はコミット履歴もファイルの中身も完全に消去します**  
> 予行演習中にシナリオ台本や Tips を手で修正した場合、その変更も消えます。  
> リセット前に手動で修正した内容がある場合は、先に別ブランチに退避してください。
>
> ```bash
> # リセット前に予行演習の内容を退避（手動修正がある場合のみ）
> git checkout -b rehearsal-backup
> git push origin rehearsal-backup
>
> # その後 main をリセット
> git checkout main
> git reset --hard v0-start
> git push origin main --force
> ```

### 本番当日の公開手順

参加者が fork できるよう、リポジトリを public に変更します。

```bash
gh repo edit Katsuya-Ishihara/claude-code-workshop --visibility public --accept-visibility-change-consequences
```

ワークショップ終了後は private に戻します。

```bash
gh repo edit Katsuya-Ishihara/claude-code-workshop --visibility private --accept-visibility-change-consequences
```

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
