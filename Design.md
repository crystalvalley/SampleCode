# Discord.Net Bot Prefix Routing 設計書（ExecuteAsync 単一実装版）

## 1. 目的

`SampleCommandService.ExecuteAsync` 内のみで `! / $ / %` を処理するミニマルな Discord.Net Bot の仕様を定義する。本ドキュメントは実装ではなく、GitHub で共有するための正式な設計定義である。

## 2. 開発環境と前提

- 言語: .NET 8 / C#
- Discord SDK: Discord.Net (`Discord.WebSocket`)
- Bot クライアントの接続処理（Login / Start）は別箇所で実施済みである前提
- 本設計では `SampleCommandService.ExecuteAsync` のみが対象範囲
- Discord Developer Portal 側で Message Content Intent を有効化しておくこと
- Bot 自身のメッセージは無視すること

## 3. Prefix 仕様

| Prefix | 種別 | 動作 | 例 |
|--------|------|------|------|
| `!` | 一般コマンド | `!ping` → `pong`（この1件のみ対応） | `!ping` |
| `$` | エコー | `$` 以降の文字列をそのまま返信（空文字は返信しない） | `$hello → hello` |
| `%` または `％` | 開発者専用 | 開発者ID一致時のみ有効。`ping` → `dev-pong` | `%ping`, `％ping` |

- `%`（半角, U+0025）と `％`（全角, U+FF05）の双方を許可する。
- DeveloperId は一旦定数ハードコードで対応する。

## 4. メッセージ処理フロー


```text
MessageReceived
 ├─ Bot発言 → 無視
 ├─ Content空 → 無視
 ├─ Prefix = content[0], Body = content[1..]
 │
 ├─ Prefix == '!' → Body.Trim() == "ping" なら「pong」
 ├─ Prefix == '$' → Bodyをそのまま返信（空なら無視）
 ├─ Prefix == '%' or '％'
 │      ├─ Author.Id == DeveloperId → Body.Trim() == "ping" なら「dev-pong」
 │      └─ 不一致なら「開発者専用コマンドです。」を返信
 │
 └─ その他 → 無視

 
## 5. 実装方針

- `ExecuteAsync` の中で `Client.MessageReceived += ...` を登録して処理を行う。
- Prefix 判定は文字列先頭1文字のみを見る。
- `!ping` のみ対応し、他の `!xxx` は未知扱いとする。
- `$` の後続文字列はトリムせず、そのまま返信する。
- `%` / `％` の開発者判定は `Author.Id == DeveloperId` で判定する。
- エラー発生時も Bot プロセスが停止しないようにログだけを残す。

## 6. テスト観点

| 入力 | 開発者ID一致 | 期待応答 |
|------|-------------|----------|
| `!ping` | 任意 | `pong` |
| `$hello test` | 任意 | `hello test` |
| `%ping` または `％ping` | Yes | `dev-pong` |
| `%ping` または `％ping` | No | `開発者専用コマンドです。` |
| `$` | 任意 | 応答なし |
| Bot の発言 | - | 応答なし |
| Prefix なしの文字列 | - | 応答なし |

## 7. ロギング方針

| ケース | ログレベル |
|--------|-----------|
| Prefix処理開始時 | Information |
| 開発者ID不一致 | Warning |
| 例外発生時 | Error |
| Bot発言・空文字無視 | Debug もしくは Trace（必要なら） |

## 8. 拡張ロードマップ

- `!` 系を Discord.Commands または SlashCommands へ移行する
- `$` 系にテンプレ展開（`${user}` など）を導入する
- `%` 系の判定を DeveloperId 固定から Roleチェックに拡張する
- Prefix Router を別クラス化して xUnit による単体テストを導入する
- Slash Commands により権限管理とUI操作を Discord 側に委譲する

## 9. Definition of Done（MVP 完了条件）

- [ ] `ExecuteAsync` のみで Prefix 処理が完結している
- [ ] `!ping` で `pong` が返信される
- [ ] `$text` で `text` が返信される（空文字は無視される）
- [ ] `%ping` / `％ping` が DeveloperId 一致時にのみ `dev-pong` を返す
- [ ] Bot の発言には反応しない
- [ ] 例外がログに記録され、プロセスが落ちない
- [ ] Message Content Intent が Discord Dev Portal で有効化されている

## 10. 注意事項

- DeveloperId のハードコードは暫定的な対応であり、実運用前に設定ファイルや環境変数化する必要がある。
- `ExecuteAsync` に全処理を記述する実装方式は可読性が低下しやすいため、拡張前に責務分離を行うこと。
- Message Content Intent が無効な場合、Bot はメッセージ本文を取得できず常に無反応となるため注意する。
