# Yaxel

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
    "A": {
        "union": {
            "X": null,
            "Y": "int"
        }
    },
    "B": {
        "record": {
            "Field1": "double",
            "Field2": "A",
            "Field3": {
                "type": "list",
                "args": [ "int" ]
            }
        }
    },
    "C": {
        "params": [ "T1", "T2" ],
        "record": {
            "hoge": "int"
        }
    }
}
```
型名はフルパスで入れるものとする。
例：　`Namespace1.Modue1.Module2.Hoge`

数値型の場合、範囲指定が出来るようにしたい。
GUI入力時のバリデーションの自動化。
あるいは固定リストからの選択。

```json
{
    "type": "int",
    "validation": [
    ]
}
```

うーん、まず F# でメタ情報の構造を表現してみよう。それを後からJSONに変換するという作戦。

```fsharp
type Type =
    | Int
    | Float
    | Bool
    | String
    | Option of Type
    | List of Type
    | Record of RecordType

and RecordType = {
        Name : string
        Fields: RecordField list
    }

and RecordField = {
        Name : string
        Type : Type
    }

and UnionType = {
        Name : string
        Cases : UnionCase list
    }

and UnionCase = {
        Name : string
    }
```


