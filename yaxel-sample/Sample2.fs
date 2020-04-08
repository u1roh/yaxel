module Sample2

type EquationAnswer =
    | NoAnswers
    | OneAnswer of float
    | TwoAnswers of float * float

let solveQuadricEquation (a: float) (b: float) (c: float) =
    if a = 0.0 then
        if b = 0.0 then NoAnswers else OneAnswer(-c / b)
    else
        let d = b * b - 4.0 * a * c
        if d < 0.0 then
            NoAnswers
        elif d = 0.0 then
            -b / (2.0 * a) |> OneAnswer
        else
            let x1 = (-b + sqrt d) / (2.0 * a)
            let x2 = (-b - sqrt d) / (2.0 * a)
            TwoAnswers(x1, x2)
