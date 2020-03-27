module Sample

type Material =
    | SUS304
    | SPCC
    | ABC a

type Foo =
    { X: int
      Y: bool }

type Input =
    { A: int
      //B: double
      C: Material
      D: string option
      E: Foo option }

type Output =
    { Number: int
      Material: Material }

let hoge (x: Input) =
    { Number = x.A * 23
      Material = x.C }

let piyo (x: Material) = "buzz"

let simple (x: int) = x * 2
