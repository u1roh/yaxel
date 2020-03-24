# Yaxel - Yet Another ???

メモ書き。なぐり書きレベル。

アレよりもキレイな言語で、アレに迫る手軽さで、ロジックを書きたい。

## おもいついき
* アレよりキレイな言語 = F#
   * F# で色んなことやろうとすると割と難しくなっちゃうけど
   * 状態遷移のないシンプルなロジックの記述なら F# はとてもシンプルに書ける
   * F# のキレイな部分だけを subset として取り出せば、アレのユーザーも書けるはず
      * 型推論あるからシンプルに書ける
      * 型は プリミティブ型 + Record 型 + Union 型 だけあれば戦える
* .NET はリフレクションが使える
   * 実行時に型情報を取り出せる
   * 実行時に型情報を JSON に変換できる
   * JSON を client に送って GUI 自動生成できるのでは
* つまり作りたいものは
   * F# で関数を書くと
   * 関数の引数の型情報が動的に JSON に変換され
   * client に送られて入力 GUI が自動生成され
   * GUI に値を入力すると、値が JSON に変換されてサーバーに送られ
   * サーバーで F# 製の関数を動的に呼び出し
   * 戻り値が再びクライアントに送られて表示される

## 構成
* server side: F#
* client: TypeScript + React
* 2種類のJSON
    * Schema JSON
        * 型情報をJSON形式で表現したもの
        * F# (.NET) からリフレクションにより自動生成
        * Schema JSON から入力UI・表示UIを自動生成
    * Data JSON
        * Schema JSON に沿った形式のデータ。インスタンスの情報。
        * 

## Schema JSON
* プリミティブ型
    * int 整数値
    * float 実数値
    * bool
* 直積型（record/struct）
* 直和型（union/enum）
* リスト型

```fsharp
type A =
    | X
    | Y of int

type B = {
    Field1 : double
    Field2 : A
}  
```
```json
{
    "tag": "record",
    "name": "B",
    "fields": [
        {
            "name": "Field1",
            "type": "float"
        },
        {
            "name": "Field2",
            "type": {
                "tag": "union",
                "name": "A",
                "cases": [
                    {
                        "name": "X",
                        "type": null
                    },
                    {
                        "name": "Y",
                        "type": "int"
                    }
                ]
            }
        }
    ]
}
```

## やりたいこと
* 数値型の場合、範囲指定が出来るようにしたい。
* GUI入力時のバリデーションの自動化。
* あるいは固定リストからの選択。

