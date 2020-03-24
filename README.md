# Yaxel - Yet Another Excel

メモ書き。なぐり書きレベル。

## 構成
* server side: F#
* client: TypeScript
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

