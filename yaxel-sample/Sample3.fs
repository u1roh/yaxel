module Sample3

type JisMaterial =
    | A1100
    | A1050
    | A6061
    | SUS304
    | SUS316
    | SS400
    | S45C
    | SPCC

type MaterialCategory =
    | Aluminium
    | StainlessSteel
    | Steel

let getMaterialCategory (jis: JisMaterial) =
    match jis with
    | A1100
    | A1050
    | A6061 -> Aluminium
    | SUS304
    | SUS316 -> StainlessSteel
    | SS400
    | S45C
    | SPCC -> Steel

type MetalSheet =
    { Material: JisMaterial
      SizeX: float
      SizeY: float
      Thickness: float }

let metalSheetDescription (ms: MetalSheet) =
    let area = ms.SizeX * ms.SizeY
    let volume = area * ms.Thickness
    sprintf "%A, %f[mm^2], %f[mm^3]" ms.Material area volume
